using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using Localization = L10n.Properties.Resources;

namespace GenericDataAccessLayer.Core
{
    /// <summary>
    /// Some usefull database and dal extensions
    /// </summary>
    public static partial class Extensions
    {
        /// <summary>
        /// Add new parameter. Use command to create new or a lookup for existing parameter with override options
        /// </summary>
        /// <typeparam name="T">Parameter Type that should added</typeparam>
        /// <param name="command">Using command to create new parameter and add it instantly to parameter list of command</param>
        /// <param name="parameterName">Database parameter name</param>
        /// <param name="value">value that should pass to database</param>
        /// <param name="direction">optional value for direction. Default value is input. Can be used return or output parameter required</param>
        /// <param name="type">optional database type value. Dont use </param>
        /// <param name="size">max length of current value</param>
        /// <returns>Created or updated parameter</returns>
        public static IDbDataParameter AddParameter<T>(this IDbCommand command, String parameterName, T value = default(T),
            ParameterDirection direction = ParameterDirection.Input, DbType? type = null, int? size = null)
        {
            // more efficient as IndexOf or Contains
            IDbDataParameter parameter = command.Parameters.OfType<IDbDataParameter>().FirstOrDefault(a => a.ParameterName == parameterName);
            if (parameter == null)
            {
                parameter = command.CreateParameter();
                parameter.ParameterName = parameterName;
                command.Parameters.Add(parameter);
            }
            parameter.Value = value;
            parameter.Direction = direction;

            if (size != null)
            {
                parameter.Size = size.Value;
            }
            if (type != null)
            {
                parameter.DbType = type.Value;
            }

            return parameter;
        }
        /// <summary>
        /// Get Value from Reader and convert it into Type given by generic parameter
        /// </summary>
        /// <typeparam name="T">Type of the Column value</typeparam>
        /// <param name="reader">Reader that we use to retrieve data</param>
        /// <param name="ordinal">Column Number</param>
        /// <param name="defaultValue">default Value if reader value is DbNull</param>
        /// <returns>Column value</returns>
        public static T ReadObject<T>(this IDataRecord reader, int ordinal, T defaultValue = default(T))
        {
            T result = defaultValue;
            object value = reader.ReadObject(ordinal);
            if (value != null)
            {
                result = (T)value;
            }
            return result;
        }

        public static object ReadObject(this IDataRecord reader, int ordinal)
        {
            object result = null;
            try
            {
                if (reader.IsDBNull(ordinal) == false)
                {
                    result = reader.GetValue(ordinal);
                }
            }
            catch
            {
                throw new ArgumentOutOfRangeException(nameof(reader), ordinal, nameof(Localization.CE001));
            }

            return result;
        }

        /// <summary>
        /// Get Value from Reader and convert it into Type given by generic parameter using column name
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="reader"></param>
        /// <param name="columnName"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public static T ReadObject<T>(this IDataRecord reader, String columnName, T defaultValue = default(T))
        {
            int ordinal = reader.GetOrdinal(columnName);
            return reader.ReadObject(ordinal, defaultValue);
        }

        public static DataRow AddCellValue(this DataRow row, String name, Object value)
        {
            row[name] = value ?? DBNull.Value;
            return row;
        }

        public static DataColumn AddColumn<T>(this DataTable table, String name)
        {
            var column = table.Columns.Add(name, typeof(T));
            column.AllowDBNull = true;

            return column;
        }

        public static void IntoList<T>(this IDbCommand command, Func<IDataRecord, T> getter, List<T> result)
        {
            using (var reader = command.ExecuteReader())
            {
                result.AddRange(reader.ToRecord().Select(getter));
            }
        }
        /// <summary>
        /// Create DbEnumerator and returns IDataRecord for each iteration.
        /// DbEnumerator is a bit faster then IDataReader... using cache for column names
        /// </summary>
        /// <param name="reader">Reader to use</param>
        /// <returns>Enumerable Data Records</returns>
        public static IEnumerable<IDataRecord> ToRecord(this IDataReader reader)
        {
            var enumerator = new DbEnumerator(reader);
            while (enumerator.MoveNext())
            {
                yield return (IDataRecord)enumerator.Current;
            }
        }

        public static int GetFirstRow<T>(this IDbCommand command, Action<T, IDataRecord> getter, T data)
        {
            int result;
            using (var reader = command.ExecuteReader())
            {
                foreach (var record in reader.ToRecord())
                {
                    getter(data, record);
                }
                result = reader.RecordsAffected;
            }
            return result;
        }

        public static T GetFirstRow<T>(this IDbCommand command, Func<IDataRecord, T> getter)
        {
            T result = default(T);
            using (var reader = command.ExecuteReader())
            {
                foreach (var record in reader.ToRecord())
                {
                    result = getter(record);
                }
            }
            return result;
        }

        public static void Update<T>(this IDbCommand command, T item, Action<T, IDataRecord> getter)
        {
            using (var reader = command.ExecuteReader())
            {
                foreach (var record in reader.ToRecord())
                {
                    getter(item, record);
                }
            }
        }

        public static void Update<T>(this IDbCommand command, List<T> items, Action<T, IDataRecord> getter, String indexName)
        {
            using (var reader = command.ExecuteReader())
            {
                foreach (var record in reader.ToRecord())
                {
                    int index = record.ReadObject<int>(indexName);
                    getter(items[index], record);
                }
            }
        }
    }
}
