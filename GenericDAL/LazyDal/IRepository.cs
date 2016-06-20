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
        /// Total time for query execution in ms
        /// </summary>
        long QueryExecutionTime { get; }

        /// <summary>
        /// Execution time for total process
        /// </summary>
        long ExecutionTime { get; }
    }
}
