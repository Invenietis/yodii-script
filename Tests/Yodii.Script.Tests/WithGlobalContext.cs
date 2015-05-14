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
using CK.Core;
using NUnit.Framework;

namespace Yodii.Script.Tests
{
    [TestFixture]
    public class WithGlobalContext
    {
        class Context : GlobalContext
        {
            public double [] AnIntrinsicArray = new double[0];

            public override PExpr Visit( IAccessorFrame frame )
            {
                var s = frame.GetState( c => c
                    .On( "AnIntrinsicArray" ).OnIndex( ( f, idx ) =>
                    {
                        if( idx.Type != "number" ) return f.SetError( "Number expected." );
                        int i = JSSupport.ToInt32( idx.ToDouble() );
                        if( i < 0 || i >= AnIntrinsicArray.Length ) return f.SetError( "Index out of range." );
                        return f.SetResult( CreateNumber( AnIntrinsicArray[i] ) );
                    } )
                    .On( "An" ).On( "array" ).On( "with" ).On( "one" ).On( "cell" ).OnIndex( ( f, idx ) =>
                    {
                        return f.SetResult( CreateString( "An.array.with.one.cell[] => " + idx.ToString() ) );
                    } )
                    .On( "array" ).OnIndex( ( f, idx ) =>
                    {
                        throw new CKException( "Accessing XXX.array other than 'An.Array' must not be found." );
                    } )
                    .On( "Ghost" ).On( "M" ).OnCall( ( f, args ) =>
                    {
                        Console.WriteLine( "Ghost.M() called with {0} arguments: {1} (=> returns {0}).", 
                                                args.Count, 
                                                String.Join( ", ", args.Select( a => a.ToString() )) );
                        return f.SetResult( f.Global.CreateNumber( args.Count ) );
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

        [Test]
        public void access_to_a_non_exisitng_object_on_the_Context_is_a_runtime_error()
        {
            string s = "AnIntrinsicArray[0]";
            TestHelper.RunNormalAndStepByStep( s, o =>
            {
                Assert.That( o is RuntimeError );
            } );
        }


        [Test]
        public void access_to_members_via_On_does_not_fallback()
        {
            var ctx = new Context();
            string s = "An.array.with.one.cell[6]";
            TestHelper.RunNormalAndStepByStep( s, o =>
            {
                Assert.That( o is JSEvalString && o.ToString() == "An.array.with.one.cell[] => 6" );
            }, ctx );

            s = "array";
            TestHelper.RunNormalAndStepByStep( s, o =>
            {
                Assert.That( o is RuntimeError );
            }, ctx );

            s = "XX.array";
            TestHelper.RunNormalAndStepByStep( s, o =>
            {
                Assert.That( o is RuntimeError );
            }, ctx );
        }

        [Test]
        public void access_to_AnIntrinsicArray_exposed_by_the_Context()
        {
            string s;
            var ctx = new Context();
            s = "AnIntrinsicArray[0]";
            TestHelper.RunNormalAndStepByStep( s, o =>
            {
                Assert.That( ((RuntimeError)o).Message, Is.EqualTo( "Index out of range." ) );
            }, ctx );

            ctx.AnIntrinsicArray = new[] { 1.2 };
            
            s = "AnIntrinsicArray[-1]";
            TestHelper.RunNormalAndStepByStep( s, o =>
            {
                Assert.That( ((RuntimeError)o).Message, Is.EqualTo( "Index out of range." ) );
            }, ctx );            
            
            s = "AnIntrinsicArray[2]";
            TestHelper.RunNormalAndStepByStep( s, o =>
            {
                Assert.That( ((RuntimeError)o).Message, Is.EqualTo( "Index out of range." ) );
            }, ctx );
            
            s = "AnIntrinsicArray[0]";
            TestHelper.RunNormalAndStepByStep( s, o =>
            {
                Assert.That( o is JSEvalNumber );
                Assert.That( o.ToDouble(), Is.EqualTo( 1.2 ) );
            }, ctx );
            
            ctx.AnIntrinsicArray = new[] { 3.4, 5.6 };

            s = "AnIntrinsicArray[0+(7-7)] + AnIntrinsicArray[1+0]";
            TestHelper.RunNormalAndStepByStep( s, o =>
            {
                Assert.That( o is JSEvalNumber );
                Assert.That( o.ToDouble(), Is.EqualTo( 3.4 + 5.6 ) );
            }, ctx );
        }

        [TestCase( "typeof Ghost.M( 'any', Ghost.M[5+8], 'args' ) == 'number'" )]
        [TestCase( "typeof Ghost.M( Ghost.M[((3+2)*1)+(2*(1+1))*(1+1)], Date(2015, 4, 23) ) == 'number'" )]
        public void access_to_a_ghost_object_step_by_step( string s )
        {
            var ctx = new Context();
            TestHelper.RunNormalAndStepByStep( s, o =>
            {
                Assert.That( o is JSEvalBoolean );
                Assert.That( o.ToBoolean() );
            }, ctx );
        }
    }
}
