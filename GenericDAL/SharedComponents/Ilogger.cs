using System;
using System.Data;

namespace SharedComponents
{
    /// <summary>
    /// basic logger logic
    /// </summary>
    public interface ILogger
    {
        /// <summary>
        /// Write a log entry and decide what should happen to transaction
        /// </summary>
        /// <param name="e">Exception to log</param>
        /// <param name="transaction">current transaction used for this operation</param>
        /// <returns>If true, step into exception catch block</returns>
        bool WriteLog(Exception e, IDbTransaction transaction);

        /// <summary>
        /// Write a log entry
        /// </summary>
        /// <param name="e">Exception to log</param>
        /// <returns>If true, step into exception catch block</returns>
        bool WriteLog(Exception e);
    }
}
