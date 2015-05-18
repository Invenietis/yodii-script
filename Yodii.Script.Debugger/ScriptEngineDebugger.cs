using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Yodii.Script.Debugger
{
    public class ScriptEngineDebugger : ScriptEngine, IVariableList
    {
        List<Variable> _variables = new List<Variable>();
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
            //ScopeManager.FindByName( "hh" );
        }

        #region IVariableList Members

        public List<Variable> Vars
        {
            get { return _variables; }
        }

        public Variable FindByName( string name )
        {
            return _variables.Find( v => v.Name == name );
        }

        #endregion
    }
}
