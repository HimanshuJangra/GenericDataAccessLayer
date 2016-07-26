using System;
using GenericDataAccessLayer.Core;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ploeh.AutoFixture;

namespace UnitTests.Extension
{
    [TestClass]
    public class ExtensionTest
    {
        private Fixture _fixture = new Fixture();

        [TestMethod]
        public void ParameterTest()
        {
            using (var command = new System.Data.SqlClient.SqlCommand())
            {
                var name = _fixture.Create<String>();
                var value = _fixture.Create<DateTime>();
                var direction = _fixture.Create<System.Data.ParameterDirection>();
                var type = _fixture.Create<System.Data.DbType>();
                command.AddParameter(name, value, direction, type);
            }
        }
    }
}
