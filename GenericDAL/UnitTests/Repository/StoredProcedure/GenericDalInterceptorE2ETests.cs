using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Transactions;
using DalCore;
using DalCore.Repository;
using DalCore.Repository.StoredProcedure;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnitTests.Repository.StoredProcedure
{
    [TestClass]
    public class GenericDalInterceptorE2ETests
    {
        private ExecutionTest _testTvp = DynamicRepository.CreateDynamic<ExecutionTest>(true);
        private ExecutionTest _test = DynamicRepository.CreateDynamic<ExecutionTest>(false);
        /// <summary>
        /// Testcontext auf dem man eventuell zugreifen möchte
        /// </summary>
        public TestContext TestContext { get; set; }

        [TestCleanup]
        public void Cleanup()
        {
            // force close connection
            BasicDbAccess.DisposeConnection();
        }

        [TestInitialize]
        public void Init()
        {
            // Since Unit tests override the default Db Access, we have to restore the default for each test.
            GenericDalInterceptor.AccessLayer = new BasicDbAccess();
        }

        [TestMethod()]
        public void GetSomeEntityTest()
        {
            var result = _testTvp.GetSomeEntity(1);
            Assert.AreEqual("Test 1", result.Remark);

            result = _test.GetSomeEntity(2);
            Assert.AreEqual("Yes we can", result.Remark);
        }

        [TestMethod()]
        public void SaveSomeEntitiesTVPTest()
        {

            using (var scope = new TransactionScope())
            {
                var newItem1 = new SomeEntity { Id = 4, Remark = "Test 2" };
                var newItem2 = new SomeEntity { Id = 5, Remark = "Test 2" };

                var x = new List<SomeEntity> { newItem1, newItem2 };
                _testTvp.SaveSomeEntities(x);

                var result = _test.GetSomeEntity(4);
                Assert.AreEqual(newItem1.Remark, result.Remark);
                result = _test.GetSomeEntity(5);
                Assert.AreEqual(newItem2.Remark, result.Remark);
            }
        }

        [TestMethod()]
        public void ReadSomeEntitiesTest()
        {
            var x = _test.ReadSomeEntities();
            Assert.AreEqual(3, x.Count);
        }

        [TestMethod]
        public void UpdateSomeTest()
        {
            var x = _test.GetSomeEntity(1);
            using (var scope = new TransactionScope())
            {
                var update = new SomeEntity { Id = 1, Remark = "Test 2" };
                var result = _test.UpdateSomeEntity(update.Id, update.Remark);

                Assert.AreEqual(update.Remark, result.Remark);
                Assert.IsTrue(result.Updated);
                Assert.AreNotEqual(x.Remark, result.Remark);
            }
        }
    }
}
