using SharedComponents.Data;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnitTests.SingleItemProcessor
{
    /// <summary>
    /// Test Entity
    /// </summary>
    public class SomeEntity : IDataTransferObject
    {
        public int Id { get; set; }

        public string Remark { get; set; }

        public bool Updated { get; set; }

        /// <summary>
        /// Help Identify if Primary Key exists and has a value
        /// </summary>
        public bool HasPrimaryKey => this.Id > 0;

        /// <summary>
        /// current state of the data
        /// </summary>
        public DataRowState RowState { get; set; }
    }
}
