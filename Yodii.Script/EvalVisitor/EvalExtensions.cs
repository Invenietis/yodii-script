using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Yodii.Script
{
    public static class EvalExtensions
    {
        /// <summary>
        /// Returns the next accessor frames in the chain, optionally starting with this frame.
        /// </summary>
        /// <param name="this">This frame.</param>
        /// <param name="withThis">True to start with this accessor.</param>
        /// <returns>The next accessors in the chain.</returns>
        public static IEnumerable<IAccessorFrame> NextAccessors( this IAccessorFrame @this, bool withThis = false )
        {
            IAccessorFrame n = withThis ? @this : @this.NextAccessor;
            while( n != null )
            {
                yield return n;
                n = n.NextAccessor;
            }
        }
    }

}
