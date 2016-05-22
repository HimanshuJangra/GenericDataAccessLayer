using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using SharedComponents;

namespace DalCore.DatabaseAccess
{
    /// <summary>
    /// Default Descriptor for Basic Database Access
    /// </summary>
    public interface IBasicDbAccess : IDisposable
    {
        /// <summary>
        /// If true, close connection on Dispose
        /// </summary>
        bool CloseConnectionOnDispose { get; set; }
        
        /// <summary>
        /// Database Provider Factory. Manage Database Connection(s)
        /// </summary>
        DbProviderFactory Factory { get; set; }

        /// <summary>
        /// Internal Logger
        /// </summary>
        IEnumerable<Lazy<ILogger>> Logger { get; }

        /// <summary>
        /// Refresh disconnected connection
        /// </summary>
        /// <param name="providerName">Configuration connection string name</param>
        void RefreshConnectionSettings(string providerName);

        /// <summary>
        /// Refresh disconnected connection
        /// </summary>
        /// <param name="connectionString">connection string</param>
        /// <param name="providerName">SQL Provider</param>
        void RefreshConnectionSettings(String connectionString, String providerName);

        /// <summary>
        /// Execute Remote Procedure Call (can be anything that go to database)
        /// </summary>
        /// <typeparam name="TItem">Generic Item that pass to execution engine</typeparam>
        /// <param name="execute">Execution engine</param>
        /// <param name="data">data for execution engine</param>
        /// <param name="externalConnection">re-useable optional connection, if nut null</param>
        /// <param name="externalTransaction">re-useable optional transaction</param>
        /// <param name="useTransaction">if true, transaction will be initialized (optional)</param>
        void Execute<TItem>(Action<TItem, IDbCommand> execute, TItem data, IDbConnection externalConnection = null, IDbTransaction externalTransaction = null, bool useTransaction = false);

        /// <summary>
        /// Execute Remote Procedure Call (can be anything that go to database)
        /// </summary>
        /// <typeparam name="TItem">Generic Item that pass to execution engine</typeparam>
        /// <param name="execute">Execution engine</param>
        /// <param name="data">data for execution engine</param>
        /// <param name="externalConnection">re-useable connection, if nut null</param>
        void Execute<TItem>(Action<TItem, IDbCommand> execute, TItem data, IDbConnection externalConnection = null);
        
        /// <summary>
        /// Manage SQL connections. If no connection is initialized, it will create one, else take existing
        /// </summary>
        /// <returns></returns>
        IDbConnection OpenConnection();
        
        /// <summary>
        /// Same as <see cref="OpenConnection"/>. Open transaction if needed 
        /// </summary>
        /// <param name="connection">Connection that has been created or retrieved</param>
        /// <returns>Transaction that belongs to Connection</returns>
        IDbTransaction OpenConnectionWithTransaction(out IDbConnection connection);
    }
}