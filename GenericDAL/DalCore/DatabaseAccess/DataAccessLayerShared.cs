using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharedComponents.Data;

namespace GenericDataAccessLayer.Core.DatabaseAccess
{
    public abstract class DataAccessLayerShared<T>: BasicDbAccess, IDataAccessLayerShared
        where T : class, IDataTransferObject, new()
    {

        /// <summary>
        /// what kind of command type should used
        /// </summary>
        public CommandType DefaultCommandType { get; set; } = CommandType.StoredProcedure;
        /// <summary>
        /// Name of the Entity
        /// </summary>
        public virtual string EntityName => typeof(T).Name;

        public DataAccessLayerShared()
            : base()
        {
            
        }

        /// <summary>
        /// Use DAL with specific Connection String Information
        /// </summary>
        /// <param name="configurationName"></param>
        public DataAccessLayerShared(string configurationName)
            : base(configurationName)
        {
        }

        /// <summary>
        /// Use DAK with disconnected Configuration.
        /// </summary>
        /// <param name="connectionString"></param>
        /// <param name="providerName"></param>
        public DataAccessLayerShared(string connectionString, string providerName)
            : base(connectionString, providerName)
        {
        }

        /// <summary>
        /// read "object bound or unbound" data from query. 
        /// </summary>
        /// <typeparam name="TGeneric">Type of the Object unbound data</typeparam>
        /// <param name="items">List that will contain the data</param>
        /// <param name="generator">Read data from query and return it for items list</param>
        /// <param name="reader">Data Accessor</param>
        public void Read<TGeneric>(IList<TGeneric> items, Func<IDataRecord, TGeneric> generator, IDataReader reader)
        {
            foreach (var record in reader.ToRecord())
            {
                items.Add(generator(record));
            }
        }

        public abstract void FillParameter(T item, IDbCommand command, DefaulDatabaseOperation operation);

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
                command.CommandType = this.DefaultCommandType;
                command.CommandText = this.Operations[operation];
                this.FillParameter(item, command, operation);
                command.Prepare();
                result = execute(item, command);
                this.OutputParameters.AddRange(command.Parameters.OfType<IDbDataParameter>().Where(a => a.Direction != ParameterDirection.Input));
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
                command.CommandType = this.DefaultCommandType;
                command.CommandText = this.Operations[operation];
                result = execute(item, command);
                this.OutputParameters.AddRange(command.Parameters.OfType<IDbDataParameter>().Where(a => a.Direction != ParameterDirection.Input));
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
                command.CommandType = this.DefaultCommandType;
                execute(data, command);
                this.OutputParameters.AddRange(command.Parameters.OfType<IDbDataParameter>().Where(a => a.Direction != ParameterDirection.Input));
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
                command.CommandType = this.DefaultCommandType;
                result = execute(item, command);
                this.OutputParameters.AddRange(command.Parameters.OfType<IDbDataParameter>().Where(a => a.Direction != ParameterDirection.Input));
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

        #region IDataAccessLayerShared Implementation

        public abstract void Save(List<T> data);

        public abstract T Get(IDataRecord record);

        public abstract void Get(T item, IDataRecord reader);

        /// <summary>
        /// Get one entry from db using current Entity as keyholder
        /// </summary>
        /// <param name="keyHolder">Object that contains primary keys for the Stored procedure</param>
        /// <returns>Affected rows</returns>
        public int Get(T keyHolder)
        {
            return this.Execute(this.Get, keyHolder, DefaulDatabaseOperation.Get);
        }

        /// <summary>
        /// Get one entry from db using current Entity as keyholder and existing command
        /// </summary>
        /// <param name="keyHolder">Object that holds the key</param>
        /// <param name="command">command to use</param>
        /// <returns>Affected rows</returns>
        public int Get(T keyHolder, IDbCommand command)
        {
            return command.GetFirstRow(this.Get, keyHolder);
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
        /// <returns>Affected rows</returns>
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

        #endregion


        #region IDataAccessLayerShared


        /// <summary>
        /// A datareader that allow to get specific Entity information and save it into an object
        /// </summary>
        /// <param name="reader">Datareader that allow read from Stream</param>
        /// <returns>Current DC</returns>
        IDataTransferObject IDataAccessLayerShared.Get(IDataRecord reader)
        {
            return this.Get(reader);
        }

        /// <summary>
        /// Get all data from table
        /// </summary>
        /// <param name="items">data from query where to save in</param>
        void IDataAccessLayerShared.Read(List<IDataTransferObject> items)
        {
            this.Read(items as List<T>);
        }

        /// <summary>
        /// Get all data from table
        /// </summary>
        /// <param name="items">data from query where to save in</param>
        /// <param name="command">Existing Command to use</param>
        void IDataAccessLayerShared.Read(List<IDataTransferObject> items, IDbCommand command)
        {
            this.Read(items as List<T>, command);
        }

        /// <summary>
        /// Get one entry from db using current Entity as keyholder
        /// </summary>
        /// <param name="keyHolder">Object that contains primary keys for the Stored procedure</param>
        void IDataAccessLayerShared.Get(IDataTransferObject keyHolder)
        {
            this.Get(keyHolder as T);
        }

        /// <summary>
        /// Get one entry from db using current Entity as keyholder
        /// </summary>
        /// <param name="keyHolder">Object that contains primary keys for the Stored procedure</param>
        /// <param name="command">Existing Command to use</param>
        void IDataAccessLayerShared.Get(IDataTransferObject keyHolder, IDbCommand command)
        {
            this.Get(keyHolder as T, command);
        }

        /// <summary>
        /// A datareader that allow to get specific Entity information and save it into an object
        /// </summary>
        /// <param name="item">current item to fill</param>
        /// <param name="reader">Datareader that allow read from Stream</param>
        void IDataAccessLayerShared.Get(IDataTransferObject item, IDataReader reader)
        {
            this.Get(item as T, reader);
        }

        /// <summary>
        /// Save full list of the Transfer objects
        /// </summary>
        /// <param name="data">Transfer data to save</param>
        void IDataAccessLayerShared.Save(IEnumerable<IDataTransferObject> data)
        {
            this.Save(data.OfType<T>().ToList());
        }
        #endregion
    }
}
