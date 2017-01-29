using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Yodii.Script
{
    /// <summary>
    /// Exposes 'with object' scope stack.
    /// </summary>
    public interface IWithObjectScope : IDisposable
    {
        /// <summary>
        /// Gets the parent scope. Null for the first opened scope.
        /// </summary>
        IWithObjectScope Parent { get; }

        /// <summary>
        /// Gets the scope object.
        /// </summary>
        RuntimeObj Object { get; }
    }

}
