#region LGPL License
/*----------------------------------------------------------------------------
* This file (Tests\Yodii.Script.Tests\WithGlobalContext.cs) is part of Yodii-Script. 
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
using System.Threading.Tasks;
using Xunit;
using FluentAssertions;

namespace Yodii.Script.Tests
{
    
    public class GlobalContextTests
    {
        [Fact]
        public void successful_namespace_registration()
        {
            GlobalContext c = new GlobalContext();
            c.Register( "Numbers.One", 1 );
            ScriptEngine.Evaluate( "Numbers.One", c ).ToString().Should().Be( "1" );

            Action a = () => c.Register( "Numbers.One", 1 );
            a.ShouldThrow<ArgumentException>();

            c.Register( "Numbers.Two", 2 );
            ScriptEngine.Evaluate( "Numbers.One + Numbers.Two", c ).ToString().Should().Be( "3" );
        }

        [Fact]
        public void namespace_can_not_be_registered_on_or_below_a_registered_object()
        {
            GlobalContext c = new GlobalContext();
            c.Register( "Numbers", 1 );

            Action a = () => c.Register( "Numbers", 2 );
            a.ShouldThrow<ArgumentException>();

            a = () => c.Register( "Numbers.One", 3 );
            a.ShouldThrow<ArgumentException>();

            c.Register( "X.Numbers", 1 );
            a = () => c.Register( "X.Numbers", 2 );
            a.ShouldThrow<ArgumentException>();

            a = () => c.Register( "X.Numbers.One", 3 );
            a.ShouldThrow<ArgumentException>();

            ScriptEngine.Evaluate( "X.Numbers", c ).ToString().Should().Be( "1" );
        }

        [Fact]
        public void namespace_and_function_simple_demo()
        {
            var c = new GlobalContext();
            Func<string, string> func = s => "N'" + s.Replace( "'", "''" ) + "'";
            c.Register( "SqlHelper.ToSqlNString", func );
            ScriptEngine.Evaluate( @" 'hop = ' + SqlHelper.ToSqlNString( ""Aujourd'hui"" )", c )
                .ToString().Should().Be( "hop = N'Aujourd''hui'" );
        }

        [Fact]
        public void object_can_not_be_regitered_on_or_below_a_registered_namespace()
        {
            GlobalContext c = new GlobalContext();
            c.Register( "NS.Sub.NS.Obj", 1 );
            Action a = () => c.Register( "NS", 2 );
            a.ShouldThrow<ArgumentException>();
            a = () => c.Register( "NS.Sub", 3 );
            a.ShouldThrow<ArgumentException>();
            a = () => c.Register( "NS.Sub.NS", 4 );
            a.ShouldThrow<ArgumentException>();
            a = () => c.Register( "NS.Sub.NS.Obj", 5 );
            a.ShouldThrow<ArgumentException>();
            ScriptEngine.Evaluate( "NS.Sub.NS.Obj", c ).ToString().Should().Be( "1" );
        }

        class Context : GlobalContext
        {
            public double [] AnIntrinsicArray = new double[0];

            public override PExpr Visit( IAccessorFrame frame )
            {
                var s = frame.GetImplementationState( c => c
                    .On( "AnIntrinsicArray" ).OnIndex( ( f, idx ) =>
                    {
                        if( idx.Type != "number" ) return f.SetError( "Number expected." );
                        int i = JSSupport.ToInt32( idx.ToDouble() );
                        if( i < 0 || i >= AnIntrinsicArray.Length ) return f.SetError( "Index out of range." );
                        return f.SetResult( DoubleObj.Create( AnIntrinsicArray[i] ) );
                    } )
                    .On( "An" ).On( "array" ).On( "with" ).On( "one" ).On( "cell" ).OnIndex( ( f, idx ) =>
                    {
                        return f.SetResult( StringObj.Create( "An.array.with.one.cell[] => " + idx.ToString() ) );
                    } )
                    .On( "array" ).OnIndex( ( f, idx ) =>
                    {
                        throw new Exception( "Accessing XXX.array other than 'An.Array' must not be found." );
                    } )
                    .On( "Ghost" ).On( "M" ).OnCall( ( f, args ) =>
                    {
                        Console.WriteLine( "Ghost.M() called with {0} arguments: {1} (=> returns {0}).", 
                                                args.Count, 
                                                string.Join( ", ", args.Select( a => a.ToString() )) );
                        return f.SetResult( DoubleObj.Create( args.Count ) );
                    } )
                    .On( "Ghost" ).On( "M" ).OnIndex( ( f, idx ) =>
                    {
                        Console.WriteLine( "Ghost.M[{0}] called (=> returns {0}).", JSSupport.ToInt32( idx.ToDouble() ) );
                        return f.SetResult( idx );
                    } )
                    );
                return s == null ? base.Visit( frame ) : s.Visit();
            }
        }

        [Fact]
        public void access_to_a_non_existing_object_on_the_Context_is_a_runtime_error()
        {
            string s = "AnIntrinsicArray[0]";
            TestHelper.RunNormalAndStepByStep( s, o =>
            {
                o.Should().BeOfType<RuntimeError>();
            } );
        }


        [Fact]
        public void access_to_members_via_On_does_not_fallback()
        {
            var ctx = new Context();
            string s = "An.array.with.one.cell[6]";
            TestHelper.RunNormalAndStepByStep( s, o =>
            {
                o.Should().BeOfType<StringObj>();
                o.ToString().Should().Be( "An.array.with.one.cell[] => 6" );
            }, ctx );

            s = "array";
            TestHelper.RunNormalAndStepByStep( s, o =>
            {
                o.Should().BeOfType<RuntimeError>();
            }, ctx );

            s = "XX.array";
            TestHelper.RunNormalAndStepByStep( s, o =>
            {
                o.Should().BeOfType<RuntimeError>();
            }, ctx );
        }

        [Fact]
        public void access_to_AnIntrinsicArray_exposed_by_the_Context()
        {
            string s;
            var ctx = new Context();
            s = "AnIntrinsicArray[0]";
            TestHelper.RunNormalAndStepByStep( s, o =>
            {
                ((RuntimeError)o).Message.Should().Be( "Index out of range." );
            }, ctx );

            ctx.AnIntrinsicArray = new[] { 1.2 };
            
            s = "AnIntrinsicArray[-1]";
            TestHelper.RunNormalAndStepByStep( s, o =>
            {
                ((RuntimeError)o).Message.Should().Be( "Index out of range." );
            }, ctx );            
            
            s = "AnIntrinsicArray[2]";
            TestHelper.RunNormalAndStepByStep( s, o =>
            {
                ((RuntimeError)o).Message.Should().Be( "Index out of range." );
            }, ctx );
            
            s = "AnIntrinsicArray[0]";
            TestHelper.RunNormalAndStepByStep( s, o =>
            {
                o.Should().BeOfType<DoubleObj>();
                o.ToDouble().Should().Be( 1.2 );
            }, ctx );
            
            ctx.AnIntrinsicArray = new[] { 3.4, 5.6 };

            s = "AnIntrinsicArray[0+(7-7)] + AnIntrinsicArray[1+0]";
            TestHelper.RunNormalAndStepByStep( s, o =>
            {
                o.Should().BeOfType<DoubleObj>();
                o.ToDouble().Should().Be( 3.4 + 5.6 );
            }, ctx );
        }

        [Theory]
        [InlineData( "typeof Ghost.M( 'any', Ghost.M[5+8], 'args' ) == 'number'" )]
        [InlineData( "typeof Ghost.M( Ghost.M[((3+2)*1)+(2*(1+1))*(1+1)], 'a string' ) == 'number'" )]
        public void access_to_a_ghost_object_step_by_step( string s )
        {
            var ctx = new Context();
            TestHelper.RunNormalAndStepByStep( s, o =>
            {
                o.Should().BeOfType<BooleanObj>();
                o.ToBoolean().Should().BeTrue();
            }, ctx );
        }
    }
}
