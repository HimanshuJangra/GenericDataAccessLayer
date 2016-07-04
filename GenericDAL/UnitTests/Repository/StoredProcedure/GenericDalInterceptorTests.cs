using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using GenericDataAccessLayer.LazyDal.StoredProcedure;
using NProxy.Core;
using NSubstitute;
using Ploeh.AutoFixture;
using UnitTests.Repository.StoredProcedure;

namespace GenericDataAccessLayer.Core.Repository.StoredProcedure.Tests
{
    [TestClass()]
    public class GenericDalInterceptorTests
    {
        private ExecutionTest _test;
        private DbCommand _realCommand;
        private IDbCommand _testCommand;
        private SomeEntity _returnValue;

        [TestInitialize]
        public void InitTest()
        {
            var factory = DbProviderFactories.GetFactory("System.Data.SqlClient");
            _realCommand = factory.CreateCommand();
            _testCommand = Substitute.For<DbCommand>();
            _testCommand.CreateParameter().Returns(a => factory.CreateParameter());
            _testCommand.Parameters.Returns(a=> _realCommand.Parameters);

            var connection = Substitute.For<DbConnection>();
            connection.State.Returns(ConnectionState.Open);
            connection.CreateCommand().Returns(a => _testCommand);

            
            _test = new ProxyFactory().CreateProxy<ExecutionTest>(Type.EmptyTypes, new GenericDalInterceptor());
            _test.Operations = LazyDal.RepositoryOperations.All;
            _test.Connection = connection;


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
        /// <summary>
        /// Testcontext auf dem man eventuell zugreifen möchte
        /// </summary>
        public TestContext TestContext { get; set; }

        [TestCleanup]
        public void Cleanup()
        {
            System.Console.WriteLine($"MIR: Query Time Execution: {_test.QueryExecutionTime} ticks, {_test.QueryExecutionTime / System.TimeSpan.TicksPerMillisecond} ms");
            System.Console.WriteLine($"MIR: Total Time Execution: {_test.TotalExecutionTime} ticks, {_test.TotalExecutionTime / System.TimeSpan.TicksPerMillisecond} ms");
            BasicDbAccess.DisposeConnection();
            _realCommand?.Dispose();
        }

        [TestMethod()]
        public void GetSomeEntityTest()
        {
            _test.ConnectionStringSettings = "test";
            var result = _test.GetSomeEntity(1);

            Assert.AreEqual(_returnValue.Id, result.Id);
            Assert.AreEqual(_returnValue.Remark, result.Remark);
            Assert.AreEqual(nameof(ExecutionTest.GetSomeEntity), this._testCommand.CommandText);
        }

        [TestMethod()]
        public void SaveSomeEntitiesTest()
        {
            var x = new List<SomeEntity> { _returnValue };
            _test.SaveSomeEntities(x);

            Assert.AreEqual(nameof(ExecutionTest.SaveSomeEntities), this._testCommand.CommandText);

            _testCommand.Received(1);
        }

        [TestMethod()]
        public void ReadSomeEntitiesTest()
        {
            var x = _test.ReadSomeEntities();
            var result = x[0];
            Assert.AreEqual(_returnValue.Id, result.Id);
            Assert.AreEqual(_returnValue.Remark, result.Remark);

            Assert.AreEqual(nameof(ExecutionTest.ReadSomeEntities), this._testCommand.CommandText);
        }

        [TestMethod]
        public void UpdateSomeTest()
        {
            var x = _test.UpdateSomeEntity(_returnValue.Id, _returnValue.Remark);

            Assert.AreEqual(_returnValue.Id, x.Id);
            Assert.AreEqual(_returnValue.Remark, x.Remark);
        }
    }
}