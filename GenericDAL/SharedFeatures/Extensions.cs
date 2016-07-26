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
    }
}
