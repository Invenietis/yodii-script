using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Yodii.Script;

namespace SimpleConsole
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var c = new GlobalContext();
            c.Register("TheConsole.Out.Print", (Action<string>)(s => Console.WriteLine(s)));
            c.Register("TheConsole.Read", (Func<string>)Console.ReadLine);
            string script = @"
                            let r;
                            TheConsole.Out.Print( 'Type exit to... exit' );
                            while( (r = TheConsole.Read()) != 'exit' )
                            {
                                TheConsole.Out.Print( 'You type: ' + r );
                            }
                            TheConsole.Out.Print( 'Bye bye!' );
                        ";
            ScriptEngine.Evaluate(script, c);
        }
    }
}
