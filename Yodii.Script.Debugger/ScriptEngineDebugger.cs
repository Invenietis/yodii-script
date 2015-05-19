using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Yodii.Script.Debugger
{
    public class ScriptEngineDebugger : ScriptEngine
    {
        
        public ScriptEngineDebugger( GlobalContext ctx )
            : base( ctx, null, new DynamicScopeDebugger() )
        {
        }

        public new DynamicScopeDebugger ScopeManager
        {
            get { return (DynamicScopeDebugger)base.ScopeManager; }
        }

        public void ShowDebug()
        {
            throw new NotImplementedException();           
        }
    }
}
