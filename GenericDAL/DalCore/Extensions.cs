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
    }
}
