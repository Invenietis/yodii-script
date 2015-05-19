using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Yodii.Script.Debugger.Tests
{
    [TestFixture]
    class DynamicalVariableEdition
    {
        [Test]
        public void Check_if_variables_can_be_edited_on_breakpoint()
        {
            ScriptEngineDebugger engine = new ScriptEngineDebugger(new GlobalContext());

            string script = @"let a,b,c;
                               a=5;
                               b=2;
                               c= a+b;
                               let d=0;";

            Expr exp = ExprAnalyser.AnalyseString( script );

            BreakableVisitor bkv = new BreakableVisitor();
            bkv.VisitExpr( exp );
            engine.Breakpoints.AddBreakpoint( bkv.BreakableExprs[3] );
            engine.Breakpoints.AddBreakpoint( bkv.BreakableExprs[4] );
            
            using( var r2 = engine.Execute( exp ) )
            {
                
                RefRuntimeObj O = engine.ScopeManager.FindByName( "b" ).Object;
                O.Value = new JSEvalNumber( 5.0 );
                Assert.That( engine.ScopeManager.FindByName( "b" ).Object.Value.ToDouble(), Is.EqualTo( 5.0 ) );
                
                r2.Continue();

                Assert.That( engine.ScopeManager.FindByName( "c" ).Object.Value.ToDouble(), Is.EqualTo( 10.0 ) );
                
                r2.Continue();
                
            }                           
        }
    }
}
