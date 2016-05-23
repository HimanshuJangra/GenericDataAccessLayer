using NProxy.Core;
using System;
using DalCore.Repository.StoredProcedure;

namespace DalCore.Repository
{
    public static class RepositoryOperator
    {
        public static TRepository ToList<TRepository>()
            where TRepository : class, IRepository
        {
            return new ProxyFactory().CreateProxy<TRepository>(Type.EmptyTypes, new SpGetListHandler());
        }
    }
}
