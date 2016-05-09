using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharedDal.SingleItemProcessor
{
    /// <summary>
    /// Generic implementation of the current Data Access Layer
    /// </summary>
    /// <typeparam name="T">Entity to handle</typeparam>
    public interface IDalGeneric<T> : IDataAccessLayer
    {
        /// <summary>
        /// Entity that represents Interface between database and transfer objects
        /// </summary>
        T DataContract { get; set; }
        /// <summary>
        /// Checks if Data Contract exists in current instance
        /// </summary>
        Boolean HasContract { get; }
        /// <summary>
        /// Get one entry from db using current Entity as keyholder
        /// </summary>
        /// <param name="keyHolder">Object that contains primary keys for the Stored procedure</param>
        void Get(T keyHolder);

        /// <summary>
        /// Write into given list all Items
        /// </summary>
        /// <param name="items">Empty (?) list for Items</param>
        void Read(IList<T> items);

        /// <summary>
        /// Write into given list all Items
        /// </summary>
        /// <param name="items">Empty (?) list for Items</param>
        /// <param name="command">Existing Command to use</param>
        void Read(IList<T> items, System.Data.IDbCommand command);
        /// <summary>
        /// Read Entity and return it completelly
        /// </summary>
        /// <param name="reader">Database Reader</param>
        /// <returns>Entity</returns>
        T GetDataContract(System.Data.IDataReader reader);
    }
}
