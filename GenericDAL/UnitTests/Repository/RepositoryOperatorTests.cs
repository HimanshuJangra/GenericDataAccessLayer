using Microsoft.VisualStudio.TestTools.UnitTesting;
using DalCore.Repository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization;
using FastMember;

namespace DalCore.Repository.Tests
{
    [TestClass()]
    public class RepositoryOperatorTests
    {

        TypeAccessor accessor = TypeAccessor.Create(typeof(Entity));

        [TestMethod()]
        public void ToListTest()
        {
            var result = RepositoryOperator.ToList<RepoTest>().ReadEntity(2);
            Assert.AreEqual(4, result.Count);
        }

        [TestMethod()]
        public void GetSomeOut()
        {
            int id;
            string test;
            var result = RepositoryOperator.ToList<RepoTest>().GetSomeOut(out test, out id);
            Assert.IsTrue(id == result.FirstOrDefault()?.Id);
            Assert.AreEqual("OK", test);
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

        public class Entity
        {
            public int Id { get; set; }
            public String Name { get; set; }
            public String Remark { get; set; }
        }

        public interface RepoTest : IRepository
        {
            List<Entity> ReadEntity(int Id);
            List<Entity> ReadRefEntity(ref int id);
            List<Entity> GetSomeOut(out string test, out int id);
        }
    }
}