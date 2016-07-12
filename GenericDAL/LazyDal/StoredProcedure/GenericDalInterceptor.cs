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
        /// Configuration Setting for Connection String
        /// </summary>
        private ConnectionStringSettings _settings;

        /// <summary>
        /// Provider Factory that we need to create connection
        /// </summary>
        private System.Data.Common.DbProviderFactory _factory;

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
                    _connection = _factory.CreateConnection();
                    _connection.ConnectionString = _settings.ConnectionString;
                    _connection.Open();
                }
                else if (_connection.State == ConnectionState.Closed || _connection.State == ConnectionState.Broken)
                {
                    _connection.Open();
                }

                return _connection;
            }
        }

        private void ConfigureDbAccess()
        {
            if (_connection == null)
            {
                _settings = ConfigurationManager.ConnectionStrings[_connectionStringSettings];
                _factory = System.Data.Common.DbProviderFactories.GetFactory(_settings.ProviderName);
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

            transit.ReturnObject = transit.ReturnType == typeof(void) ? null : Activator.CreateInstance(transit.ReturnType);
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
            else if (methodInfo.Name == $"get_{nameof(IRepository.QueryExecutionTime)}")
            {
                result = _queryTime?.ElapsedTicks;
            }
            else if (methodInfo.Name == $"get_{nameof(IRepository.TotalExecutionTime)}")
            {
                result = _totalTime?.ElapsedTicks;
            }
            else if (methodInfo.Name == $"set_{nameof(IRepository.Operations)}")
            {
                _operations = (RepositoryOperations)parameters[0];
            }
            else if (methodInfo.Name == $"get_{nameof(IRepository.Operations)}")
            {
                result = _operations;
            }
            else
            {
                InitWatches();
                _totalTime?.Start();
                try
                {
                    ConfigureDbAccess();
                    var transit = new TransitObject();
                    PrepareInitialization(transit, methodInfo, parameters);

                    using (var command = Connection.CreateCommand())
                    {
                        command.Connection = Connection;
                        Execute(transit, command);
                    }

                    // write output parameters
                    foreach (var item in transit.Outputs)
                    {
                        parameters[item.Key.Position] = item.Value.Value;
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
            else if (_operations.HasFlag(RepositoryOperations.LogQueryExecutionTime) == false && _queryTime != null)
            {
                _queryTime = null;
            }

            if (_operations.HasFlag(RepositoryOperations.LogTotalExecutionTime) && _totalTime == null)
            {
                _totalTime = new Stopwatch();
            }
            else if (_operations.HasFlag(RepositoryOperations.LogTotalExecutionTime) == false && _totalTime != null)
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
            return false;
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
                IEnumerable data = value as IEnumerable;
                var dataType = data.GetType().GenericTypeArguments[0];
                var tAccessor = TypeAccessor.Create(dataType);

                if (_operations.HasFlag(RepositoryOperations.UseTableValuedParameter))
                {
                    var table = new DataTable();

                    foreach (var column in tAccessor.GetMembers())
                    {
                        var correctColumnType = column.Type;
                        // attention, DataColumn cannot handle nullables
                        if (correctColumnType.IsGenericType && correctColumnType.GetGenericTypeDefinition() == typeof(Nullable<>))
                        {
                            correctColumnType = column.Type.GenericTypeArguments[0];
                        }
                        // nested reference types are not supported, right now
                        if (column.Type != typeof(string) && column.Type.IsClass)
                        {
                            throw new NotSupportedException(nameof(Resources.DA003));
                        }
                        table.Columns.Add(new DataColumn(column.Name, correctColumnType));
                    }
                    foreach (var entry in data)
                    {
                        var row = table.NewRow();
                        table.Rows.Add(row);
                        foreach (var columnItem in tAccessor.GetMembers())
                        {
                            row.AddCellValue(columnItem.Name, tAccessor[entry, columnItem.Name]);
                        }
                    }
                    command.AddParameter(parameterName, table);
                }
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
            foreach (var item in transit.Parameters.Where(a => a.Key.ParameterType.GenericTypeArguments.Length == 0))
            {
                ParameterInfo pi = item.Key;
                ParameterDirection direction = ParameterDirection.Input;
                if (pi.IsOut && pi.IsIn == false)
                    direction = ParameterDirection.Output;
                else if (pi.IsOut && pi.IsIn)
                    direction = ParameterDirection.InputOutput;
                int? size = null;

                if (pi.ParameterType.In(typeof(string), refString))
                {
                    size = -1;
                }
                var parameter = command.AddParameter(pi.Name, item.Value, direction, size: size);

                if (pi.IsOut)
                {
                    transit.Outputs.Add(pi, parameter);
                }
            }
            command.CommandType = CommandType.StoredProcedure;
            command.CommandText = transit.CommandText;
            command.Prepare();
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
            var collection = transit.Parameters.Where(a => a.Value is ICollection).ToList();
            if (_operations.HasFlag(RepositoryOperations.UseTableValuedParameter) == false &&
                collection.Count > 1 &&
                (transit.ReturnObject is IList) == false)
            {
                throw new NotSupportedException(nameof(Resources.DA004));
            }
            PrepareExecute(transit, command);
            // if transit is List execute reader
            if (transit.ReturnObject is IList)
            {
                var items = (IList)transit.ReturnObject;
                var accessor = TypeAccessor.Create(transit.ReturnType.GenericTypeArguments[0]);
                
                if (collection.Count > 0)
                {
                    var genericType = collection[0].Value.GetType().GenericTypeArguments[0];
                    TypeAccessor listType = genericType.IsClass && genericType != typeof(string) ? TypeAccessor.Create(genericType) : null;

                    foreach (var item in collection)
                    {
                        CreateRefScalarVariable(item.Key.Name, item.Value, listType, command);
                        ExecuteReaderToList(items, accessor, command);
                    }
                }
                else
                {
                    ExecuteReaderToList(items, accessor, command);
                }
            }
            // execute scalar. Can be Struct too
            else if (transit.ReturnObject != null)
            {
                ExecuteWithSingleReturnValue(transit, command);
            }
            // ExecuteNonQuery
            else
            {
                if (collection.Count > 0)
                {
                    var genericType = collection[0].Value.GetType().GenericTypeArguments[0];
                    TypeAccessor listType = genericType.IsClass && genericType != typeof(string) ? TypeAccessor.Create(genericType) : null;

                    foreach (var item in collection)
                    {
                        CreateRefScalarVariable(item.Key.Name, item.Value, listType, command);
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
            if (transit.ReturnObject is IList)
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
        /// <summary>
        /// Use a list entry to create
        /// </summary>
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

        #region Execution Helpers
        /// <summary>
        /// Execute SP with return value as non Enumerable
        /// </summary>
        /// <param name="transit">return value as reference value</param>
        /// <param name="command">executive command</param>
        private void ExecuteWithSingleReturnValue(TransitObject transit, IDbCommand command)
        {
            if (transit.ReturnType.IsClass && transit.ReturnType != typeof(string))
            {
                _queryTime?.Start();
                using (var reader = command.ExecuteReader())
                {
                    _queryTime?.Stop();
                    int columns = reader.FieldCount;
                    var accessor = TypeAccessor.Create(transit.ReturnType);
                    if (reader.Read())
                    {
                        ExtractObject(columns, transit.ReturnObject, accessor, reader);
                    }
                }
            }
            else
            {
                transit.ReturnObject = command.ExecuteScalar();
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
            _queryTime?.Start();
            using (var reader = command.ExecuteReader())
            {
                _queryTime?.Stop();
                int columns = reader.FieldCount;
                foreach (var record in reader.ToRecord())
                {
                    var item = accessor.CreateNew();
                    items?.Add(item);
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
            _queryTime?.Start();
            command.ExecuteNonQuery();
            _queryTime?.Stop();
        }
        #endregion
    }
}
