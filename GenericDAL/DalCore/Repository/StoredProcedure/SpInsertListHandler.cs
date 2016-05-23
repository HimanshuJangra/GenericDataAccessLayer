using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using FastMember;
using L10n.Properties;
using SharedComponents;

namespace DalCore.Repository.StoredProcedure
{
    public class SpInsertListHandler : SpHandlerBase
    {
        /// <summary>
        /// Execute definition
        /// </summary>
        /// <param name="transit">return value as reference value</param>
        /// <param name="command">executive command</param>
        protected override void Execute(object transit, IDbCommand command)
        {
            var items = transit as IList;
            Type refString = Type.GetType("System.String&");

            var saveList = Parameters.Select(a=>a.Value).FirstOrDefault(a => a is IList) as IList;
            if (saveList == null)
            {
                throw new ArgumentException(nameof(Resources.DA002), nameof(saveList));
            }

            var parameterType = TypeAccessor.Create(saveList.GetType().GenericTypeArguments[0]);
            foreach (var saveItem in saveList)
            {
                foreach (var item in parameterType.GetMembers())
                {
                    int? size = null;
                    if (item.Type.In(typeof(string), refString))
                    {
                        size = -1;
                    }
                    command.AddParameter($"@{item.Name}", parameterType[saveItem, item.Name], size: size);
                }
                PrepareExecute(command);
                using (var reader = command.ExecuteReader())
                {
                    int columns = reader.FieldCount;
                    foreach (var record in reader.ToRecord())
                    {
                        items?.Add(saveItem);
                        for (int i = 0; i < columns; i++)
                        {
                            string name = record.GetName(i);
                            parameterType[saveItem, name] = record.ReadObject(i);
                        }
                    }
                }
            }
        }

        protected override bool ParameterValidation(ParameterInfo pi)
        {
            return pi.ParameterType?.GetInterface(nameof(IList)) == null;
        }
    }
}
