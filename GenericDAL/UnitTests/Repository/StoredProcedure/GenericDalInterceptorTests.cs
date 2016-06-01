using Microsoft.VisualStudio.TestTools.UnitTesting;
using DalCore.Repository.StoredProcedure;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NProxy.Core;
using NSubstitute;
using Ploeh.AutoFixture;

namespace DalCore.Repository.StoredProcedure.Tests
{
    [TestClass()]
    public class GenericDalInterceptorTests
    {
        private ExecutionTest _test;
        private BasicDbAccess _dal;
        private DbCommand _realCommand;
        private IDbCommand _testCommand;
        private SomeEntity _returnValue;

        [TestInitialize]
        public void InitTest()
        {
            _dal = new BasicDbAccess();

            var factory = DbProviderFactories.GetFactory("System.Data.SqlClient");
            _realCommand = factory.CreateCommand();
            _testCommand = Substitute.For<DbCommand>();
            _testCommand.CreateParameter().Returns(a => factory.CreateParameter());
            var collection = _realCommand.Parameters;
            _testCommand.Parameters.Returns(collection);

            var connection = Substitute.For<DbConnection>();
            connection.State.Returns(ConnectionState.Open);
            connection.CreateCommand().Returns(a => _testCommand);


            _dal.Factory = Substitute.For<DbProviderFactory>();
            _dal.Factory.CreateConnection().Returns(connection);
            GenericDalInterceptor.AccessLayer = _dal;
            _test = new ProxyFactory().CreateProxy<ExecutionTest>(Type.EmptyTypes, new GenericDalInterceptor { UseTvp = true });


            var reader = Substitute.For<DbDataReader>();
            reader.FieldCount.Returns(2);
            int counter = 1;
            reader.Read().Returns(a => counter-- > 0);
            _testCommand.ExecuteReader().Returns(reader);

            var fixture = new Fixture();
            _returnValue = fixture.Create<SomeEntity>();

            reader.GetValue(0).Returns(_returnValue.Id);
            reader.GetValue(1).Returns(_returnValue.Remark);
            reader.GetName(0).Returns(nameof(SomeEntity.Id));
            reader.GetName(1).Returns(nameof(SomeEntity.Remark));
            reader.IsDBNull(Arg.Any<int>()).ReturnsForAnyArgs(false);
            // DbEnumerator
            reader.GetFieldType(0).Returns(typeof(int));
            reader.GetFieldType(1).Returns(typeof(string));
            reader.GetDataTypeName(0).Returns("int");
            reader.GetDataTypeName(1).Returns("nvarchar(max)");
            reader.GetValues(Arg.Any<object[]>()).Returns(a =>
            {
                var x = a[0] as object[];
                x[0] = _returnValue.Id;
                x[1] = _returnValue.Remark;
                return 2;
            });
        }

        [TestCleanup]
        public void Cleanup()
        {
            _realCommand?.Dispose();
        }

        [TestMethod()]
        public void GetTest()
        {

            var result = _test.Get(1);

            Assert.AreEqual(_returnValue.Id, result.Id);
            Assert.AreEqual(_returnValue.Remark, result.Remark);
            Assert.AreEqual(nameof(ExecutionTest.Get), this._testCommand.CommandText);
        }

        [TestMethod()]
        public void SaveTest()
        {
            var x = new List<SomeEntity> { _returnValue };
            _test.Save(x);

            Assert.AreEqual(nameof(ExecutionTest.Save), this._testCommand.CommandText);

            _testCommand.Received(1);
        }

        [TestMethod()]
        public void ReadTest()
        {
            var x = _test.Read();
            var result = x[0];
            Assert.AreEqual(_returnValue.Id, result.Id);
            Assert.AreEqual(_returnValue.Remark, result.Remark);

            Assert.AreEqual(nameof(ExecutionTest.Read), this._testCommand.CommandText);
        }

        [TestMethod]
        public void UpdateTest()
        {
            var x = _test.Update(_returnValue.Id, _returnValue.Remark);

            Assert.AreEqual(_returnValue.Id, x.Id);
            Assert.AreEqual(_returnValue.Remark, x.Remark);
        }

        public class SomeEntity
        {
            public int Id { get; set; }

            public string Remark { get; set; }
        }

        public interface ExecutionTest : IRepository
        {
            SomeEntity Get(int Id);
            void Save(List<SomeEntity> items);
            List<SomeEntity> Read();
            SomeEntity Update(int id, string remark);
        }
    }
}