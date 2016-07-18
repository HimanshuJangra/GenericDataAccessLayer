using System;
using System.Collections.Generic;
using System.Data;
using GenericDataAccessLayer.LazyDal.Repository;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Ploeh.AutoFixture;
using System.Linq;
using GenericDataAccessLayer.LazyDal;

namespace UnitTests.Repository.Calls
{
    [TestClass]
    public class UnitTest
    {
        private Fixture _fixture = new Fixture();

        private void InitializeProxy(out TestRepository proxy, out IDbConnection connection, out IDbCommand command)
        {
            proxy = DynamicRepository.Create<TestRepository>();
            connection = Substitute.For<IDbConnection>();
            connection.ConnectionString = "some";
            proxy.Connection = connection;
            command = Substitute.For<IDbCommand>();
            var collection = new ParameterCollection();
            command.Parameters.Returns(collection);
            command.CreateParameter().Returns(a => Substitute.For<IDbDataParameter>());
            connection.CreateCommand().Returns(command);
        }

        private IDataReader InitializeReaderForUser(IDbCommand command, params User[] users)
        {
            var reader = Substitute.For<IDataReader>();
            command.ExecuteReader().Returns(reader);

            User user = null;
            int index = 0;
            bool read = true;
            reader.Read().Returns(a => read);
            reader.When(a => a.Read()).Do(a =>
            {
                read = index < users.Length;
                user = read ? users[index++] : null;
            });

            reader.FieldCount.Returns(5);
            reader.GetName(0).Returns(nameof(User.Id));
            reader.GetName(1).Returns(nameof(User.UserName));
            reader.GetName(2).Returns(nameof(User.Created));
            reader.GetName(3).Returns(nameof(User.IsActive));
            reader.GetName(4).Returns(nameof(User.Avatar));
            reader.IsDBNull(Arg.Any<int>()).ReturnsForAnyArgs(false);
            // DbEnumerator
            reader.GetFieldType(0).Returns(typeof(int));
            reader.GetFieldType(1).Returns(typeof(string));
            reader.GetFieldType(2).Returns(typeof(DateTime));
            reader.GetFieldType(3).Returns(typeof(bool));
            reader.GetFieldType(4).Returns(typeof(byte[]));
            reader.GetDataTypeName(0).Returns("int");
            reader.GetDataTypeName(1).Returns("nvarchar(max)");
            reader.GetDataTypeName(2).Returns("datetime");
            reader.GetDataTypeName(3).Returns("bit");
            reader.GetDataTypeName(4).Returns("varbinary(max)");
            reader.GetValues(Arg.Any<object[]>()).Returns(a =>
            {
                var x = (object[])a[0];
                x[0] = _fixture.Create<int>();
                x[1] = user.UserName;
                x[2] = user.Created;
                x[3] = true;
                x[4] = user.Avatar;
                return 5;
            });
            return reader;
        }

        private void DefaultAsserts(IDbConnection connection, IDbCommand command)
        {
            connection.Received(1).Open();
            connection.Received(1).CreateCommand();
        }

        private void ExecuteReaderAssert(IDbCommand command, int times)
        {
            command.DidNotReceive().ExecuteNonQuery();
            command.Received(times).ExecuteReader();
            command.DidNotReceive().ExecuteReader(Arg.Any<CommandBehavior>());
            command.DidNotReceive().ExecuteScalar();
        }

        private void ExecuteNonQueryAssert(IDbCommand command, int times)
        {
            command.Received(times).ExecuteNonQuery();
            command.DidNotReceive().ExecuteReader();
            command.DidNotReceive().ExecuteReader(Arg.Any<CommandBehavior>());
            command.DidNotReceive().ExecuteScalar();
        }

        private class ParameterCollection : List<IDataParameter>, IDataParameterCollection
        {
            /// <summary>Ruft einen Wert ab, der angibt, ob ein Parameter in der Auflistung über den angegebenen Namen verfügt.</summary>
            /// <returns>true, wenn die Auflistung den Parameter enthält, andernfalls false.</returns>
            /// <param name="parameterName">Der Name des Parameters. </param>
            public bool Contains(string parameterName)
            {
                return this.Any(a => a.ParameterName == parameterName);
            }

            /// <summary>Ruft die Position der <see cref="T:System.Data.IDataParameter" />-Schnittstelle in der Auflistung ab.</summary>
            /// <returns>Die nullbasierte Position der <see cref="T:System.Data.IDataParameter" />-Schnittstelle in der Auflistung.</returns>
            /// <param name="parameterName">Der Name des Parameters. </param>
            public int IndexOf(string parameterName)
            {
                var item = this.FirstOrDefault(a => a.ParameterName == parameterName);
                return item == null ? -1 : this.IndexOf(item);
            }

            /// <summary>Entfernt die <see cref="T:System.Data.IDataParameter" />-Schnittstelle aus der Auflistung.</summary>
            /// <param name="parameterName">Der Name des Parameters. </param>
            public void RemoveAt(string parameterName)
            {
                this.RemoveAt(this.IndexOf(parameterName));
            }

            /// <summary>Ruft den Parameter am angegebenen Index ab oder legt diesen fest.</summary>
            /// <returns>Eine <see cref="T:System.Object" />-Klasse am angegebenen Index.</returns>
            /// <param name="parameterName">Der Name des abzurufenden Parameters. </param>
            public object this[string parameterName]
            {
                get { return this[IndexOf(parameterName)]; }
                set { this[IndexOf(parameterName)] = (IDataParameter)value; }
            }
        }

        private void GenericCreate(Action<TestRepository, IDbCommand> custom, RepositoryOperations options)
        {
            TestRepository proxy;
            IDbConnection connection;
            IDbCommand command;
            InitializeProxy(out proxy, out connection, out command);
            proxy.Operations = options;
            custom(proxy, command);

            DefaultAsserts(connection, command);

            if (proxy.Operations.HasFlag(RepositoryOperations.LogQueryExecutionTime))
            {
                Console.WriteLine($"Query Execution Time: {proxy.QueryExecutionTime}");
            }
            if (proxy.Operations.HasFlag(RepositoryOperations.LogTotalExecutionTime))
            {
                Console.WriteLine($"Total Execution Time: {proxy.TotalExecutionTime}");
            }
        }

        #region TestCore
        private void CreateUserRef_Init(TestRepository proxy, IDbCommand command)
        {
            var testUser = _fixture.Create<User>();
            testUser.IsActive = false;
            int oldId = testUser.Id;

            int newId = _fixture.Create<int>();
            command.When(a => a.ExecuteNonQuery()).Do(a =>
            {
                command.Parameters.OfType<IDataParameter>().First(x => x.ParameterName == nameof(User.Id)).Value = newId;
                command.Parameters.OfType<IDataParameter>().First(x => x.ParameterName == nameof(User.IsActive)).Value = true;
            });

            proxy.CreateUseRef(ref testUser);

            Assert.AreEqual(nameof(proxy.CreateUseRef), command.CommandText);
            Assert.AreNotEqual(oldId, testUser.Id);
            Assert.IsTrue(testUser.IsActive);
            ExecuteNonQueryAssert(command, 1);
        }
        private void CreateUseReturn_Init(TestRepository proxy, IDbCommand command)
        {
            var testUser = _fixture.Create<User>();
            testUser.IsActive = false;
            testUser.Id = 0;
            var reader = InitializeReaderForUser(command, testUser);
            var result = proxy.CreateUseReturn(testUser);

            Assert.AreEqual(nameof(proxy.CreateUseReturn), command.CommandText);

            Assert.AreNotEqual(testUser.Id, result.Id);
            Assert.AreEqual(testUser.Avatar, result.Avatar);
            Assert.AreEqual(testUser.Created, result.Created);
            Assert.AreEqual(testUser.UserName, result.UserName);
            Assert.IsTrue(result.IsActive);
            reader.Received(1).Read();
            ExecuteReaderAssert(command, 1);
        }
        private void UpdateUseArrayReturn_Init(TestRepository proxy, IDbCommand command)
        {
            var users = _fixture.CreateMany<User>().ToArray();
            foreach (var item in users)
            {
                item.Id = 0;
                item.IsActive = false;
            }
            InitializeReaderForUser(command, users);
            var result = proxy.UpdateUseArrayReturn(users.ToList());

            Assert.AreEqual(nameof(proxy.UpdateUseArrayReturn), command.CommandText);
            Assert.AreEqual(result.Length, users.Length);
            Assert.IsFalse(result.Any(a => a.Id == 0));
            Assert.IsFalse(result.Any(a => a.IsActive == false));
            var readerReceived = proxy.Operations.HasFlag(RepositoryOperations.UseTableValuedParameter) ? 1 : users.Length;
            ExecuteReaderAssert(command, readerReceived);
        }
        private void ReadEnumerable_Init(TestRepository proxy, IDbCommand command)
        {
            var users = _fixture.CreateMany<User>(100000).ToArray();
            var reader = InitializeReaderForUser(command, users);
            var result = proxy.Read();
            Assert.AreEqual(users.Length, result.Count());
            ExecuteReaderAssert(command, 1);
            reader.Received(users.Length + 1).Read();
        }
        #endregion

        [TestMethod]
        public void CreateUseReturn_NoOptions_Test()
        {
            GenericCreate(CreateUseReturn_Init, RepositoryOperations.None);
        }

        [TestMethod]
        public void CreateUseReturn_FullOptions_Test()
        {
            GenericCreate(CreateUseReturn_Init, RepositoryOperations.All);
        }

        [TestMethod]
        public void CreateUseRef_NoOptions_Test()
        {
            GenericCreate(CreateUserRef_Init, RepositoryOperations.None);
        }

        [TestMethod]
        public void CreateUseRef_FullOptions_Test()
        {
            GenericCreate(CreateUserRef_Init, RepositoryOperations.All);
        }

        [TestMethod]
        public void UpdateUseArrayReturn_NoOptions_Test()
        {
            GenericCreate(UpdateUseArrayReturn_Init, RepositoryOperations.None);
        }

        [TestMethod]
        public void UpdateUseArrayReturn_FullOptions_Test()
        {
            GenericCreate(UpdateUseArrayReturn_Init, RepositoryOperations.All);
        }

        [TestMethod]
        public void ReadEnumerable_NoOptions_Test()
        {
            GenericCreate(ReadEnumerable_Init, RepositoryOperations.None);
        }

        [TestMethod]
        public void ReadEnumerable_FullOptions_Test()
        {
            GenericCreate(ReadEnumerable_Init, RepositoryOperations.All);
        }
    }
}
