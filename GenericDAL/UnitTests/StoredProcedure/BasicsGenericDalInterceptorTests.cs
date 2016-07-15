using Microsoft.VisualStudio.TestTools.UnitTesting;
using GenericDataAccessLayer.LazyDal.StoredProcedure;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GenericDataAccessLayer.LazyDal.Repository;
using NSubstitute;
using UnitTests.Repository.StoredProcedure;
using NSubstitute.ExceptionExtensions;
using Ploeh.AutoFixture;

namespace GenericDataAccessLayer.LazyDal.StoredProcedure.Tests
{
    [TestClass()]
    public class BasicsGenericDalInterceptorTests
    {
        /// <summary>
        /// change only, without initializing connection
        /// </summary>
        [TestMethod]
        public void ChangeConnectionSettings_1()
        {
            using (var repository = DynamicRepository.Create<ExecutionTest>())
            {
                string conSettings = "Test";
                repository.ConnectionStringSettings = conSettings;
                Assert.AreEqual(conSettings, repository.ConnectionStringSettings);
            }
        }
        /// <summary>
        /// change only, without initializing connection
        /// </summary>
        [TestMethod]
        public void ChangeConnectionSettings_2()
        {
            string conSettings = "Test";
            using (ExecutionTest repository = DynamicRepository.Create<ExecutionTest>())
            {
                var connection = repository.Connection;
                repository.ConnectionStringSettings = conSettings;
                Assert.AreEqual(conSettings, repository.ConnectionStringSettings);
            }
        }


        [TestMethod]
        public void Operations()
        {
            var repository = DynamicRepository.Create<ExecutionTest>();
            Assert.AreEqual(repository.Operations, RepositoryOperations.None);
            repository.Operations = RepositoryOperations.TimeLoggerOnly;
            Assert.AreEqual(repository.QueryExecutionTime, 0);
            Assert.AreEqual(repository.TotalExecutionTime, 0);

            repository.Operations = RepositoryOperations.None;
            Assert.IsNull(repository.QueryExecutionTime);
            Assert.IsNull(repository.TotalExecutionTime);
        }

        public interface MockRepository : IRepository
        {
            void Init(out int test, ref string data);
            void Some();
            void SomeComplex(IEnumerable data, SomeEntity single, IEnumerable crack, int id);
            List<SomeEntity> Save(IEnumerable data, IEnumerable test);
            string Test1();
            int Test2();
        }
        [TestMethod]
        public void SaveSirTest()
        {
            using (var repository = DynamicRepository.Create<MockRepository>())
            {
                var fixture = new Fixture();

                var data = new List<SomeEntity>(fixture.CreateMany(new SomeEntity { Id = 1, Remark = "Test", Updated = false }, 10));

                var factory = DbProviderFactories.GetFactory("System.Data.SqlClient");
                // using castle proxy to create 
                var realCommand = factory.CreateCommand();
                var testCommand = Substitute.For<IDbCommand>();
                testCommand.CreateParameter().Returns(a => factory.CreateParameter());
                testCommand.Parameters.Returns(a => realCommand.Parameters);
                var connection = Substitute.For<IDbConnection>();
                connection.State.Returns(ConnectionState.Open);
                connection.CreateCommand().Returns(a => testCommand);
                var reader = Substitute.For<IDataReader>();
                SomeEntity current = null;
                int index = 0;
                testCommand.ExecuteReader().Returns(reader);
                testCommand.ExecuteScalar().Returns(a =>
                {
                    object res = null;
                    if (testCommand.CommandText == "Test1")
                    {
                        res = "hmm";
                    }
                    else if (testCommand.CommandText == "Test2")
                    {
                        res = 2;
                    }
                    return res;
                });
                reader.Read().Returns(a => index < data.Count);
                reader.When(a => a.Read()).Do(x =>
                {
                    current = index < data.Count ? data[index++] : null;
                });
                reader.FieldCount.Returns(3);
                reader.GetName(0).Returns(nameof(SomeEntity.Id));
                reader.GetName(1).Returns(nameof(SomeEntity.Remark));
                reader.GetName(2).Returns(nameof(SomeEntity.Updated));
                reader.IsDBNull(Arg.Any<int>()).ReturnsForAnyArgs(false);
                // DbEnumerator
                reader.GetFieldType(0).Returns(typeof(int));
                reader.GetFieldType(1).Returns(typeof(string));
                reader.GetFieldType(2).Returns(typeof(bool));
                reader.GetDataTypeName(0).Returns("int");
                reader.GetDataTypeName(1).Returns("nvarchar(max)");
                reader.GetDataTypeName(2).Returns("bit");
                reader.GetValues(Arg.Any<object[]>()).Returns(a =>
                {
                    var x = a[0] as object[];
                    x[0] = current.Id;
                    x[1] = current.Remark;
                    x[2] = current.Updated;
                    return 3;
                });
                repository.Connection = connection;
                var result = repository.Save(data, "test");
                result = repository.Save("test", fixture.CreateMany<string>().ToList());

                repository.SomeComplex("test", data.First(), fixture.CreateMany<string>().ToList(), 1);
                repository.SomeComplex(data, data.First(), "test", 1);
                
                object temp = repository.Test1();
                Assert.AreEqual("hmm", temp);

                temp = repository.Test2();
                Assert.AreEqual(2, temp);

                repository.Operations = RepositoryOperations.IgnoreException;
                repository.SomeComplex(data, data.First(), fixture.CreateMany<string>().ToList(), 1);
            }
        }

        [TestMethod]
        public void SimpleInvokeWithException()
        {
            using (MockRepository repository = DynamicRepository.Create<MockRepository>())
            {
                repository.Operations = RepositoryOperations.LogQueryExecutionTime;
                var command = Substitute.For<IDbCommand>();
                var connection = Substitute.For<IDbConnection>();
                connection.State.Returns(ConnectionState.Open);
                connection.CreateCommand().Returns(a => command);
                string exceptionText = "test";
                command.ExecuteNonQuery().Returns(a => { throw new Exception(exceptionText); });
                repository.Connection = connection;
                try
                {
                    repository.Some();
                }
                catch (Exception e)
                {
                    Assert.AreEqual(exceptionText, e.Message);
                }
                repository.Operations = RepositoryOperations.None;
                command = Substitute.For<IDbCommand>();
                command.ExecuteNonQuery().Returns(1);
                repository.Some();
            }
        }

        [TestMethod]
        public void SimpleInvokeWithoutWatches()
        {
            var repository = DynamicRepository.Create<MockRepository>();

            var factory = DbProviderFactories.GetFactory("System.Data.SqlClient");
            // using castle proxy to create 
            var realCommand = factory.CreateCommand();
            var testCommand = Substitute.For<IDbCommand>();
            testCommand.CreateParameter().Returns(a => factory.CreateParameter());
            testCommand.Parameters.Returns(a => realCommand.Parameters);

            var connection = Substitute.For<IDbConnection>();
            connection.State.Returns(ConnectionState.Open);
            connection.CreateCommand().Returns(a => testCommand);


            var reader = Substitute.For<DbDataReader>();
            reader.FieldCount.Returns(3);
            int counter = 1;
            reader.Read().Returns(a => counter-- > 0);
            testCommand.ExecuteNonQuery().Returns(a =>
            {
                realCommand.Parameters.OfType<IDataParameter>().Where(b => b.Direction == ParameterDirection.Output).FirstOrDefault().Value = 2;
                realCommand.Parameters.OfType<IDataParameter>().Where(b => b.Direction == ParameterDirection.InputOutput).FirstOrDefault().Value = "Hallo";
                return 1;
            });


            int value;
            string data = "test";
            repository.Connection = connection;
            repository.Init(out value, ref data);
            Assert.AreEqual(2, value);
            Assert.AreEqual("Hallo", data);
        }
    }
}