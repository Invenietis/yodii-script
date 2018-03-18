#region LGPL License
/*----------------------------------------------------------------------------
* This file (Tests\Yodii.Script.Tests\FunctionTests.cs) is part of Yodii-Script. 
*  
* Yodii-Script is free software: you can redistribute it and/or modify 
* it under the terms of the GNU Lesser General Public License as published 
* by the Free Software Foundation, either version 3 of the License, or 
* (at your option) any later version. 
*  
* Yodii-Script is distributed in the hope that it will be useful, 
* but WITHOUT ANY WARRANTY; without even the implied warranty of
* MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the 
* GNU Lesser General Public License for more details. 
* You should have received a copy of the GNU Lesser General Public License 
* along with Yodii-Script. If not, see <http://www.gnu.org/licenses/>. 
*  
* Copyright © 2007-2015, 
*     Invenietis <http://www.invenietis.com>, IN'TECH INFO <http://www.intechinfo.fr>
* All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using FluentAssertions;

namespace Yodii.Script.Tests
{

    [TestFixture]
    public class FunctionTests
    {
        [Test]
        public void functions_are_runtime_objects()
        {
            string s = @"function yo(a) { return 'yo' + a; }";
            TestHelper.RunNormalAndStepByStep( s, o =>
            {
                o.Should().BeOfType<FunctionObj>();
                var f = (FunctionObj)o;
                f.Expr.Parameters.Select( p => p.Name ).Should().Equal( new[] { "a" } );
            } );
        }

        [Test]
        public void functions_are_callable()
        {
            string s = @"function yo(a) { return 'yo' + a; }
                         yo('b');";
            TestHelper.RunNormalAndStepByStep( s, o =>
            {
                o.Should().BeOfType<StringObj>();
                o.ToString().Should().Be( "yob" );
            } );
        }

        [Test]
        public void functions_have_multiple_parameters_and_superfluous_actual_parameters_are_ignored()
        {
            string s = @"function F(a,b,c,d,e,f,g) { return a+b+c+d+e+f+g; }
                         F(1,2,3,4,5,6,7,8,9,10,11,12);";

            TestHelper.RunNormalAndStepByStep( s, o =>
            {
                o.Should().BeOfType<DoubleObj>();
                o.ToDouble().Should().Be( 1.0 + 2 + 3 + 4 + 5 + 6 + 7 );
            } );
        }

        [Test]
        public void functions_are_first_class_objects()
        {
            string s = @" 
                            function gen() 
                            { 
                              return function(a,b) { return a+b; };
                            }
                            let f = gen();
                            f( 'x', 'y' );
                        ";
            TestHelper.RunNormalAndStepByStep( s, o =>
            {
                o.Should().BeOfType<StringObj>();
                o.ToString().Should().Be( "xy" );
            } );
        }

        [Test]
        public void closure_is_supported()
        {
            string s = @"
                        function next() 
                        { 
                          let _seed = 0; 
                          return function() { return ++_seed; };
                        }
                        let f = next();
                        f() + f() + f();
                        ";
            TestHelper.RunNormalAndStepByStep( s, o =>
            {
                o.Should().BeOfType<DoubleObj>();
                o.ToDouble().Should().Be( 1.0 + 2 + 3 );
            } );
        }

        [Test]
        public void parameters_hide_closure_variables()
        {
            string s = @"let _seed = 0;
                         function next()
                         {
                             return function(_seed) { return ++_seed; };
                         }
                         let f = next();
                         f(0) + f(0);";
            TestHelper.RunNormalAndStepByStep( s, o =>
            {
                o.Should().BeOfType<DoubleObj>();
                o.ToDouble().Should().Be( 2.0 );
            } );
        }

        [Test]
        public void intializing_a_masking_variable_from_the_one_it_masks()
        {
            string s = @"let x = 10;
                         function counterBasedOnX()
                         {
                             let x = x;
                             return function() { return ++x; };
                         }
                         let f = counterBasedOnX();
                         // First call to f returns 11, second 12.
                         // While x here is still 10.
                         f() + f() + 1000*x;";
            TestHelper.RunNormalAndStepByStep( s, o =>
            {
                o.Should().BeOfType<DoubleObj>();
                o.ToDouble().Should().Be( 11.0 + 12 + 1000 * 10 );
            } );
        }

        [Test]
        public void closure_with_two_levels()
        {
            string s = @"
                        function next() 
                        { 
                            let _seed = 0; 
                            function oneMore() {
                                return function() { return ++_seed; };
                            }
                            return oneMore();
                        }
                        let f = next();
                        f() + f() + f();
                        f = next();
                        f() + f() + f() + f();
                        ";
            TestHelper.RunNormalAndStepByStep( s, o =>
            {
                o.Should().BeOfType<DoubleObj>();
                o.ToDouble().Should().Be( 1.0 + 2 + 3 + 4 );
            } );
        }

        [Test]
        public void closure_and_immediately_invoked_function_expression_IIFE()
        {
            string script = @"
                            let i = 10, j = 10; 
                            (function() { 
                              i = j + i; 
                            })();
                            i.ToString();
                            ";
            TestHelper.RunNormalAndStepByStep( script, o =>
            {
                o.Should().BeOfType<StringObj>();
                o.ToString().Should().Be( "20" );
            } );
        }


        [Test]
        public void recursive_function()
        {
            string s = @"function fib(n)
                         {
                            return n <= 2 ? 1 : fib(n-2)+fib(n-1);
                         }
                         fib(20);";
            TestHelper.RunNormalAndStepByStep( s, o =>
            {
                o.Should().BeOfType<DoubleObj>();
                o.ToDouble().Should().Be( 6765.0 );
            } );
        }


        [Test]
        public void returning_a_closed_variable_does_not_return_the_reference()
        {
            string s = @"   function f()
                            {
                                let max = 0;
                                function f(n)
                                {
                                    if( n/2 > max ) max = n/2;
                                    while( n > 0 )
                                    {
                                        if( n <= max ) return max;
                                        --n;
                                    }
                                }
                                return f;
                            }
                            let m = f();
                            m(5) + m(10) + m(20);";
            TestHelper.RunNormalAndStepByStep( s, o =>
            {
                o.Should().BeOfType<DoubleObj>();
                o.ToDouble().Should().Be( 17.5 );
            } );
        }

        [Test]
        public void checking_complex_closure_and_evaluations()
        {
            // Code has been tested on Chrome & Firefox (with firebug).
            string s = @"   function f()
                            {
                               let x = 0;
                               function fSetX(f,n)
                               {
                                 return x = f(n);
                               }
                               return function(n)
                               {
                                 return fSetX( function(n) { return x + n; }, x + n ) + x;
                               };
                            }
                            let wtf = f();
                            wtf(5)+','+wtf(6)+','+wtf(42)+','+wtf(3)+','+wtf(1)+','+wtf(0);";
            TestHelper.RunNormalAndStepByStep( s, o =>
            {
                o.Should().BeOfType<StringObj>();
                o.ToString().Should().Be( "10,32,148,302,606,1212" );
            } );
        }

        [Test]
        public void named_functions_defined_in_parameters()
        {
            string s = @"   let x = 0;
                            function fX( f1, f2, c )
                            {
                                f1(c);
                                f2(c);
                            }
                            // Here, f2 can call f2 since f1 is declared before f1.
                            fX( function f1( c ) { x += c; return c; },
                                function f2( c ) { x *= f1(c); },
                                3
                              );
                            x;
                            ";
            TestHelper.RunNormalAndStepByStep( s, o =>
            {
                o.Should().BeOfType<RefRuntimeObj>();
                o.ToDouble().Should().Be( 18.0 );
            } );
        }

    }
}
