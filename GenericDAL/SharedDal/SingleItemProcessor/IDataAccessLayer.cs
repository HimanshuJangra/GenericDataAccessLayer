using System;
using System.Collections.Generic;
using SharedComponents.Data;
using SharedDal.DatabaseAccess;

namespace SharedDal.SingleItemProcessor
{
    /// <summary>
    /// Depenends on Implementation can use Single Statement Execution (SSE) for CRUD or
    /// Table Value Parameter (TVP)
    /// </summary>
    public interface IDataAccessLayer
    {
        /// <summary>
        /// If true, connection will be automatically closed, else keep connection open
        /// </summary>
        Boolean CloseConnectionOnDispose { get; set; }

        /// <summary>
        /// Contains default Operations (CRUD) descriptions
        /// </summary>
        IReadOnlyDictionary<DefaulDatabaseOperation, String> Operations { get; }

        /// <summary>
        /// List mit den Output, InputOutput, Return Parametern
        /// </summary>
        List<System.Data.IDbDataParameter> OutputParameters { get; }

        /// <summary>
        /// Name of the Entity
        /// </summary>
        String EntityName { get; }

        /// <summary>
        /// Create new Connection
        /// </summary>
        /// <returns></returns>
        System.Data.IDbConnection OpenConnection();

        /// <summary>
        /// Use to fill Data for SSE
        /// </summary>
        /// <param name="item">placeholder for data</param>
        /// <param name="command">Existing Command to use. Allow directly create new Parameter</param>
        /// <param name="operation">include operation for current execution</param>
        void FillParameter(IDataTransferObject item, System.Data.IDbCommand command, DefaulDatabaseOperation operation);

        /// <summary>
        /// A datareader that allow to get specific Entity information and save it into an object
        /// </summary>
        /// <param name="reader">Datareader that allow read from Stream</param>
        /// <returns>Current DC</returns>
        IDataTransferObject Get(System.Data.IDataReader reader);

        /// <summary>
        /// A datareader that allow to get specific Entity information and save it into an object
        /// </summary>
        /// <param name="item">current item to fill</param>
        /// <param name="reader">Datareader that allow read from Stream</param>
        void Get(IDataTransferObject item, System.Data.IDataReader reader);

        /// <summary>
        /// Save Entity
        /// </summary>
        /// <param name="item">data for add or update</param>
        /// <returns>True, if succes</returns>
        Boolean Save(IDataTransferObject item);

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
        void Read(List<IDataTransferObject> items, System.Data.IDbCommand command);

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
        void Get(IDataTransferObject keyHolder, System.Data.IDbCommand command);

        /// <summary>
        /// read "object bound or unbound" data from query. 
        /// </summary>
        /// <typeparam name="TGeneric">Type of the Object unbound data</typeparam>
        /// <param name="items">List that will contain the data</param>
        /// <param name="generator">Read data from query and return it for items list</param>
        /// <param name="reader">Data Accessor</param>
        void Read<TGeneric>(IList<TGeneric> items, Func<System.Data.IDataReader, TGeneric> generator, System.Data.IDataReader reader);

        #region CRUD

        /// <summary>
        /// SSE for Insert
        /// </summary>
        int Create(IDataTransferObject item);

        /// <summary>
        /// SSE for Insert
        /// </summary>
        /// <param name="item">item to create</param>
        /// <param name="command">Existing Command to use</param>
        int Create(IDataTransferObject item, System.Data.IDbCommand command);

        /// <summary>
        /// SSE for Update
        /// </summary>
        int Update(IDataTransferObject item);

        /// <summary>
        /// SSE for Update
        /// </summary>
        /// <param name="item">item to update</param>
        /// <param name="command">Existing Command to use</param>
        int Update(IDataTransferObject item, System.Data.IDbCommand command);

        /// <summary>
        /// SSE for Delete
        /// </summary>
        int Delete(IDataTransferObject item);

        /// <summary>
        /// SSE for Delete
        /// </summary>
        /// <param name="item">item to delete</param>
        /// <param name="command">Existing Command to use</param>
        int Delete(IDataTransferObject item, System.Data.IDbCommand command);

        #endregion
    }
}
