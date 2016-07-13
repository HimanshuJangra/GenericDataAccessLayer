using System.Collections;
using System.Collections.Generic;
using GenericDataAccessLayer.LazyDal;

namespace UnitTests.Repository.StoredProcedure
{
    public interface ExecutionTest : IRepository
    {
        SomeEntity GetSomeEntity(int Id);
        void DeleteSomeEntity(int Id);
        void SaveSomeEntities(List<SomeEntity> items);
        List<SomeEntity> ReadSomeEntities();
        SomeEntity UpdateSomeEntity(int id, string remark);

        List<SomeEntity> FilterForSome(IEnumerable include, IEnumerable exclude);
    }
}
