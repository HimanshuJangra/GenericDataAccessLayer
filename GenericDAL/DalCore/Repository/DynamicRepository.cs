using NProxy.Core;
using System;
using DalCore.Repository.StoredProcedure;

namespace DalCore.Repository
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
        /// <param name="useTvp">If true, any Collection will be converted to a DataTable</param>
        /// <returns>Intercepted Repository</returns>
        public static TRepository CreateDynamic<TRepository>(bool useTvp = true)
            where TRepository : class, IRepository
        {
            return new ProxyFactory().CreateProxy<TRepository>(Type.EmptyTypes, new GenericDalInterceptor { UseTvp = useTvp });
        }
    }
}
