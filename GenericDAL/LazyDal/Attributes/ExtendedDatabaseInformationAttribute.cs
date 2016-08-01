using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GenericDataAccessLayer.LazyDal.Attributes
{
    /// <summary>
    /// Allow define additional information for a database call.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Property, AllowMultiple = false)]
    public class ExtendedDatabaseInformationAttribute : Attribute
    {
        /// <summary>
        /// Default schema is "dbo" if not set
        /// </summary>
        public string Schema { get; private set; }

        /// <summary>
        /// The default Database is one from connection string
        /// </summary>
        public string Database { get; private set; }
        /// <summary>
        /// name of the stored procedure... dont use it, if not necessary
        /// </summary>
        public string CustomProcedureName { get; set; }

        /// <summary>
        /// Create new Extended Info with new default schema
        /// </summary>
        /// <param name="schema">schema name</param>
        public ExtendedDatabaseInformationAttribute(string schema)
        {
            Schema = string.IsNullOrWhiteSpace(schema) ? null : schema;
        }
        /// <summary>
        /// Create new Extended Info with custom database name and custom schema
        /// </summary>
        /// <param name="database">deferred database name from connection string</param>
        /// <param name="schema">deferred schema name</param>
        public ExtendedDatabaseInformationAttribute(string database, string schema)
        {
            Database = database;
            Schema = schema;
        }

        public string CreateCall(string procedureName)
        {
            if (string.IsNullOrEmpty(Database) && string.IsNullOrEmpty(Schema) && string.IsNullOrEmpty(CustomProcedureName))
            {
                throw new ArgumentException(L10n.Properties.Resources.GE003, $"{nameof(Database)} | {nameof(Schema)} | {nameof(CustomProcedureName)}");
            }
            string result = string.Empty;

            if (string.IsNullOrEmpty(Database) == false)
            {
                result = Database;
            }

            if (string.IsNullOrEmpty(result) && string.IsNullOrEmpty(Schema) == false)
            {
                result = Schema;
            }
            else if (string.IsNullOrEmpty(result) == false && string.IsNullOrEmpty(Schema) == false)
            {
                result = $"{result}.{Schema}";
            }
            
            if (string.IsNullOrEmpty(result) == false)
            {
                result = $"{result}.{CustomProcedureName ?? procedureName}";
            }
            else
            {
                result = CustomProcedureName;
            }

            return result;
        }
    }
}
