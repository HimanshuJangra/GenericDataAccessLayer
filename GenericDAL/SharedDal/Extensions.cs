using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using SharedComponents.Data;
using SharedDal.SingleItemProcessor;
using Localization = L10n.Properties.Resources;

namespace SharedDal
{
    /// <summary>
    /// Some usefull database and dal extensions
    /// </summary>
    internal static class Extensions
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
        /// <returns>Created or updated parameter</returns>
        public static IDbDataParameter AddParameter<T>(this IDbCommand command, String parameterName, T value = default(T), ParameterDirection direction = ParameterDirection.Input, DbType? type = null)
        {
            IDbDataParameter parameter = (command.Parameters as IList).OfType<IDbDataParameter>().FirstOrDefault(a => a.ParameterName == parameterName);
            if (parameter == null)
            {
                parameter = command.CreateParameter();
                parameter.ParameterName = parameterName;
                command.Parameters.Add(parameter);
            }
            parameter.Value = value;
            parameter.Direction = direction;
            if (type != null)
            {
                parameter.DbType = type.Value;
            }

            return parameter;
        }

        public static T ReadObject<T>(this IDataReader reader, int ordinal, T defaultValue = default(T))
        {
            T result = defaultValue;
            try
            {
                if (reader.IsDBNull(ordinal) == false)
                {
                    result = (T)reader.GetValue(ordinal);
                }
            }
            catch (Exception e)
            {
                throw new Exception(String.Format(Localization.DataReaderColumnException, ordinal), e);
            }

            return result;
        }

        public static T ReadObject<T>(this IDataReader reader, String columnName, T defaultValue = default(T))
        {
            int ordinal = reader.GetOrdinal(columnName);
            return reader.ReadObject<T>(ordinal, defaultValue);
        }

        public static DataRow AddCellValue(this System.Data.DataRow row, String name, Object value)
        {
            row[name] = value ?? DBNull.Value;
            return row;
        }

        public static DataColumn AddColumn<T>(this System.Data.DataTable table, String name)
        {
            var column = table.Columns.Add(name, typeof(T));
            column.AllowDBNull = true;

            return column;
        }

        public static void IntoList<T>(this IDbCommand command, Func<IDataReader, T> getter, List<T> result)
        {
            using (var reader = command.ExecuteReader())
            {
                while (reader.Read() == true)
                {
                    result.Add(getter(reader));
                }
            }
        }
        
        public static void IntoList<T>(this List<T> result, IDataReader reader, Action<T, IDataReader> eachItem = null)
            where T : class, IDataTransferObject
        {
            var dal = FastAccess.Instance.GetDal<T>();
            while (reader.Read() == true)
            {
                T item = (T)dal.Get(reader);
                if (eachItem != null)
                {
                    eachItem(item, reader);
                }
                result.Add(item);
            }
        }

        public static int GetFirstRow<T>(this IDbCommand command, Action<T, IDataReader> getter, T data)
        {
            int result;
            using (var reader = command.ExecuteReader())
            {
                while (reader.Read() == true)
                {
                    getter(data, reader);
                }
                result = reader.RecordsAffected;
            }
            return result;
        }

        public static T GetFirstRow<T>(this IDbCommand command, Func<IDataReader, T> getter)
        {
            T result = default(T);
            using (var reader = command.ExecuteReader())
            {
                if (reader.Read() == true)
                {
                    result = getter(reader);
                }
            }
            return result;
        }

        public static void Update<T>(this IDbCommand command, T item, Action<T, IDataReader> getter)
        {
            using (var reader = command.ExecuteReader())
            {
                if (reader.Read() == true)
                {
                    getter(item, reader);
                }
            }
        }

        public static void Update<T>(this IDbCommand command, List<T> items, Action<T, IDataReader> getter, String indexName)
        {
            using (var reader = command.ExecuteReader())
            {
                while (reader.Read() == true)
                {
                    int index = reader.ReadObject<int>(indexName);
                    getter(items[index], reader);
                }
            }
        }

        public static void Save<T>(this T data)
            where T : class, IDataTransferObject
        {
            var instance = FastAccess.Instance.GetDal<T>();
            instance.Save(data);
        }

        public static void Read<T>(this List<T> result)
            where T : class, IDataTransferObject
        {
            var temp = new List<IDataTransferObject>();
            FastAccess.Instance.GetDal<T>().Read(temp);
            result.AddRange(temp.OfType<T>());
        }

        public static List<T> Read<T>()
            where T : class, IDataTransferObject
        {
            var temp = new List<IDataTransferObject>();
            FastAccess.Instance.GetDal<T>().Read(temp);
            return temp.OfType<T>().ToList();
        }

        public static IDataAccessLayer Get<T>(this T filter)
            where T : class, IDataTransferObject
        {
            var instance = FastAccess.Instance.GetDal<T>();
            instance.Get(filter);

            return instance;
        }

        public static TDal GetDal<T, TDal>()
            where T : class, IDataTransferObject
            where TDal : class, IDataAccessLayer
        {
            return FastAccess.Instance.GetDal<T>() as TDal;
        }

        public static IDataAccessLayer Load<TDc>(this TDc item)
            where TDc : IDataTransferObject
        {
            var dal = FastAccess.Instance.GetDal<TDc>();
            dal.Get(item);
            return dal;
        }

        public static Boolean Update<TDc>(this TDc item)
            where TDc : IDataTransferObject
        {
            var dal = FastAccess.Instance.GetDal<TDc>();

            return dal.Update(item) > 0;
        }

        public static Boolean Create<TDc>(this TDc item)
            where TDc : IDataTransferObject
        {
            var dal = FastAccess.Instance.GetDal<TDc>();
            return dal.Create(item) > 0;
        }

        public static Boolean Delete<TDc>(this TDc keyHolder)
            where TDc : IDataTransferObject
        {
            var dal = keyHolder.Load();
            return dal.Delete(keyHolder) > 0;
        }
    }
}
