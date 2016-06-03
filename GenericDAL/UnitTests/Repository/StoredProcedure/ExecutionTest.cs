using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DalCore.Repository;

namespace UnitTests.Repository.StoredProcedure
{
    public interface ExecutionTest : IRepository
    {
        SomeEntity GetSomeEntity(int Id);
        void SaveSomeEntities(List<SomeEntity> items);
        List<SomeEntity> ReadSomeEntities();
        SomeEntity UpdateSomeEntity(int id, string remark);
    }
}
