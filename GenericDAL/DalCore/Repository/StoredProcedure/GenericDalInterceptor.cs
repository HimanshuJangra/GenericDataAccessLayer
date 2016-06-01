using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using DalCore.DatabaseAccess;
using FastMember;
using L10n.Properties;
using NProxy.Core;
using SharedComponents;

namespace DalCore.Repository.StoredProcedure
{
    /// <summary>
    /// Rules 
    /// <para>Read</para>
    /// Read data using stored procedure should return List with underlying generic type of current entity
    /// <para>Update</para>
    /// </summary>
    public class GenericDalInterceptor : IInvocationHandler
    {
        private static IBasicDbAccess _accessLayer;
        private static readonly object Sync = new object();
        /// <summary>
        /// Basic DAL Operator that help us handle commands. If not manually set, will be created by default
        /// </summary>
        public static IBasicDbAccess AccessLayer
        {
            get
            {
                if (_accessLayer == null)
                {
                    lock (Sync)
                    {
                        if (_accessLayer == null)
                            _accessLayer = new BasicDbAccess();
                    }
                }

                return _accessLayer;
            }
            set
            {
                _accessLayer = value;
            }
        }
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
            public Dictionary<ParameterInfo, IDataParameter> Outputs;

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
        /// If true, all list will be mapped as Table Value parameters
        /// </summary>
        public bool UseTvp;

        /// <summary>
        /// Initialize some vital properties for SP execution
        /// </summary>
        /// <param name="transit">Placeholder for internal parameter use</param>
        /// <param name="methodInfo">Method Information</param>
        /// <param name="parameters">Parameters from execution code</param>
        /// <returns></returns>
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
            var transit = new TransitObject();
            this.PrepareInitialization(transit, methodInfo, parameters);

            AccessLayer.Execute(Execute, transit, null);

            // write output parameters
            foreach (var item in transit.Outputs)
            {
                parameters[item.Key.Position] = item.Value.Value;
            }

            return transit.ReturnObject;
        }

        /// <summary>
        /// Create new TVP is interceptor configured for Table Valued Parameters
        /// </summary>
        /// <param name="parameterName">Property name used as TVP name</param>
        /// <param name="command">Using DbCommand handle db parameter value</param>
        /// <param name="value">ICollection that should pass to DB</param>
        /// <param name="index">Extract from index, if not TVP</param>
        /// <returns>If true, skip scalar parameter created</returns>
        private bool CreateNewDataTableParameter(string parameterName, IDbCommand command, object value, ref int index)
        {
            bool result = false;
            if (value is IEnumerable && (value is string || value.GetType() == Type.GetType("System.String&")) == false)
            {
                IEnumerable data = value as IEnumerable;
                var dataType = data.GetType().GenericTypeArguments[0];
                var tAccessor = TypeAccessor.Create(dataType);

                if (UseTvp == true)
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
                        if (column.Type != typeof(string) && column.Type.IsClass == true)
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
                    result = true;
                }
                else
                {
                    object dataValue = null;
                    int internalIndex = 0;
                    bool found = false;
                    foreach (var item in data)
                    {
                        if (internalIndex == index)
                        {
                            found = true;
                            dataValue = item;
                            break;
                        }
                        internalIndex++;
                    }

                    if (found == false)
                    {
                        index = -1;
                    }
                    else
                    {
                        // allow only Class to be interpreted, except string
                        if (dataType.IsClass == true && (dataType == typeof(string)) == false)
                        {
                            foreach (var item in tAccessor.GetMembers())
                            {
                                command.AddParameter(item.Name, tAccessor[dataValue, item.Name]);
                            }
                        }
                        else
                        {
                            command.AddParameter(parameterName, dataValue);
                        }
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Pre-Execution Process that allow create stored Procedure parameters
        /// </summary>
        /// <param name="transit">placeholder that contains sp parameters</param>
        /// <param name="command">command we used to store parameters</param>
        /// <param name="index">Extract from index, if not TVP</param>
        private void PrepareExecute(TransitObject transit, IDbCommand command, ref int index)
        {
            transit.Outputs = new Dictionary<ParameterInfo, IDataParameter>();
            Type refString = Type.GetType("System.String&");
            // fill the parameters
            foreach (var item in transit.Parameters)
            {
                ParameterInfo pi = item.Key;
                // true => exists TVP
                if (CreateNewDataTableParameter(pi.Name, command, item.Value, ref index) == false)
                {
                    ParameterDirection direction = ParameterDirection.Input;
                    if (pi.IsOut == true && pi.IsIn == false)
                        direction = ParameterDirection.Output;
                    else if (pi.IsOut == true && pi.IsIn == true)
                        direction = ParameterDirection.InputOutput;
                    int? size = null;

                    if (pi.ParameterType.In(typeof(string), refString))
                    {
                        size = -1;
                    }
                    var parameter = command.AddParameter(pi.Name, item.Value, direction, size: size);

                    if (pi.IsOut == true)
                    {
                        transit.Outputs.Add(pi, parameter);
                    }
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
            if (UseTvp == true)
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
            if (UseTvp == false &&
                transit.Parameters.Count(a => a.Value is IEnumerable) > 1 &&
                transit.Parameters.Count(a => (a.Value is IEnumerable) == false) > 0)
            {
                throw new NotSupportedException(nameof(Resources.DA004));
            }
            int index = 0;
            // if transit is List execute reader
            if (transit.ReturnObject is IList)
            {
                var items = transit.ReturnObject as IList;
                var accessor = TypeAccessor.Create(transit.ReturnType.GenericTypeArguments[0]);

                do
                {
                    PrepareExecute(transit, command, ref index);

                    using (var reader = command.ExecuteReader())
                    {
                        int columns = reader.FieldCount;
                        foreach (var record in reader.ToRecord())
                        {
                            var item = accessor.CreateNew();
                            items?.Add(item);
                            ExtractObject(columns, item, accessor, record);
                        }
                    }
                }
                while (index >= 0);
            }
            // execute scalar. Can be Struct too
            else if (transit.ReturnObject != null)
            {
                var accessor = TypeAccessor.Create(transit.ReturnType);
                do
                {
                    PrepareExecute(transit, command, ref index);
                    if (transit.ReturnType.IsClass == true && transit.ReturnType != typeof(string))
                    {
                        using (var reader = command.ExecuteReader())
                        {
                            int columns = reader.FieldCount;
                            if (reader.Read() == true)
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
                while (index >= 0);
            }
            // ExecuteNonQuery
            else
            {
                do
                {
                    PrepareExecute(transit, command, ref index);
                    command.ExecuteNonQuery();
                }
                while (index >= 0);
            }
        }
        /// <summary>
        /// Execute if TVP is activated
        /// </summary>
        /// <param name="transit">return value as reference value</param>
        /// <param name="command">executive command</param>
        private void ExecuteTvp(TransitObject transit, IDbCommand command)
        {
            int index = -1;
            PrepareExecute(transit, command, ref index);
            // if transit is List execute reader
            if (transit.ReturnObject is IList)
            {
                var items = transit.ReturnObject as IList;
                var accessor = TypeAccessor.Create(transit.ReturnType.GenericTypeArguments[0]);
                using (var reader = command.ExecuteReader())
                {
                    int columns = reader.FieldCount;
                    foreach (var record in reader.ToRecord())
                    {
                        var item = accessor.CreateNew();
                        items?.Add(item);
                        ExtractObject(columns, item, accessor, record);
                    }
                }
            }
            // execute scalar. Can be Struct too
            else if (transit.ReturnObject != null)
            {
                if (transit.ReturnType.IsClass == true)
                {
                    using (var reader = command.ExecuteReader())
                    {
                        int columns = reader.FieldCount;
                        var accessor = TypeAccessor.Create(transit.ReturnType);
                        if (reader.Read() == true)
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
            // ExecuteNonQuery
            else
            {
                command.ExecuteNonQuery();
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
    }
}
