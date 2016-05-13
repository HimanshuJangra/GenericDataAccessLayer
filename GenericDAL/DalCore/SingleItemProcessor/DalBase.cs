using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using SharedComponents.Data;
using DalCore.DatabaseAccess;
using Localization = L10n.Properties.Resources;

namespace DalCore.SingleItemProcessor
{
    /// <summary>
    /// Base definition how to process single statement execution
    /// </summary>
    /// <typeparam name="T">Data Transfer Object</typeparam>
    public abstract class DalBase<T> : DataAccessLayerShared<T>, IDataAccessLayer
           where T : class, IDataTransferObject, new()
    {
        /// <summary>
        /// Use DAL with default Connection String Information
        /// </summary>
        public DalBase()
        {
        }
        /// <summary>
        /// Use DAL with specific Connection String Information
        /// </summary>
        /// <param name="configurationName"></param>
        public DalBase(String configurationName) : base(configurationName)
        {
        }
        /// <summary>
        /// Use DAL with disconnected Configuration.
        /// </summary>
        /// <param name="connectionString">manual connection String</param>
        /// <param name="providerName">name of the SQL Server provider</param>
        public DalBase(String connectionString, String providerName) : base(connectionString, providerName)
        {
        }
        #region Generic implementation

        /// <summary>
        /// Save Entity, only create or update
        /// </summary>
        /// <param name="item">data for add or update</param>
        /// <returns>True, if succes</returns>
        public Boolean Save(T item)
        {
            Boolean result = true;
            if (item != null)
            {
                Func<T, IDbCommand, int> save = this.Save;
                result = this.Execute(save, item) > 0;
            }
            else
            {
                throw new ArgumentNullException(nameof(Localization.DE001));
            }

            return result;
        }

        /// <summary>
        /// Save Entity using rowstate
        /// </summary>
        /// <param name="item">item to modify</param>
        /// <param name="command">Command to (re)use</param>
        /// <returns> greater than zeor, if succes</returns>
        public int Save(T item, IDbCommand command)
        {
            int result = -1;
            Func<T, IDbCommand, int> execute = null;
            DefaulDatabaseOperation operation = DefaulDatabaseOperation.None;
            
            if (item.RowState == DataRowState.Added)
            {
                operation = DefaulDatabaseOperation.Create; 
                execute = this.Create;
            }
            else if (item.RowState == DataRowState.Deleted)
            {
                operation = DefaulDatabaseOperation.Delete;
                execute = this.Delete;
            }
            else if (item.RowState == DataRowState.Modified)
            {
                operation = DefaulDatabaseOperation.Update;
                execute = this.Update;
            }

            if (execute != null)
            {
                command.CommandText = this.Operations[operation];
                this.FillParameter(item, command, operation);
                result = execute(item, command);
            }

            return result;
        }
        /// <summary>
        /// Save complete list sequentially
        /// </summary>
        /// <param name="data">data to save</param>
        public override void Save(List<T> data)
        {
            Action<List<T>, IDbCommand> save = this.Save;
            this.Execute(save, data);
        }
        /// <summary>
        /// save complete list use same command
        /// </summary>
        /// <param name="data"></param>
        /// <param name="command"></param>
        public void Save(List<T> data, IDbCommand command)
        {
            foreach (var item in data)
            {
                this.Save(item, command);
            }
        }
        /// <summary>
        /// Create new Entry in database for given entity
        /// </summary>
        /// <param name="item">Item that keep the entries for a row</param>
        /// <returns>Affected rows</returns>
        public int Create(T item)
        {
            return this.Execute(this.Create, item, DefaulDatabaseOperation.Create);
        }

        /// <summary>
        /// Create new Entry in database for given entity and command
        /// </summary>
        /// <param name="item">Item that keep the entries for a row</param>
        /// <param name="command">command to use</param>
        /// <returns>Affected rows</returns>
        public int Create(T item, IDbCommand command)
        {
            return command.GetFirstRow(this.Get, item);
        }

        public int Update(T item)
        {
            return this.Execute(this.Update, item, DefaulDatabaseOperation.Update);
        }

        public int Update(T item, IDbCommand command)
        {
            return command.GetFirstRow(this.Get, item);
        }

        public int Delete(T item)
        {
            return this.Execute(this.Delete, item, DefaulDatabaseOperation.Delete);
        }

        public int Delete(T item, IDbCommand command)
        {
            return command.ExecuteNonQuery();
        }
        
        #endregion


        #region Explicit definition

        /// <summary>
        /// Use to fill Data for SSE
        /// </summary>
        /// <param name="item">placeholder for data</param>
        /// <param name="command">Existing Command to use. Allow directly create new Parameter</param>
        /// <param name="operation">include operation for current execution</param>
        void IDataAccessLayer.FillParameter(IDataTransferObject item, IDbCommand command, DefaulDatabaseOperation operation)
        {
            this.FillParameter(item as T, command, operation);
        }

        /// <summary>
        /// Save Entity
        /// </summary>
        /// <param name="item">data for add or update</param>
        /// <returns>True, if succes</returns>
        bool IDataAccessLayer.Save(IDataTransferObject item)
        {
            return this.Save(item as T);
        }

        /// <summary>
        /// SSE for Insert
        /// </summary>
        int IDataAccessLayer.Create(IDataTransferObject item)
        {
            return this.Create(item as T);
        }

        /// <summary>
        /// SSE for Insert
        /// </summary>
        /// <param name="item">item to create</param>
        /// <param name="command">Existing Command to use</param>
        int IDataAccessLayer.Create(IDataTransferObject item, IDbCommand command)
        {
            return this.Create(item as T, command);
        }

        /// <summary>
        /// SSE for Update
        /// </summary>
        int IDataAccessLayer.Update(IDataTransferObject item)
        {
            return this.Update(item as T);
        }

        /// <summary>
        /// SSE for Update
        /// </summary>
        /// <param name="item">item to update</param>
        /// <param name="command">Existing Command to use</param>
        int IDataAccessLayer.Update(IDataTransferObject item, IDbCommand command)
        {
            return this.Update(item as T, command);
        }

        /// <summary>
        /// SSE for Delete
        /// </summary>
        int IDataAccessLayer.Delete(IDataTransferObject item)
        {
            return this.Delete(item as T);
        }

        /// <summary>
        /// SSE for Delete
        /// </summary>
        /// <param name="item">item to delete</param>
        /// <param name="command">Existing Command to use</param>
        int IDataAccessLayer.Delete(IDataTransferObject item, IDbCommand command)
        {
            return this.Delete(item as T, command);
        }

        #endregion
    }
}
