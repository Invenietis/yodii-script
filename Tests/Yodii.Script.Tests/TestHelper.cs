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
            RuntimeObj syncResult = ScriptEngine.Evaluate( script, ctx );
            test( syncResult );
            ScriptEngine engine = new ScriptEngine( ctx );
            engine.Breakpoints.BreakAlways = true;
            using( ScriptEngine.Result rAsync = engine.Execute( script ) )
            {
                int nbStep = 0;
                while( rAsync.Status == ScriptEngineStatus.IsPending )
                {
                    ++nbStep;
                    rAsync.Continue();
                }
                test( rAsync.CurrentResult );
                Console.WriteLine( "Script '{0}' => {1} evaluated in {2} steps.", script, syncResult.ToString(), nbStep );
            }
        }

    }
}
