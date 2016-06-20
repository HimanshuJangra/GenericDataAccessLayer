using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharedComponents.Data;

namespace GenericDataAccessLayer.Core.DatabaseAccess
{
    public interface IDataAccessLayerShared : IBasicDbAccess
    {
        /// <summary>
        /// what kind of command type should used
        /// </summary>
        CommandType DefaultCommandType { get; set; }
        /// <summary>
        /// Name of the Entity
        /// </summary>
        String EntityName { get; }
        /// <summary>
        /// Contains default Operations (CRUD) descriptions
        /// </summary>
        IReadOnlyDictionary<DefaulDatabaseOperation, String> Operations { get; }

        /// <summary>
        /// List mit den Output, InputOutput, Return Parametern
        /// </summary>
        List<IDbDataParameter> OutputParameters { get; }

        /// <summary>
        /// Get one entry from db using current Entity as keyholder
        /// </summary>
        /// <param name="keyHolder">Object that contains primary keys for the Stored procedure</param>
        void Get(IDataTransferObject keyHolder);

        /// <summary>
        /// Get one entry from db using current Entity as keyholder
        /// </summary>
        /// <param name="keyHolder">Object that contains primary keys for the Stored procedure</param>
        /// <param name="command">Existing Command to use</param>
        void Get(IDataTransferObject keyHolder, IDbCommand command);

        /// <summary>
        /// A datareader that allow to get specific Entity information and save it into an object
        /// </summary>
        /// <param name="reader">Datareader that allow read from Stream</param>
        /// <returns>Current DC</returns>
        IDataTransferObject Get(IDataRecord reader);

        /// <summary>
        /// A datareader that allow to get specific Entity information and save it into an object
        /// </summary>
        /// <param name="item">current item to fill</param>
        /// <param name="reader">Datareader that allow read from Stream</param>
        void Get(IDataTransferObject item, System.Data.IDataReader reader);

        /// <summary>
        /// Save full list of the Transfer objects
        /// </summary>
        /// <param name="data">Transfer data to save</param>
        void Save(IEnumerable<IDataTransferObject> data);


        /// <summary>
        /// Get all data from table
        /// </summary>
        /// <param name="items">data from query where to save in</param>
        void Read(List<IDataTransferObject> items);

        /// <summary>
        /// Get all data from table
        /// </summary>
        /// <param name="items">data from query where to save in</param>
        /// <param name="command">Existing Command to use</param>
        void Read(List<IDataTransferObject> items, IDbCommand command);

        /// <summary>
        /// read "object bound or unbound" data from query. 
        /// </summary>
        /// <typeparam name="TGeneric">Type of the Object unbound data</typeparam>
        /// <param name="items">List that will contain the data</param>
        /// <param name="generator">Read data from query and return it for items list</param>
        /// <param name="reader">Data Accessor</param>
        void Read<TGeneric>(IList<TGeneric> items, Func<IDataRecord, TGeneric> generator, IDataReader reader);
    }
}
