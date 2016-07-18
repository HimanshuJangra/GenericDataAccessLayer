using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using FastMember;
using L10n.Properties;
using NProxy.Core;
using SharedComponents;
using GenericDataAccessLayer.Core;
using System.Configuration;
using System.Diagnostics;

namespace GenericDataAccessLayer.LazyDal.StoredProcedure
{
    /// <summary>
    /// Rules 
    /// <para>Read</para>
    /// Read data using stored procedure should return List with underlying generic type of current entity
    /// <para>Update</para>
    /// </summary>
    public class GenericDalInterceptor : IInvocationHandler
    {
        /// <summary>
        /// since more or less GenericDalInterceptor an a singleton and IBasicDbAccess.Execute support only transit parameter
        /// use this transit object to make GenericDalInterceptor Thread Safe, which mean, no private fields/properties are allowed
        /// </summary>
        private class TransitObject
        {
            /// <summary>
            /// List of parameters that contains it value too
            /// </summary>
            public Dictionary<ParameterInfo, object> Parameters;

            /// <summary>
            /// list of output parameters that should write to "out"
            /// </summary>
            public readonly Dictionary<ParameterInfo, IDataParameter> Outputs = new Dictionary<ParameterInfo, IDataParameter>();
            /// <summary>
            /// cache all referece types (class) that are used as Output Or RefBy
            /// </summary>
            public readonly List<RefScalarVariables> Refernces = new List<RefScalarVariables>();

            /// <summary>
            /// Name of Stored Procedure
            /// </summary>
            public string CommandText;

            /// <summary>
            /// Return Type
            /// </summary>
            public Type ReturnType;

            /// <summary>
            /// Reference to Return value
            /// </summary>
            public object ReturnObject;
        }

        /// <summary>
        /// Connection String setting name. Be aware that this parameter is global depends on instance.
        /// </summary>
        private string _connectionStringSettings = "DefaultConnection";

        private RepositoryOperations _operations = RepositoryOperations.None;

        /// <summary>
        /// Default naming convention for TVP variables
        /// </summary>
        private string _tvpNameConvention = "{0}TVP";

        /// <summary>
        /// Configuration Setting for Connection String
        /// </summary>
        private ConnectionStringSettings _settings;

        private ConnectionStringSettings Settings
        {
            get
            {
                if (_settings == null)
                {
                    _settings = ConfigurationManager.ConnectionStrings[_connectionStringSettings];
                }

                return _settings;
            }
        }

        /// <summary>
        /// Provider Factory that we need to create connection
        /// </summary>
        private System.Data.Common.DbProviderFactory _factory;

        private System.Data.Common.DbProviderFactory Factory
        {
            get
            {
                if (_factory == null)
                {
                    _factory = System.Data.Common.DbProviderFactories.GetFactory(Settings.ProviderName);
                }
                return _factory;
            }
        }

        /// <summary>
        /// Watch for query execution time
        /// </summary>
        private Stopwatch _queryTime;

        /// <summary>
        /// Tracker for total execution time
        /// </summary>
        private Stopwatch _totalTime;

        private IDbConnection _connection;

        /// <summary>
        /// Current Connection in Use
        /// </summary>
        private IDbConnection Connection
        {
            get
            {
                if (_connection == null)
                {
                    _connection = Factory.CreateConnection();
                }
                if (string.IsNullOrEmpty(_connection.ConnectionString))
                {
                    _connection.ConnectionString = Settings.ConnectionString;
                }

                if ((ConnectionState.Closed | ConnectionState.Broken).HasFlag(_connection.State))
                {
                    _connection.Open();
                }

                return _connection;
            }
        }

        /// <summary>
        /// Initialize some vital properties for SP execution
        /// </summary>
        /// <param name="transit">Placeholder for internal parameter use</param>
        /// <param name="methodInfo">Method Information</param>
        /// <param name="parameters">Parameters from execution code</param>
        private void PrepareInitialization(TransitObject transit, MethodInfo methodInfo, object[] parameters)
        {
            transit.ReturnType = methodInfo.ReturnType;
            // fill parameter informations
            transit.Parameters = new Dictionary<ParameterInfo, object>();
            int index = 0;
            foreach (var item in methodInfo.GetParameters())
            {
                transit.Parameters.Add(item, parameters[index++]);
            }

            transit.CommandText = methodInfo.Name;
            var retType = transit.ReturnType;
            
            if (retType.In(typeof(void), typeof(string)) == false)
            {
                if (retType.IsArray || typeof(IEnumerable).IsAssignableFrom(retType))
                {
                    transit.ReturnObject = new ArrayList();
                }
                else
                {
                    transit.ReturnObject = Activator.CreateInstance(transit.ReturnType);
                }
            }
        }

        /// <summary>Processes an invocation on a target.</summary>
        /// <param name="target">The target object.</param>
        /// <param name="methodInfo">The method information.</param>
        /// <param name="parameters">The parameter values.</param>
        /// <returns>The return value.</returns>
        public object Invoke(object target, MethodInfo methodInfo, object[] parameters)
        {
            Object result = null;
            if (methodInfo.Name == nameof(IRepository.Dispose))
            {
                _connection?.Dispose();
                _connection = null;
            }
            else if (methodInfo.Name == $"set_{nameof(IRepository.ConnectionStringSettings)}")
            {
                string newSetting = parameters[0].ToString();
                if (newSetting != _connectionStringSettings)
                {
                    _connection?.Dispose();
                    _connection = null;
                    _settings = null;
                    _connectionStringSettings = newSetting;
                }
            }
            else if (methodInfo.Name == $"get_{nameof(IRepository.ConnectionStringSettings)}")
            {
                result = _connectionStringSettings;
            }
            else if (methodInfo.Name == $"set_{nameof(IRepository.Connection)}")
            {
                _connection = parameters[0] as IDbConnection;
            }
            else if (methodInfo.Name == $"get_{nameof(IRepository.Connection)}")
            {
                result = Connection;
            }
            else if (methodInfo.Name == $"set_{nameof(IRepository.TvpNameConvension)}")
            {
                _tvpNameConvention = parameters[0]?.ToString();
            }
            else if (methodInfo.Name == $"get_{nameof(IRepository.TvpNameConvension)}")
            {
                result = _tvpNameConvention;
            }
            else if (methodInfo.Name == $"get_{nameof(IRepository.QueryExecutionTime)}")
            {
                result = _queryTime?.Elapsed;
            }
            else if (methodInfo.Name == $"get_{nameof(IRepository.TotalExecutionTime)}")
            {
                result = _totalTime?.Elapsed;
            }
            else if (methodInfo.Name == $"set_{nameof(IRepository.Operations)}")
            {
                _operations = (RepositoryOperations)parameters[0];
                InitWatches();
            }
            else if (methodInfo.Name == $"get_{nameof(IRepository.Operations)}")
            {
                result = _operations;
            }
            else
            {
                InitWatches();
                _totalTime?.Reset();
                _queryTime?.Reset();
                _totalTime?.Start();
                try
                {
                    var transit = new TransitObject();
                    PrepareInitialization(transit, methodInfo, parameters);
                    var connection = Connection;
                    using (var command = connection.CreateCommand())
                    {
                        command.Connection = connection;
                        Execute(transit, command);
                    }

                    // write output parameters
                    foreach (var item in transit.Outputs)
                    {
                        parameters[item.Key.Position] = item.Value.Value;
                    }

                    foreach (var item in transit.Refernces)
                    {
                        item.WriteBack(parameters);
                    }

                    result = transit.ReturnObject;
                }
                catch when (ExceptionFilter())
                {
                }
                _totalTime?.Stop();
            }
            return result;
        }
        /// <summary>
        /// Init or remove watches
        /// </summary>
        private void InitWatches()
        {
            if (_operations.HasFlag(RepositoryOperations.LogQueryExecutionTime) && _queryTime == null)
            {
                _queryTime = new Stopwatch();
            }
            else if (_operations.HasFlag(RepositoryOperations.LogQueryExecutionTime) == false)
            {
                _queryTime = null;
            }

            if (_operations.HasFlag(RepositoryOperations.LogTotalExecutionTime) && _totalTime == null)
            {
                _totalTime = new Stopwatch();
            }
            else if (_operations.HasFlag(RepositoryOperations.LogTotalExecutionTime) == false)
            {
                _totalTime = null;
            }
        }

        /// <summary>
        /// Check if current Query Timer is running. Stop it and continue with throwing exception
        /// </summary>
        /// <returns>FALSE, because we dont want to catch the exception</returns>
        private bool ExceptionFilter()
        {
            if (_queryTime?.IsRunning == true)
            {
                _queryTime?.Stop();
            }
            return _operations.HasFlag(RepositoryOperations.IgnoreException);
        }

        /// <summary>
        /// Create new TVP is interceptor configured for Table Valued Parameters. Accept any data of type ICollection
        /// </summary>
        /// <param name="parameterName">Property name used as TVP name</param>
        /// <param name="command">Using DbCommand handle db parameter value</param>
        /// <param name="value">ICollection that should pass to DB</param>
        /// <returns>If true, skip scalar parameter created</returns>
        private void CreateNewDataTableParameter(string parameterName, IDbCommand command, object value)
        {
            if (value is ICollection)
            {
                var data = (ICollection)value;
                var dataType = data.GetType().GenericTypeArguments[0];

                var table = new DataTable(string.Format(_tvpNameConvention, dataType.Name));

                if (dataType.IsClass && dataType != typeof(string))
                {
                    var reader = new ObjectReader(dataType, data);
                    table.Load(reader);
                }
                else
                {
                    throw new NotSupportedException();
                }
                command.AddParameter(parameterName, table);
            }
        }
        /// <summary>
        /// Pre-Execution Process that allow create stored Procedure parameters
        /// </summary>
        /// <param name="transit">placeholder that contains sp parameters</param>
        /// <param name="command">command we used to store parameters</param>
        private void PrepareExecute(TransitObject transit, IDbCommand command)
        {
            Type refString = Type.GetType("System.String&");
            // fill the parameters
            foreach (var item in transit.Parameters.Where(a => a.Value.GetType().GenericTypeArguments.Length == 0))
            {
                ParameterInfo pi = item.Key;
                ParameterDirection direction = ParameterDirection.Input;
                if (pi.IsOut && pi.IsIn == false)
                    direction = ParameterDirection.Output;
                else if (pi.ParameterType.IsByRef)
                    direction = ParameterDirection.InputOutput;

                var type = item.Value?.GetType() ?? pi.ParameterType;

                if (type.IsClass && type.In(typeof(string), refString) == false)
                {
                    CreateRefScalarVariable(transit, item.Key.Position, type, item.Value, command, direction);
                }
                else
                {
                    int? size = null;

                    if (pi.ParameterType.In(typeof(string), refString))
                    {
                        size = -1;
                    }
                    var parameter = command.AddParameter(pi.Name, item.Value, direction, size: size);

                    if (pi.IsOut || pi.ParameterType.IsByRef)
                    {
                        transit.Outputs.Add(pi, parameter);
                    }
                }
            }
            command.CommandType = CommandType.StoredProcedure;
            command.CommandText = transit.CommandText;
        }


        /// <summary>
        /// Concrete implementation for List handler
        /// </summary>
        /// <param name="transit">return value as reference value</param>
        /// <param name="command">executive command</param>
        private void Execute(TransitObject transit, IDbCommand command)
        {
            // can only set if TVP is activated
            if (_operations.HasFlag(RepositoryOperations.UseTableValuedParameter))
            {
                ExecuteTvp(transit, command);
            }
            else
            {
                ExecuteSingleItem(transit, command);
            }
        }
        /// <summary>
        /// Execute Single Item, when TVP is not available but list is given
        /// </summary>
        /// <param name="transit">return value as reference value</param>
        /// <param name="command">executive command</param>
        private void ExecuteSingleItem(TransitObject transit, IDbCommand command)
        {
            // do not allow cartesian product
            var collection = transit.Parameters.Where(a => a.Value is ICollection).Select(a => new KeyValuePair<ParameterInfo, ICollection>(a.Key, (ICollection)a.Value)).ToList();
            if (_operations.HasFlag(RepositoryOperations.UseTableValuedParameter) == false &&
                collection.Count > 1 &&
                (transit.ReturnObject is IList) == false)
            {
                throw new NotSupportedException(nameof(Resources.DA004));
            }
            PrepareExecute(transit, command);
            bool isArrayOrEnumerable = transit.ReturnType.IsArray || typeof(IEnumerable).IsAssignableFrom(transit.ReturnType);
            // if transit is List execute reader
            if (transit.ReturnObject is IList || isArrayOrEnumerable)
            {
                IList items = null;
                TypeAccessor accessor = null;
                if (transit.ReturnType.IsArray)
                {
                    accessor = TypeAccessor.Create(transit.ReturnType.GetElementType());
                }
                else
                {
                    accessor = TypeAccessor.Create(transit.ReturnType.GenericTypeArguments[0]);
                }
                items = (IList)transit.ReturnObject;

                if (collection.Count > 0)
                {
                    var first = collection.First();
                    var valueType = first.Value.GetType();
                    var genericType = valueType.IsArray ? valueType.GetElementType() : valueType.GenericTypeArguments[0];
                    TypeAccessor listType = genericType.IsClass && genericType != typeof(string) ? TypeAccessor.Create(genericType) : null;

                    foreach (var item in first.Value)
                    {
                        CreateRefScalarVariable(first.Key.Name, item, listType, command);
                        ExecuteReaderToList(items, accessor, command);
                    }
                }
                else
                {
                    ExecuteReaderToList(items, accessor, command);
                }

                if (transit.ReturnType.IsArray)
                {
                    transit.ReturnObject = ((ArrayList)items).ToArray();
                }
            }
            // execute scalar. Can be Struct too
            else if (transit.ReturnObject != null || transit.ReturnType == typeof(string))
            {
                ExecuteWithSingleReturnValue(transit, command);
            }
            // ExecuteNonQuery
            else
            {
                if (collection.Count > 0)
                {
                    var genericType = collection.First().Value.GetType().GenericTypeArguments[0];
                    TypeAccessor listType = genericType.IsClass && genericType != typeof(string) ? TypeAccessor.Create(genericType) : null;

                    foreach (var item in collection.First().Value)
                    {
                        CreateRefScalarVariable(genericType.Name, item, listType, command);
                        ExecuteNonQuery(command);
                    }
                }
                else
                {
                    ExecuteNonQuery(command);
                }
            }
        }
        /// <summary>
        /// Execute if TVP is activated
        /// </summary>
        /// <param name="transit">return value as reference value</param>
        /// <param name="command">executive command</param>
        private void ExecuteTvp(TransitObject transit, IDbCommand command)
        {
            PrepareExecute(transit, command);
            foreach (var item in transit.Parameters)
            {
                CreateNewDataTableParameter(item.Key.Name, command, item.Value);
            }

            // if transit is List execute reader
            if (transit.ReturnType.IsArray || (true.In(transit.ReturnType.IsInterface, transit.ReturnType.IsAbstract) && typeof(IEnumerable).IsAssignableFrom(transit.ReturnType)))
            {
                TypeAccessor accessor = TypeAccessor.Create(transit.ReturnType.IsArray ? transit.ReturnType.GetElementType() : transit.ReturnType.GenericTypeArguments[0]);
                var items = transit.ReturnObject as ArrayList;
                ExecuteReaderToList(items, accessor, command);
                transit.ReturnObject = items.ToArray();
            }
            else if (transit.ReturnObject is IList)
            {
                var items = (IList)transit.ReturnObject;
                var accessor = TypeAccessor.Create(transit.ReturnType.GenericTypeArguments[0]);
                ExecuteReaderToList(items, accessor, command);
            }
            else if (transit.ReturnObject != null)
            {
                // execute scalar. Can be Struct too
                ExecuteWithSingleReturnValue(transit, command);
            }
            else
            {
                ExecuteNonQuery(command);
            }
        }

        /// <summary>
        /// read only one object
        /// </summary>
        /// <param name="columns">number of fields</param>
        /// <param name="item">Current Empty Item to fill</param>
        /// <param name="accessor">TypeAccessor that help create new Object and fast access it properties/fields by name</param>
        /// <param name="record">active record</param>
        /// <returns></returns>
        private void ExtractObject(int columns, object item, TypeAccessor accessor, IDataRecord record)
        {
            for (int i = 0; i < columns; i++)
            {
                string name = record.GetName(i);
                accessor[item, name] = record.ReadObject(i);
            }
        }

        private void CreateRefScalarVariable(TransitObject transit, int index, Type type, object data, IDbCommand command, ParameterDirection direction)
        {
            var tAccessor = TypeAccessor.Create(type);
            RefScalarVariables result = null;
            if (direction == ParameterDirection.InputOutput || direction == ParameterDirection.Output)
            {
                transit.Refernces.Add(result = new RefScalarVariables
                {
                    Direction = direction,
                    Index = index,
                    Value = data,
                    Accessor = tAccessor
                });
            }
            foreach (var item in tAccessor.GetMembers())
            {
                object value = direction == ParameterDirection.Output ? null : tAccessor[data, item.Name];
                var parameter = command.AddParameter(item.Name, value, direction);
                result?.Parameters.Add(parameter);
            }
        }

        /// <summary>
        /// Use a list entry to create
        /// </summary>
        /// <param name="name">name of the single entry, if enumerable is a string or struct</param>
        /// <param name="data"></param>
        /// <param name="tAccessor"></param>
        /// <param name="command"></param>
        private void CreateRefScalarVariable(string name, object data, TypeAccessor tAccessor, IDbCommand command)
        {
            if (tAccessor != null)
            {
                foreach (var item in tAccessor.GetMembers())
                {
                    command.AddParameter(item.Name, tAccessor[data, item.Name]);
                }
            }
            else
            {
                command.AddParameter(name, data);
            }
        }
        /// <summary>
        /// Handle Reference scalars variables. Only usefull for Output and InputOutput Parameters
        /// </summary>
        private class RefScalarVariables
        {
            /// <summary>
            /// Direction of the parameter
            /// </summary>
            public ParameterDirection Direction;
            /// <summary>
            /// Parameter Information
            /// </summary>
            public int Index;
            /// <summary>
            /// Parameter Value
            /// </summary>
            public object Value;
            /// <summary>
            /// Current Parameter
            /// </summary>
            public readonly List<IDataParameter> Parameters = new List<IDataParameter>();
            /// <summary>
            /// Type Accessor the we need to write back parameter to reference type
            /// </summary>
            public TypeAccessor Accessor { private get; set; }

            public void WriteBack(object[] parameters)
            {
                if (Direction == ParameterDirection.Input || Direction == ParameterDirection.ReturnValue)
                {
                    return;
                }

                if (Value == null && Direction == ParameterDirection.Output)
                {
                    Value = Accessor.CreateNew();
                }

                foreach (var item in Parameters)
                {
                    Accessor[Value, item.ParameterName] = item.Value;
                }

                parameters[Index] = Value;
            }
        }

        #region Execution Helpers
        /// <summary>
        /// Execute SP with return value as non Enumerable
        /// </summary>
        /// <param name="transit">return value as reference value</param>
        /// <param name="command">executive command</param>
        private void ExecuteWithSingleReturnValue(TransitObject transit, IDbCommand command)
        {
            command.Prepare();
            if (transit.ReturnType.IsClass && transit.ReturnType != typeof(string))
            {
                _queryTime?.Start();
                using (var reader = command.ExecuteReader())
                {
                    _queryTime?.Stop();
                    int columns = reader.FieldCount;
                    var accessor = TypeAccessor.Create(transit.ReturnType);
                    foreach (var record in reader.ToRecord())
                    {
                        var item = accessor.CreateNew();
                        ExtractObject(columns, item, accessor, record);
                        transit.ReturnObject = item;
                        break;
                    }
                }
            }
            else
            {
                _queryTime?.Start();
                transit.ReturnObject = command.ExecuteScalar();
                _queryTime?.Stop();
            }
        }
        /// <summary>
        /// Execute SP that return a result set
        /// </summary>
        /// <param name="items">return result set</param>
        /// <param name="accessor">Type Accessor for a single item value</param>
        /// <param name="command">executive command</param>
        private void ExecuteReaderToList(IList items, TypeAccessor accessor, IDbCommand command)
        {
            command.Prepare();
            _queryTime?.Start();
            using (var reader = command.ExecuteReader())
            {
                _queryTime?.Stop();
                int columns = reader.FieldCount;
                foreach (var record in reader.ToRecord())
                {
                    var item = accessor.CreateNew();
                    items.Add(item);
                    ExtractObject(columns, item, accessor, record);
                }
            }
        }
        /// <summary>
        /// Execute NonQuery... without any return result
        /// </summary>
        /// <param name="command">executive command</param>
        private void ExecuteNonQuery(IDbCommand command)
        {
            command.Prepare();
            _queryTime?.Start();
            command.ExecuteNonQuery();
            _queryTime?.Stop();
        }
        #endregion
    }
}
