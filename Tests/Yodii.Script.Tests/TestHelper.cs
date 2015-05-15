using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Yodii.Script.Tests
{
    static class TestHelper
    {

        static public void RunNormalAndStepByStep( string script, Action<RuntimeObj> test, GlobalContext ctx = null )
        {
            var e = ExprAnalyser.AnalyseString( script );

            // Tests the empty, default, visitor: no change must have been made to the AST. 
            var emptyVisitor = new ExprVisitor();
            Assert.That( emptyVisitor.VisitExpr( e ), Is.SameAs( e ) );

            // Evaluates result directly.
            RuntimeObj syncResult = ScriptEngine.Evaluate( e, ctx );
            test( syncResult );

            // Step-by-step evaluation.
            ScriptEngine engine = new ScriptEngine( ctx );
            engine.Breakpoints.BreakAlways = true;
            ExecAsync( script, test, null, e, syncResult, engine, true );
        }

        static public void RunNormalAndStepByStepWithFirstChanceError( string script, Action<RuntimeObj> test, int expectedFirstChanceError, GlobalContext ctx = null )
        {
            var e = ExprAnalyser.AnalyseString( script );

            // Tests the empty, default, visitor: no change must have been made to the AST. 
            var emptyVisitor = new ExprVisitor();
            Assert.That( emptyVisitor.VisitExpr( e ), Is.SameAs( e ) );

            // Evaluates result directly.
            RuntimeObj syncResult = ScriptEngine.Evaluate( e, ctx );
            test( syncResult );

            // Evaluates result without break points but with EnabledFirstChanceError set.
            ScriptEngine engine = new ScriptEngine( ctx );
            engine.EnableFirstChanceError = true;
            ExecAsync( script, test, expectedFirstChanceError, e, syncResult, engine, false );

            // Step-by-step evaluation (with EnabledFirstChanceError set).
            engine.Breakpoints.BreakAlways = true;
            ExecAsync( script, test, expectedFirstChanceError, e, syncResult, engine, true );
        }

        static void ExecAsync( string script, Action<RuntimeObj> test, int? expectedFirstChanceError, Expr e, RuntimeObj syncResult, ScriptEngine engine, bool displayResult )
        {
            using( ScriptEngine.Result rAsync = engine.Execute( e ) )
            {
                int nbFirstChanceError = 0;
                int nbStep = 0;
                while( rAsync.CanContinue )
                {
                    if( rAsync.Status == ScriptEngineStatus.FirstChanceError ) ++nbFirstChanceError;
                    ++nbStep;
                    rAsync.Continue();
                }
                test( rAsync.CurrentResult );
                if( expectedFirstChanceError.HasValue ) Assert.That( nbFirstChanceError, Is.EqualTo( expectedFirstChanceError.Value ) );
                if( displayResult ) Console.WriteLine( "Script '{0}' => {1} evaluated in {2} steps ({3} first chance errors).", script, syncResult.ToString(), nbStep, nbFirstChanceError );
            }
        }

    }
}
