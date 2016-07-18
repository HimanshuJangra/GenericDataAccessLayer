using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GenericDataAccessLayer.LazyDal
{
    /// <summary>
    /// Base Interface that mark further interfaces as repositories
    /// </summary>
    public interface IRepository : IDisposable
    {
        /// <summary>
        /// name of the connnection string settings configuration.
        /// If not defined, "DefaultConnection" will be used as connection string
        /// </summary>
        string ConnectionStringSettings { get; set; }

        /// <summary>
        /// Current connection in use
        /// </summary>
        IDbConnection Connection { get; set; }

        /// <summary>
        /// Total time for query execution
        /// </summary>
        TimeSpan? QueryExecutionTime { get; }

        /// <summary>
        /// Execution time for total process
        /// </summary>
        TimeSpan? TotalExecutionTime { get; }

        /// <summary>
        /// Enable or disable some key features
        /// </summary>
        RepositoryOperations Operations { get; set; }
        /// <summary>
        /// Name Convension of the TVP. Default: {0}TVP
        /// </summary>
        string TvpNameConvension { get; set; }
    }
    /// <summary>
    /// Additional operations for Repository
    /// </summary>
    [Flags]
    public enum RepositoryOperations
    {
        /// <summary>
        /// Remove all extended operations
        /// </summary>
        None,
        /// <summary>
        /// Enable using Table Valued Parameter => List will be converted to Table in SQL
        /// </summary>
        UseTableValuedParameter = 1,
        /// <summary>
        /// Log Total Execution Time for a whole "Process"
        /// </summary>
        LogTotalExecutionTime = 2,
        /// <summary>
        /// Log Database Execution Time.
        /// </summary>
        LogQueryExecutionTime = 4,
        /// <summary>
        /// Any exception that happens during stored procedure execution will be ignored
        /// </summary>
        IgnoreException = 8,
        /// <summary>
        /// Init only Log Execution watches
        /// </summary>
        TimeLoggerOnly = LogTotalExecutionTime | LogQueryExecutionTime,
        /// <summary>
        /// Include all operations
        /// </summary>
        All = UseTableValuedParameter | LogTotalExecutionTime | LogQueryExecutionTime | IgnoreException
    }
}
