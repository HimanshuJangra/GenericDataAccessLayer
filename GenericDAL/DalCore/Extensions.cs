using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using GenericDataAccessLayer.Core.SingleItemProcessor;
using SharedComponents.Data;

namespace GenericDataAccessLayer.Core
{
    static partial class Extensions
    {
        public static void IntoList<T>(this List<T> result, IDataReader reader, Action<T, IDataRecord> eachItem)
            where T : class, IDataTransferObject
        {
            var dal = FastAccess.Instance.GetDal<T>();
            foreach (var record in reader.ToRecord())
            {
                T item = (T)dal.Get(record);
                eachItem(item, record);
                result.Add(item);
            }
        }

        public static void Save<T>(this T data)
            where T : class, IDataTransferObject
        {
            var instance = FastAccess.Instance.GetDal<T>();
            instance.Save(data);
        }

        public static void Save<T>(this List<T> data)
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

        public static T GetDal<T>()
            where T : class, IDataAccessLayer
        {
            return FastAccess.Instance.GetConcreteDal<T>();
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
    }
}
