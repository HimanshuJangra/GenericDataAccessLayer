using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Text;
using SharedComponents;
using Localization = L10n.Properties.Resources;
using DalCore.DatabaseAccess;

namespace DalCore
{
    /// <summary>
    /// Base definition of the Database Access
    /// </summary>
    public class BasicDbAccess : IBasicDbAccess
    {
        /// <summary>
        /// Name of the default Sql Connection String name in config file
        /// </summary>
        const string DefaultConnection = "DefaultConnection";
        /// <summary>
        /// Get logger for exception handling
        /// </summary>
        public virtual IEnumerable<Lazy<ILogger>> Logger => FastAccess.Instance.DefaultLoggers;

        /// <summary>
        /// If true, connection will be automatically closed, else keep connection open
        /// </summary>
        public Boolean CloseConnectionOnDispose { get; set; } = true;

        /// <summary>
        /// Collection of all active Connections used in system. 
        /// TODO: should be replaced by MemoryCache
        /// </summary>
        private static readonly System.Collections.Concurrent.ConcurrentDictionary<int, IDbConnection> Connections = new System.Collections.Concurrent.ConcurrentDictionary<int, IDbConnection>();

        /// <summary>
        /// Current Database Provider. No private setter for Intergration Tests
        /// </summary>
        public System.Data.Common.DbProviderFactory Factory { get; set; }

        /// <summary>
        /// Connection String Information
        /// </summary>
        private ConnectionStringSettings _connectionDesciption;

        /// <summary>
        /// Empty Constructor
        /// </summary>
        public BasicDbAccess()
            : this(DefaultConnection)
        {

        }

        /// <summary>
        /// Use DAL with specific Connection String Information
        /// </summary>
        /// <param name="configurationName"></param>
        public BasicDbAccess(String configurationName)
        {
            this.RefreshConnectionSettings(configurationName);
        }

        /// <summary>
        /// Use DAK with disconnected Configuration.
        /// </summary>
        /// <param name="connectionString"></param>
        /// <param name="providerName"></param>
        public BasicDbAccess(String connectionString, String providerName)
        {
            this.RefreshConnectionSettings(connectionString, providerName);
        }

        /// <summary>
        /// Refresh disconnected connection
        /// </summary>
        /// <param name="providerName">Configuration connection string name</param>
        public void RefreshConnectionSettings(string providerName)
        {
            this._connectionDesciption = ConfigurationManager.ConnectionStrings[providerName];
        }

        /// <summary>
        /// Refresh disconnected connection
        /// </summary>
        /// <param name="connectionString">connection string</param>
        /// <param name="providerName">SQL Provider</param>
        public void RefreshConnectionSettings(String connectionString, String providerName)
        {
            this._connectionDesciption = new ConnectionStringSettings("DisconnectedConnection", connectionString, providerName);
        }

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

        /// <summary>
        /// Create new Database Factory which can create new connection
        /// </summary>
        private void CreateFactory()
        {
            this.Factory = System.Data.Common.DbProviderFactories.GetFactory(this._connectionDesciption.ProviderName);
        }

        /// <summary>
        /// Can override current connection with new one. It is not recommended do this.
        /// </summary>
        /// <param name="connection">new connection</param>
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
        /// Reopen external connection if broken/closed or open new if no external connection passed as reference
        /// </summary>
        /// <param name="externalConnection">External Connection</param>
        /// <returns>External or new Connection</returns>
        private IDbConnection GetConnection(IDbConnection externalConnection)
        {
            IDbConnection connection = null;
            if (externalConnection != null)
            {
                connection = externalConnection;
            }
            else
            {
                connection = this.OpenConnection();
            }
            if (connection.State == ConnectionState.Broken || connection.State == ConnectionState.Closed)
            {
                connection.Open();
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
            if (this.Logger != null)
            {
                foreach (var logger in this.Logger)
                {
                    result &= (logger?.Value?.WriteLog(e)).GetValueOrDefault();
                }
            }
            else
            {
                Console.WriteLine(e.Message);
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
                result &= (logger?.Value?.WriteLog(e)).GetValueOrDefault();
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
                    this.Dispose(true);
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
                    this.Dispose(true);
                }
            }
        }

        /// <summary>
        /// Remove connection for the own Thread
        /// </summary>
        public static void DisposeConnection()
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
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (disposing == true)
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

        ~BasicDbAccess()
        {
            this.Dispose(false);
        }
    }
}
