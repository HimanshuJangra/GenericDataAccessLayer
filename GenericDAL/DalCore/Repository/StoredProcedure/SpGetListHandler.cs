using System;
using System.Collections;
using System.Data;
using FastMember;
using L10n.Properties;

namespace DalCore.Repository.StoredProcedure
{
    /// <summary>
    /// allow return list as result
    /// </summary>
    public class SpGetListHandler : SpHandlerBase
    {
        /// <summary>
        /// Concrete implementation for List handler
        /// </summary>
        /// <param name="transit">return value as reference value</param>
        /// <param name="command">executive command</param>
        protected override void Execute(object transit, IDbCommand command)
        {
            if ((transit is ICollection) == false)
            {
                throw new ArgumentException(nameof(Resources.DA001), nameof(transit));
            }
            var items = transit as IList;
            var accessor = TypeAccessor.Create(ReturnType.GenericTypeArguments[0]);
            PrepareExecute(command);
            using (var reader = command.ExecuteReader())
            {
                int columns = reader.FieldCount;
                foreach (var record in reader.ToRecord())
                {
                    var item = accessor.CreateNew();
                    items?.Add(item);
                    for (int i = 0; i < columns; i++)
                    {
                        string name = record.GetName(i);
                        accessor[item, name] = record.GetValue(i);
                    }
                }
            }
        }
    }
}
