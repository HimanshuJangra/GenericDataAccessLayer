using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GenericDataAccessLayer.Core.DatabaseAccess;
using GenericDataAccessLayer.Core.SingleItemProcessor;
using GenericDataAccessLayer.Core;

namespace UnitTests.SingleItemProcessor
{
    internal class SomeEntityDal : DalBase<SomeEntity>
    {
        public override void FillParameter(SomeEntity item, IDbCommand command, DefaulDatabaseOperation operation)
        {
            if(item.RowState == DataRowState.Deleted) { }
            //command.AddParameter(nameof(item.Id), item.Id);
        }

        /// <summary>
        /// Contains default Operations (CRUD) descriptions
        /// </summary>
        public override IReadOnlyDictionary<DefaulDatabaseOperation, string> Operations { get; }
        public override SomeEntity Get(IDataRecord record)
        {
            throw new NotImplementedException();
        }

        public override void Get(SomeEntity item, IDataRecord reader)
        {
            throw new NotImplementedException();
        }
    }
}
