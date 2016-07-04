using System.Collections.Generic;
using GenericDataAccessLayer.LazyDal;

namespace UnitTests.Repository.StoredProcedure
{
    public interface ExecutionTest : IRepository
    {
        SomeEntity GetSomeEntity(int Id);
        SomeEntity DeleteSomeEntity(int Id);
        void SaveSomeEntities(List<SomeEntity> items);
        List<SomeEntity> ReadSomeEntities();
        SomeEntity UpdateSomeEntity(int id, string remark);
    }
}
