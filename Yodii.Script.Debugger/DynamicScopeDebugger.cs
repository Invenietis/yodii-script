using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Yodii.Script.Debugger
{
    public class DynamicScopeDebugger : DynamicScope, IVariableList
    {
        List<Variable> _variables;

        public DynamicScopeDebugger()
            : base()
        {
            _variables = new List<Variable>();
        }

        public override RefRuntimeObj Register( AccessorLetExpr local )
        {
            RefRuntimeObj O = base.Register(local);
            _variables.Add( new Variable( local.Name, O ) );
            return O;
        }
        public override RefRuntimeObj Register( Closure c )
        {
            _variables.Add( new Variable( c.Variable.Name, c.Ref ) );
            return base.Register( c );
        }
        public override void Unregister( AccessorLetExpr decl )
        {
            _variables.Remove( _variables.FindLast( v => v.Name == decl.Name ) );
            base.Unregister( decl );
        }
        #region IVariableList Members

        public List<Variable> Vars
        {
            get { return _variables; }
        }

        public Variable FindByName( string name )
        {
            return _variables.FindLast( v => v.Name == name );
        }

        #endregion
    }
}
