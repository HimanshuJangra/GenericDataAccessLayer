using NProxy.Core;
using System;
using GenericDataAccessLayer.LazyDal.StoredProcedure;

namespace GenericDataAccessLayer.LazyDal.Repository
{
    /// <summary>
    /// Contains Interceptor implementation
    /// </summary>
    public static class DynamicRepository
    {
        /// <summary>
        /// Create new dynamic repository accessor.
        /// If you need performance, dont use it!
        /// </summary>
        /// <typeparam name="TRepository">Dynamic repository Interface, that contains only definition of the Stored Procedure</typeparam>
        /// <returns>Intercepted Repository</returns>
        public static TRepository Create<TRepository>()
            where TRepository : class, IRepository
        {
            return new ProxyFactory().CreateProxy<TRepository>(Type.EmptyTypes, new GenericDalInterceptor());
        }
    }
}
