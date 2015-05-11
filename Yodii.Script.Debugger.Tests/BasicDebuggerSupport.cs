using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Yodii.Script.Debugger.Tests
{
    [TestFixture]
    class BasicDebuggerSupport
    {
        [TestCase("let a;", 0)]
        [TestCase( "let a=0;",1 )]
        [TestCase( "let a,b;a=5; b=2; ", 2 )]
        [TestCase( "let a,b,c;a=5; b=2;c= a+b ", 4 )]
        public void check_if_breakpoints_count_matches(string script, int count)
        {          
            ScriptEngine engine = new ScriptEngine();

            Expr exp = ExprAnalyser.AnalyseString( script );

            BreakableVisitor bkv = new BreakableVisitor();
            bkv.VisitExpr( exp );
            Assert.That( bkv.BreakableExprs.Count, Is.EqualTo(count) );
        }
        [Test]
        public void add_breakpoint_inside_parsed_script()
        {
            ScriptEngine engine = new ScriptEngine();
            string script = @"let a,b,c;
                               a=5;
                               b=2;
                               c= a+b ";

            Expr exp = ExprAnalyser.AnalyseString( script );

            BreakableVisitor bkv = new BreakableVisitor();
            bkv.VisitExpr( exp );
            Assert.That( bkv.BreakableExprs.Count, Is.EqualTo(4) );
            engine.Breakpoints.AddBreakpoint( bkv.BreakableExprs[3] );
            
            using( var r2 = engine.Execute( exp ) )
            {
                int nbStep = 0;
                while( r2.Status == ScriptEngineStatus.IsPending )
                {
                    nbStep++;
                    r2.Continue();
                }

                Assert.That( r2.Status, Is.EqualTo( ScriptEngineStatus.IsFinished ) );
                Assert.That( nbStep, Is.EqualTo( 1 ) );
            }
        }
        [Test]
        public void show_vars_from_the_scope()
        {
            ScriptEngine engine = new ScriptEngine();
            string script = @"let a = 0 let b ='chaine';let c;";

            Expr exp = ExprAnalyser.AnalyseString( script );

            BreakableVisitor bkv = new BreakableVisitor();
            bkv.VisitExpr( exp );
           
            engine.Breakpoints.AddBreakpoint( bkv.BreakableExprs[0] );
            engine.Breakpoints.AddBreakpoint( bkv.BreakableExprs[1] );
            using( var r2 = engine.Execute( exp ) )
            {
                Assert.That( engine.Vars["a"].Type, Is.EqualTo( RuntimeObj.TypeUndefined) );
                r2.Continue();
                Assert.That( engine.Vars["a"].ToDouble(), Is.EqualTo( 0.0 ) );
                r2.Continue();
            }
        }
    }
}
