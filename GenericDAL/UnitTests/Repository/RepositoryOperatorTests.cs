using Microsoft.VisualStudio.TestTools.UnitTesting;
using DalCore.Repository;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization;
using System.Transactions;
using FastMember;
using L10n.Properties;
using Ploeh.AutoFixture;

namespace DalCore.Repository.Tests
{
    [TestClass()]
    public class RepositoryOperatorTests
    {
        TypeAccessor accessor = TypeAccessor.Create(typeof(Entity));

        [TestMethod()]
        public void ToListTest()
        {
            var result = DynamicRepository.CreateDynamic<RepoTest>().ReadEntity(2);
            Assert.AreEqual(4, result.Count);
        }

        [TestMethod()]
        public void InsertEntity()
        {
            using (new TransactionScope())
            {
                //var items = new Fixture().Create<List<Entity>>();
                var result = new List<Entity>
                {
                    new Entity { Name = "Eurika" },
                    new Entity { Name = "na suppa", Remark = "keine suppe" }
                };
                DynamicRepository.CreateDynamic<RepoTest>().InsertEntity(result);
                Assert.AreEqual(2, result.Count);
                Assert.IsFalse(result.Any(a => a.Id == 0));
            }
        }

        [TestMethod()]
        public void GetSomeOut()
        {
            int id;
            string test;
            var result = DynamicRepository.CreateDynamic<RepoTest>().GetSomeOut(out test, out id);
            Assert.IsNotNull(test);
            Assert.AreEqual(id, result.Count);
        }


        int range = 1000000;
        [TestMethod]
        public void PerfTestCreateInstance()
        {
            foreach (var i in Enumerable.Range(0, range))
            {
                var obj = accessor.CreateNew();
            }
        }

        [TestMethod]
        public void PerfTestCreateInstance2()
        {
            foreach (var i in Enumerable.Range(0, range))
            {
                var obj = Activator.CreateInstance(typeof(Entity));
            }
        }

        [TestMethod]
        public void PerfTestCreateInstance3()
        {
            foreach (var i in Enumerable.Range(0, range))
            {
                var obj = new Entity();
            }
        }

        private class Nullables
        {
            public int? Value1 { get; set; }
            public Entity Entity { get; set; }
        }

        public class Entity
        {
            public int Id { get; set; }
            public String Name { get; set; }
            public String Remark { get; set; }
        }

        public interface RepoTest : IRepository
        {
            List<Entity> ReadEntity(int Id);
            void InsertEntity(List<Entity> items);
            List<Entity> GetSomeOut(out string test, out int id);
        }
    }
}