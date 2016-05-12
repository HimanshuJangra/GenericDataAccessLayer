using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Text;
using SharedComponents;
using Localization = L10n.Properties.Resources;

namespace DalCore
{
    /// <summary>
    /// Base definition of the Database Access
    /// </summary>
    public class BasicDbAccess : IBasicDbAccess
    {
        /// <summary>
        /// Get logger for exception handling
        /// </summary>
        public virtual IEnumerable<ILogger> Logger => FastAccess.Instance.DefaultLoggers;

        /// <summary>
        /// If true, connection will be automatically closed, else keep connection open
        /// </summary>
        public Boolean CloseConnectionOnDispose { get; set; } = true;

        private static readonly System.Collections.Concurrent.ConcurrentDictionary<int, IDbConnection> Connections = new System.Collections.Concurrent.ConcurrentDictionary<int, IDbConnection>();

        /// <summary>
        /// Create a new DbConnection and open it
        /// </summary>
        public virtual IDbConnection OpenConnection()
        {
            // create factory only on request connection
            if (this.Factory == null)
            {
                this.CreateFactory();
            }

            IDbConnection currentConnection;
            int threadId = System.Threading.Thread.CurrentThread.ManagedThreadId;
            if (Connections.TryGetValue(threadId, out currentConnection) == false)
            {
                currentConnection = this.Factory.CreateConnection();
                Connections.TryAdd(threadId, currentConnection);

                currentConnection.ConnectionString = this._connectionDesciption.ConnectionString;
                currentConnection.Open();
            }
            return currentConnection;
        }

        public static void OverrideConnection(IDbConnection connection)
        {
            Connections.AddOrUpdate(System.Threading.Thread.CurrentThread.ManagedThreadId, connection, (id, con) => connection);
        }
        /// <summary>
        /// Open new Connection and begin transaction
        /// </summary>
        /// <param name="connection">current connection</param>
        /// <returns></returns>
        public IDbTransaction OpenConnectionWithTransaction(out IDbConnection connection)
        {
            connection = this.OpenConnection();
            return connection.BeginTransaction();
        }

        /// <summary>
        /// Current Database Provider. No private setter for Intergration Tests
        /// </summary>
        public System.Data.Common.DbProviderFactory Factory { get; set; }
        /// <summary>
        /// Connection String Information
        /// </summary>
        private readonly ConnectionStringSettings _connectionDesciption;
        /// <summary>
        /// Use DAL with specific Connection String Information
        /// </summary>
        /// <param name="providerName"></param>
        public BasicDbAccess(String providerName)
        {
            this._connectionDesciption = ConfigurationManager.ConnectionStrings[providerName];
        }
        /// <summary>
        /// Use DAK with disconnected Configuration.
        /// </summary>
        /// <param name="connectionString"></param>
        /// <param name="providerName"></param>
        public BasicDbAccess(String connectionString, String providerName)
        {
            this._connectionDesciption = new ConnectionStringSettings("DisconnectedConnection", connectionString, providerName);
        }
        /// <summary>
        /// for the future use... if exented
        /// </summary>
        private void CreateFactory()
        {
            this.Factory = System.Data.Common.DbProviderFactories.GetFactory(this._connectionDesciption.ProviderName);
        }

        private IDbConnection GetConnection(IDbConnection externalConnection)
        {
            IDbConnection connection = null;
            if (externalConnection != null)
            {
                connection = externalConnection;
                if (connection.State == ConnectionState.Broken || connection.State == ConnectionState.Closed)
                {
                    connection.Open();
                }
            }
            else
            {
                connection = this.OpenConnection();
            }

            return connection;
        }
        /// <summary>
        /// Write into all loggers and rollback transaction if return value is false
        /// </summary>
        /// <param name="e">exception to log</param>
        /// <returns>true if all logger returns true</returns>
        protected virtual bool WriteToAllLoggers(Exception e)
        {
            Boolean result = true;
            foreach (var logger in this.Logger)
            {
                result &= logger.WriteLog(e);
            }
            return result;
        }
        /// <summary>
        /// Write into all loggers and rollback transaction if return value is false
        /// </summary>
        /// <param name="e">exception to log</param>
        /// <param name="transaction">current transaction in use</param>
        /// <returns>true if all logger returns true</returns>
        protected virtual bool WriteToAllLoggers(Exception e, IDbTransaction transaction)
        {
            Boolean result = true;
            foreach (var logger in this.Logger)
            {
                result &= logger.WriteLog(e);
            }

            if (result == false)
            {
                transaction?.Rollback();
            }

            return result;
        }

        /// <summary>
        /// Execute Remote Procedure Call (can be anything that go to database)
        /// </summary>
        /// <typeparam name="TItem">Generic Item that pass to execution engine</typeparam>
        /// <param name="execute">Execution engine</param>
        /// <param name="data">data for execution engine</param>
        /// <param name="externalConnection">re-useable connection, if nut null</param>
        public void Execute<TItem>(Action<TItem, IDbCommand> execute, TItem data, IDbConnection externalConnection = null)
        {
            IDbConnection connection = null;

            try
            {
                connection = this.GetConnection(externalConnection);

                using (IDbCommand command = connection.CreateCommand())
                {
                    command.Connection = connection;
                    execute(data, command);
                }
            }
            catch (Exception e) when (this.WriteToAllLoggers(e))
            {
            }
            finally
            {
                if (externalConnection == null && connection != null)
                {
                    connection.Dispose();
                }
            }
        }

        /// <summary>
        /// Execute Remote Procedure Call (can be anything that go to database)
        /// </summary>
        /// <typeparam name="TItem">Generic Item that pass to execution engine</typeparam>
        /// <param name="execute">Execution engine</param>
        /// <param name="data">data for execution engine</param>
        /// <param name="externalConnection">re-useable optional connection, if nut null</param>
        /// <param name="externalTransaction">re-useable optional transaction</param>
        /// <param name="useTransaction">if true, transaction will be initialized (optional)</param>
        public void Execute<TItem>(Action<TItem, IDbCommand> execute, TItem data, IDbConnection externalConnection = null, IDbTransaction externalTransaction = null, Boolean useTransaction = false)
        {
            IDbConnection connection = null;
            IDbTransaction transaction = null;

            try
            {
                #region Connection/Transaction Configs
                connection = this.GetConnection(externalConnection);
                if (externalTransaction != null)
                {
                    transaction = externalTransaction;
                    if (transaction.Connection != connection)
                    {
                        throw new ArgumentException(nameof(Localization.DE002));
                    }
                }
                else if (useTransaction == true)
                {
                    transaction = connection.BeginTransaction();
                }
                #endregion

                using (IDbCommand command = connection.CreateCommand())
                {
                    command.Connection = connection;
                    command.Transaction = transaction;
                    execute(data, command);
                }
            }
            catch (Exception e) when (this.WriteToAllLoggers(e, transaction))
            {
                if (transaction != null)
                {
                    transaction.Rollback();
                }
            }
            finally
            {
                if (externalTransaction == null && transaction != null)
                {
                    transaction.Commit();
                    transaction.Dispose();
                }
                if (externalConnection == null && connection != null)
                {
                    connection.Dispose();
                }
            }
        }
        /// <summary>
        /// Remove connection for the own Thread
        /// </summary>
        internal static void DisposeConnection()
        {
            IDbConnection item = null;
            if (Connections.TryRemove(System.Threading.Thread.CurrentThread.ManagedThreadId, out item) == true)
            {
                item.Dispose();
            }
        }
        /// <summary>
        /// Free resources
        /// </summary>
        public void Dispose()
        {
            if (this.CloseConnectionOnDispose == true)
            {
                IDbConnection removed = null;
                if (Connections.TryRemove(System.Threading.Thread.CurrentThread.ManagedThreadId, out removed) == true)
                {
                    removed.Dispose();
                }
            }
        }
    }
}
