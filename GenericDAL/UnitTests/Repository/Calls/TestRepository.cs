using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GenericDataAccessLayer.LazyDal;

namespace UnitTests.Repository.Calls
{
    public interface TestRepository : IRepository
    {
        #region CRUD
        void CreateUseRef(ref User data);
        User CreateUseReturn(User data);
        User[] UpdateUseArrayReturn(List<User> data);
        IEnumerable<User> Read();
        void Delete(IEnumerable<User> data);

        #endregion
    }
}
