using System;
using System.Collections.Generic;
using SharedComponents.Data;
using DalCore.DatabaseAccess;

namespace DalCore.SingleItemProcessor
{
    /// <summary>
    /// Depenends on Implementation can use Single Statement Execution (SSE) for CRUD or
    /// Table Value Parameter (TVP)
    /// </summary>
    public interface IDataAccessLayer: IDataAccessLayerShared
    {

        /// <summary>
        /// Use to fill Data for SSE
        /// </summary>
        /// <param name="item">placeholder for data</param>
        /// <param name="command">Existing Command to use. Allow directly create new Parameter</param>
        /// <param name="operation">include operation for current execution</param>
        void FillParameter(IDataTransferObject item, System.Data.IDbCommand command, DefaulDatabaseOperation operation);

        /// <summary>
        /// Save Entity, only create or update
        /// </summary>
        /// <param name="item">data for add or update</param>
        /// <returns>True, if succes</returns>
        Boolean Save(IDataTransferObject item);

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
