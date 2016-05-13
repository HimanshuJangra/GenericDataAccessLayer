using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharedComponents
{
    /// <summary>
    /// Basic Interface for configuration
    /// </summary>
    public interface IConfiguration
    {
        /// <summary>
        /// save current configuration
        /// </summary>
        void Save();
    }
}
