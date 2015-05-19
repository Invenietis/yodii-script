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
                while( r2.Status == ScriptEngineStatus.Breakpoint )
                {
                    Assert.That( (r2.Status & ScriptEngineStatus.IsPending), Is.EqualTo( ScriptEngineStatus.IsPending ) );
                    nbStep++;
                    r2.Continue();
                }

                Assert.That( r2.Status, Is.EqualTo( ScriptEngineStatus.IsFinished ) );
                Assert.That( nbStep, Is.EqualTo( 1 ) );
            }
        }
        [Test]
        public void check_the_debuggers_components()
        {
            ScriptEngineDebugger engine = new ScriptEngineDebugger(new GlobalContext());
            string script = @"let a,b,c;
                               a=5;
                               b=2;
                               c= a+b ";

            Expr exp = ExprAnalyser.AnalyseString( script );

            BreakableVisitor bkv = new BreakableVisitor();
            bkv.VisitExpr( exp );
            Assert.That( bkv.BreakableExprs.Count, Is.EqualTo( 4 ) );
            engine.Breakpoints.AddBreakpoint( bkv.BreakableExprs[3] );

            using( var r2 = engine.Execute( exp ) )
            {
                Assert.That( engine.ScopeManager.Vars.Count, Is.EqualTo( 3 ) );

                r2.Continue();
            }
        }
        [Test]
        public void show_vars_from_closure()
        {
            ScriptEngineDebugger engine = new ScriptEngineDebugger(new GlobalContext());
            string script = @"let a = 0;
                              let b = 1;
                              function testfunc(){
                                let b = 2;
                                a = 'test';
                                a = 5;
                               }
                              testfunc();
                              let c = 0;
";

            Expr exp = ExprAnalyser.AnalyseString( script );

            BreakableVisitor bkv = new BreakableVisitor();
            bkv.VisitExpr( exp );
            engine.Breakpoints.AddBreakpoint( bkv.BreakableExprs[5] );
            
            using( var r2 = engine.Execute( exp ) )
            {
                Assert.That( engine.ScopeManager.FindByName( "a" ).Object.ToString(), Is.EqualTo( "test" ) );
                Assert.That( engine.ScopeManager.FindByName( "b" ).Object.ToDouble(), Is.EqualTo( 2.0 ) );
                r2.Continue();
            }
            engine.Breakpoints.RemoveBreakpoint( bkv.BreakableExprs[5] );
            engine.Breakpoints.AddBreakpoint( bkv.BreakableExprs[7] );
            using( var r2 = engine.Execute( exp ) )
            {
                Assert.That( engine.ScopeManager.FindByName( "a" ).Object.ToDouble(), Is.EqualTo( 5.0 ) );
                Assert.That( engine.ScopeManager.FindByName( "b" ).Object.ToDouble(), Is.EqualTo( 1.0 ) );
                r2.Continue();
            }
        }
    }
}
