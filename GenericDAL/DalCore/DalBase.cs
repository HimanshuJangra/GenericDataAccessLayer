using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharedComponents.Data;
using SharedDal.DatabaseAccess;
using SharedDal.SingleItemProcessor;

namespace DalCore
{
    public abstract class DalBase<T> : BasicDbAccess, IDataAccessLayer
           where T : class, IDataTransferObject, new()
    {
        /// <summary>
        /// Use DAL with default Connection String Information
        /// </summary>
        public DalBase()
            : base("DefaultConnection")
        {
        }
        /// <summary>
        /// Use DAL with specific Connection String Information
        /// </summary>
        /// <param name="providerName"></param>
        public DalBase(String providerName) : base(providerName)
        {
        }
        /// <summary>
        /// Use DAK with disconnected Configuration.
        /// </summary>
        /// <param name="connectionString"></param>
        /// <param name="providerName"></param>
        public DalBase(String connectionString, String providerName) : base(connectionString, providerName)
        {
        }

        /// <summary>
        /// read "object bound or unbound" data from query. 
        /// </summary>
        /// <typeparam name="TGeneric">Type of the Object unbound data</typeparam>
        /// <param name="items">List that will contain the data</param>
        /// <param name="generator">Read data from query and return it for items list</param>
        /// <param name="reader">Data Accessor</param>
        public void Read<TGeneric>(IList<TGeneric> items, Func<IDataReader, TGeneric> generator, IDataReader reader)
        {
            while (reader.Read() == true)
            {
                items.Add(generator(reader));
            }
        }

        /// <summary>
        /// Execute single command
        /// </summary>
        /// <param name="execute">Method to execute</param>
        /// <param name="item">IDataTransferObject</param>
        /// <param name="operation">Database Operation like CRUD</param>
        /// <returns></returns>
        protected int Execute(Func<T, IDbCommand, int> execute, T item, DefaulDatabaseOperation operation)
        {
            int result = -1;
            var connection = this.OpenConnection();
            using (IDbCommand command = connection.CreateCommand())
            {
                this.OutputParameters.Clear();
                command.CommandType = System.Data.CommandType.StoredProcedure;
                command.CommandText = this.Operations[operation];
                this.FillParameter(item, command, operation);
                command.Prepare();
                result = execute(item, command);
                this.OutputParameters.AddRange(command.Parameters.OfType<IDbDataParameter>().Where(a => a.Direction != System.Data.ParameterDirection.Input));
            }

            return result;
        }
        /// <summary>
        /// Execute single command take list of T as parameter. No parameter will be filled
        /// </summary>
        /// <param name="execute">Method to execute</param>
        /// <param name="item">IDataTransferObject</param>
        /// <param name="operation">Database Operation like CRUD</param>
        /// <returns></returns>
        protected int Execute(Func<List<T>, IDbCommand, int> execute, List<T> item, DefaulDatabaseOperation operation)
        {
            int result = -1;
            var connection = this.OpenConnection();
            using (IDbCommand command = connection.CreateCommand())
            {
                this.OutputParameters.Clear();
                command.CommandType = System.Data.CommandType.StoredProcedure;
                command.CommandText = this.Operations[operation];
                result = execute(item, command);
                this.OutputParameters.AddRange(command.Parameters.OfType<IDbDataParameter>().Where(a => a.Direction != System.Data.ParameterDirection.Input));
            }

            return result;
        }

        /// <summary>
        /// Execute single command
        /// </summary>
        /// <param name="execute">Definition to execute</param>
        /// <param name="data">Data to pass to executing method</param>
        protected void Execute<TData>(Action<TData, IDbCommand> execute, TData data)
        {
            var connection = this.OpenConnection();
            using (IDbCommand command = connection.CreateCommand())
            {
                this.OutputParameters.Clear();
                command.CommandType = System.Data.CommandType.StoredProcedure;
                execute(data, command);
                this.OutputParameters.AddRange(command.Parameters.OfType<IDbDataParameter>().Where(a => a.Direction != System.Data.ParameterDirection.Input));
            }
        }
        /// <summary>
        /// Execute single command without fill parameters
        /// </summary>
        /// <param name="execute">Definition to execute</param>
        /// <param name="item">Item to create</param>
        protected int Execute<TData>(Func<TData, IDbCommand, int> execute, TData item)
        {
            int result = -1;
            var connection = this.OpenConnection();
            using (IDbCommand command = connection.CreateCommand())
            {
                this.OutputParameters.Clear();
                command.CommandType = System.Data.CommandType.StoredProcedure;
                result = execute(item, command);
                this.OutputParameters.AddRange(command.Parameters.OfType<IDbDataParameter>().Where(a => a.Direction != System.Data.ParameterDirection.Input));
            }

            return result;
        }

        /// <summary>
        /// Contains default Operations (CRUD) descriptions
        /// </summary>
        public abstract IReadOnlyDictionary<DefaulDatabaseOperation, string> Operations { get; }

        /// <summary>
        /// List mit den Output, InputOutput, Return Parametern
        /// </summary>
        public List<IDbDataParameter> OutputParameters { get; } = new List<IDbDataParameter>();

        /// <summary>
        /// Name of the Entity
        /// </summary>
        public string EntityName => typeof(T).Name;
        #region Generic implementation

        public abstract void FillParameter(T item, IDbCommand command, DefaulDatabaseOperation operation);

        public abstract T Get(IDataReader reader);

        public abstract void Get(T item, IDataReader reader);

        public Boolean AddOrUpdate(T item, Boolean forceAdd = false)
        {
            Boolean Result = true;
            if (item != null)
            {
                if (item.HasPrimaryKey == true && forceAdd == false)
                {
                    Result = this.Update(item) > 0;
                }

                if (item.HasPrimaryKey == false || forceAdd == true)
                {
                    Result = this.Create(item) > 0;
                }
            }

            return Result;
        }



        /// <summary>
        /// Write into given list all Items
        /// </summary>
        /// <param name="items">Empty (?) list for Items</param>
        public void Read(List<T> items)
        {
            this.Execute(this.Read, items, DefaulDatabaseOperation.Read);
        }

        /// <summary>
        /// Write into given list all Items
        /// </summary>
        /// <param name="items">Empty (?) list for Items</param>
        /// <param name="command">Existing Command to use</param>
        public int Read(List<T> items, IDbCommand command)
        {
            int count = 0;
            using (var reader = command.ExecuteReader())
            {
                this.Read(items, this.Get, reader);
                count = reader.RecordsAffected;
            }

            return count;
        }

        /// <summary>
        /// Get one entry from db using current Entity as keyholder
        /// </summary>
        /// <param name="keyHolder">Object that contains primary keys for the Stored procedure</param>
        public int Get(T keyHolder)
        {
            return this.Execute(this.Get, keyHolder, DefaulDatabaseOperation.Get);
        }

        public int Get(T keyHolder, IDbCommand command)
        {
            return command.GetFirstRow(this.Get, keyHolder);
        }

        public int Create(T item)
        {
            return this.Execute(this.Create, item, DefaulDatabaseOperation.Create);
        }

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
        /// A datareader that allow to get specific Entity information and save it into an object
        /// </summary>
        /// <param name="reader">Datareader that allow read from Stream</param>
        /// <returns>Current DC</returns>
        IDataTransferObject IDataAccessLayer.Get(IDataReader reader)
        {
            return this.Get(reader);
        }

        /// <summary>
        /// Save Entity
        /// </summary>
        /// <param name="item">data for add or update</param>
        /// <param name="forceAdd">if true, data will be created and not updated</param>
        /// <returns>True, if succes</returns>
        bool IDataAccessLayer.Save(IDataTransferObject item)
        {
            return this.AddOrUpdate(item as T);
        }

        /// <summary>
        /// Get all data from table
        /// </summary>
        /// <param name="items">data from query where to save in</param>
        void IDataAccessLayer.Read(List<IDataTransferObject> items)
        {
            this.Read(items as List<T>);
        }

        /// <summary>
        /// Get all data from table
        /// </summary>
        /// <param name="items">data from query where to save in</param>
        /// <param name="command">Existing Command to use</param>
        void IDataAccessLayer.Read(List<IDataTransferObject> items, IDbCommand command)
        {
            this.Read(items as List<T>, command);
        }

        /// <summary>
        /// Get one entry from db using current Entity as keyholder
        /// </summary>
        /// <param name="keyHolder">Object that contains primary keys for the Stored procedure</param>
        void IDataAccessLayer.Get(IDataTransferObject keyHolder)
        {
            this.Get(keyHolder as T);
        }

        /// <summary>
        /// Get one entry from db using current Entity as keyholder
        /// </summary>
        /// <param name="keyHolder">Object that contains primary keys for the Stored procedure</param>
        /// <param name="command">Existing Command to use</param>
        void IDataAccessLayer.Get(IDataTransferObject keyHolder, IDbCommand command)
        {
            this.Get(keyHolder as T, command);
        }

        /// <summary>
        /// A datareader that allow to get specific Entity information and save it into an object
        /// </summary>
        /// <param name="item">current item to fill</param>
        /// <param name="reader">Datareader that allow read from Stream</param>
        void IDataAccessLayer.Get(IDataTransferObject item, IDataReader reader)
        {
            this.Get(item as T, reader);
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
