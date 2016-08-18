using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Yodii.Script
{
    /// <summary>
    /// Defines the kind of a pending <see cref="PExpr"/>. 
    /// </summary>
    public enum PExprKind
    {
        /// <summary>
        /// Not pending.
        /// </summary>
        None = 0,

        /// <summary>
        /// A breakpoint has been reached.
        /// </summary>
        Breakpoint = 1,

        /// <summary>
        /// An asynchronous call is being processed.
        /// </summary>
        AsyncCall = 2,

        /// <summary>
        /// A timeout occurred.
        /// </summary>
        Timeout = 3,

        /// <summary>
        /// An error occured.
        /// </summary>
        FirstChanceError = 4

    }

}
