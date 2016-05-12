using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharedComponents.Data;

namespace DalCore.MultiItemProcessor
{
    /// <summary>
    /// Extended version of Data Access Layer
    /// </summary>
    /// <typeparam name="T">Data Transfer Object</typeparam>
    public abstract class DalBaseExtended<T>
           where T : class, IDataTransferObject, new()
    {
    }
}
