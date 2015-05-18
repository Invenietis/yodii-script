using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Yodii.Script.Debugger
{
    class DynamicScopeDebugger : DynamicScope
    {
        public RefRuntimeObj FindByName( string name )
        {
            return null;
        }

        public override RefRuntimeObj Register( AccessorLetExpr local )
        {
            return base.Register( local );
        }
    }
}
