using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DalCore.DatabaseAccess
{
    /// <summary>
    /// Using MEF to access a specific DAL
    /// </summary>
    public interface ITypeAccessor
    {
        /// <summary>
        /// Type of the generic parameter in DAL that allow distinguish each dal implemented
        /// </summary>
        Type ParameterType { get; }
        /// <summary>
        /// Concrete type of the Data Access Layer
        /// </summary>
        Type ConcreteType { get; }
    }
}
