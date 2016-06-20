using GenericDataAccessLayer.Core.DatabaseAccess;
using SharedComponents.Data;

namespace GenericDataAccessLayer.Core.MultiItemProcessor
{
    /// <summary>
    /// Extended version of Data Access Layer
    /// </summary>
    /// <typeparam name="T">Data Transfer Object</typeparam>
    public abstract class DalBaseExtended<T> : DataAccessLayerShared<T>
           where T : class, IDataTransferObject, new()
    {
        public DalBaseExtended()
            : base()
        {
            
        }
        /// <summary>
        /// Use DAL with specific Connection String Information
        /// </summary>
        /// <param name="configurationName"></param>
        public DalBaseExtended(string configurationName)
            : base(configurationName)
        {
        }

        /// <summary>
        /// Use DAK with disconnected Configuration.
        /// </summary>
        /// <param name="connectionString"></param>
        /// <param name="providerName"></param>
        public DalBaseExtended(string connectionString, string providerName)
            : base(connectionString, providerName)
        {
        }
    }
}
