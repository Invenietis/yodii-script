#region LGPL License
/*----------------------------------------------------------------------------
* This file (Tests\Yodii.Script.Tests\StatementTests.cs) is part of Yodii-Script. 
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
using Yodii.Script;

namespace Yodii.Script.Tests
{
    [TestFixture]
    public class StatementTests
    {
        [TestCase( "6;7+3", 10.0 )]
        [TestCase( "1+2*(3+1);", 9.0 )]
        [TestCase( "6;7+3;typeof 6 == 'number' ? 2173 : 3712", 2173.0 )]
        public void evaluating_basic_numbers_expressions( string expr, double result )
        {
            TestHelper.RunNormalAndStepByStep( expr, o =>
            {
                Assert.IsInstanceOf<JSEvalNumber>( o );
                Assert.That( o.ToDouble(), Is.EqualTo( result ) );
            } );
        }

        [Test]
        public void local_variables_definition_and_assignments()
        {
            string s = @"let i;
                         let j;
                         i = 37;
                         j = i*100+12;";
            TestHelper.RunNormalAndStepByStep( s, o =>
            {
                Assert.IsInstanceOf<JSEvalNumber>( o );
                Assert.That( o.ToDouble(), Is.EqualTo( 3712 ) );
            } );
        }

        [Test]
        public void declaring_a_local_variables_do_not_evaluate_to_undefined_like_in_javascript()
        {
            string s = @"let i = 37;
                         let j = i*100+12;";
            TestHelper.RunNormalAndStepByStep( s, o =>
            {
                Assert.IsInstanceOf<JSEvalNumber>( o );
                Assert.AreEqual( o.ToString(), "3712" );
            } );
        }

        [Test]
        public void variables_evaluate_to_RefRuntimeObj_objects()
        {
            string s = @"let i = 37;
                         let j = i*100+12;
                         j;";
            TestHelper.RunNormalAndStepByStep( s, o =>
            {
                Assert.IsInstanceOf<RefRuntimeObj>( o );
                Assert.That( o.ToDouble(), Is.EqualTo( 3712 ) );
            } );
        }


        [Test]
        public void number_assignment_operators_are_supported()
        {
            string s = @"   let i = 0;
                            let bug = '';

                            i += 0+1; i *= 2*1; i <<= 1<<0; i -= 7-6;
                            if( i !== (((0+1)*(2*1))<<(1<<0))-(7-6) ) bug = 'Bug in +, *, << or -';

                            // i = 3
                            i += 4; i &= 2 | 1; 
                            if( i !== (7&2|1) ) bug = 'Bug in &';

                            // i = 3
                            i |= 7+1;
                            if( i !== 11 ) bug = 'Bug in |';

                            // i = 11
                            i >>= 1+1;
                            if( i !== 2 ) bug = 'Bug in >>';

                            // i = 2
                            i ^= 1+8;
                            if( i !== (2^(1+8)) ) bug = 'Bug in ^';

                            // i = 11
                            i ^= -3712;
                            if( i !== (11^-3712) ) bug = 'Bug in ~';

                            // i = -3701
                            i >>>= 2;
                            if( i !== (-3701>>>2) || i !== 1073740898 ) bug = 'Bug in >>>';

                            // i = 1073740898;
                            i &= 2|4|32|512|4096;
                            if( i !== 1073740898 & (2|4|32|512|4096) ) bug = 'Bug in &';

                            // i = 4130
                            i %= -(1+5+3);
                            if( i !== (4130%-(1+5+3)) || i !== 8 ) bug = 'Bug in %';
                            
                            i = 8;
                            i /= 3.52;
                            if( i !== 8/3.52 ) bug = 'Bug in /';
                        
                            bug.toString();
";
            TestHelper.RunNormalAndStepByStep( s, o =>
            {
                Assert.IsInstanceOf<JSEvalString>( o );
                Assert.That( o.ToString(), Is.EqualTo( String.Empty ) );
            } );
        }

        [Test]
        public void simple_if_block()
        {
            string s = @"let i = 37;
                         let j;
                         if( i == 37 ) 
                         {
                            j = 3712;
                            i += j;
                         }
                         // i = 0: the 0 value is the result;
                         if( j > 3000 ) i = 0;";
            TestHelper.RunNormalAndStepByStep( s, o =>
            {
                Assert.IsInstanceOf<JSEvalNumber>( o );
                Assert.That( o.ToDouble(), Is.EqualTo( 0 ) );
            } );
        }

        [Test]
        public void comparing_to_undefined_keyword_works()
        {
            string s = @"let ResultAsRefRuntimeObject = 8;
                         let X;
                         if( X === undefined ) ResultAsRefRuntimeObject;";
            TestHelper.RunNormalAndStepByStep( s, o =>
            {
                Assert.IsInstanceOf<RefRuntimeObj>( o );
                Assert.That( o.ToDouble(), Is.EqualTo( 8 ) );
            } );
        }

        [Test]
        public void post_incrementation_works()
        {
            string s = @"let i = 0;
                         if( i++ == 0 && i++ == 1 && i++ == 2 ) i;";
            TestHelper.RunNormalAndStepByStep( s, o =>
            {
                Assert.IsInstanceOf<RefRuntimeObj>( o );
                Assert.That( o.ToDouble(), Is.EqualTo( 3 ) );
            } );
        }

        [Test]
        public void pre_incrementation_works()
        {
            string s = @"let i = 0;
                         if( ++i == 1 && ++i == 2 && ++i == 3 && ++i == 4 ) i;";
            TestHelper.RunNormalAndStepByStep( s, o =>
            {
                Assert.IsInstanceOf<RefRuntimeObj>( o );
                Assert.That( o.ToDouble(), Is.EqualTo( 4 ) );
            } );
        }


        [Test]
        public void while_loop_works()
        {
            string s = @"let i = 0;
                         while( i < 10 ) i++;
                         i;";
            TestHelper.RunNormalAndStepByStep( s, o =>
            {
                Assert.IsInstanceOf<RefRuntimeObj>( o );
                Assert.That( o.ToDouble(), Is.EqualTo( 10 ) );
            } );
        }
        [Test]
        public void while_loop_with_empty_block_works()
        {
            string s = @"let i = 0;
                         while( i++ < 10 );
                         i;";
            TestHelper.RunNormalAndStepByStep( s, o =>
            {
                Assert.IsInstanceOf<RefRuntimeObj>( o );
                Assert.That( o.ToDouble(), Is.EqualTo( 11 ) );
            } );
        }

        [Test]
        public void while_loop_with_block_works()
        {
            string s = @"let i = 0;
                         let j = 0;
                         while( i < 10 ) { 
                            i++;
                            if( i%2 == 0 ) j += 10;
                         }
                         j;";
            TestHelper.RunNormalAndStepByStep( s, o =>
            {
                Assert.IsInstanceOf<RefRuntimeObj>( o );
                Assert.That( o.ToDouble(), Is.EqualTo( 50 ) );
            } );
        }

        [Test]
        public void do_while_loop_with_block_works()
        {
            string s = @"let i = 0;
                         let j = 0;
                         do
                         { 
                            i++;
                            if( i%2 == 0 ) j += 10;
                         }
                         while( i < 10 );
                         j;";
            TestHelper.RunNormalAndStepByStep( s, o =>
            {
                Assert.IsInstanceOf<RefRuntimeObj>( o );
                Assert.That( o.ToDouble(), Is.EqualTo( 50 ) );
            } );
        }

        [Test]
        public void do_while_loop_expects_a_block()
        {
            string s = @"let i = 0;
                         do i++; while( i < 10 );
                         i;";
            TestHelper.RunNormalAndStepByStep( s, o =>
            {
                Assert.IsInstanceOf<RuntimeError>( o );
            } );
        }

        [Test]
        public void while_loop_support_break_statement()
        {
            string s = @"
                        let i = 0, j = '';
                        while( true )
                        {
                            if( i++ >= 5 ) break;
                            j += 'a';
                        }
                        j;";
            TestHelper.RunNormalAndStepByStep( s, o =>
            {
                Assert.IsInstanceOf<RefRuntimeObj>( o );
                Assert.That( o.ToString(), Is.EqualTo( "aaaaa" ) );
            } );
        }

        [Test]
        public void while_loop_support_continue_statement()
        {
            string s = @"
                        let i = 0, j = '';
                        while( ++i < 10 )
                        {
                            if( i%2 == 0 ) continue;
                            j += 'a';
                        }
                        j;";
            TestHelper.RunNormalAndStepByStep( s, o =>
            {
                Assert.IsInstanceOf<RefRuntimeObj>( o );
                Assert.That( o.ToString(), Is.EqualTo( "aaaaa" ) );
            } );
        }

        [Test]
        public void do_while_loop_support_break_statement()
        {
            string s = @"
                        let i = 0, j = '';
                        do
                        {
                            if( i++ >= 4 ) break;
                            j += 'a';
                        }
                        while( i < 1000 );
                        j;";
            TestHelper.RunNormalAndStepByStep( s, o =>
            {
                Assert.IsInstanceOf<RefRuntimeObj>( o );
                Assert.That( o.ToString(), Is.EqualTo( "aaaa" ) );
            } );
        }

        [Test]
        public void multiple_variables_declaration_is_supported_and_they_can_reference_previous_ones()
        {
            string s = @"let i = 1, j = i*200+34, k = 'a string';
                         k+i+j;";
            TestHelper.RunNormalAndStepByStep( s, o =>
            {
                Assert.IsInstanceOf<JSEvalString>( o );
                Assert.That( o.ToString(), Is.EqualTo( "a string1234" ) );
            } );
        }

        [Test]
        public void lexical_scope_is_enough_with_curly()
        {
            string s = @"
                        let i = 0, j = 'a';
                        {
                            let i = 't'; 
                        }
                        i+j;";
            TestHelper.RunNormalAndStepByStep( s, o =>
            {
                Assert.IsInstanceOf<JSEvalString>( o );
                Assert.That( o.ToString(), Is.EqualTo( "0a" ) );
            } );
        }

        [TestCase( "a+++b", "r=0, a=1, b=0" )]
        [TestCase( "a+++b+++a++", "r=1, a=2, b=1" )]
        [TestCase( "a+++b+++a+b+++a", "r=3, a=1, b=2" )]
        public void ambiguous_postfix_increment_and_addition_works_like_in_javascript( string add, string result )
        {
            string s = String.Format( @"let a = 0, b = 0, r = {0};
                                        'r='+r+', a='+a+', b='+b
                                        ", add );
            TestHelper.RunNormalAndStepByStep( s, o =>
            {
                Assert.IsInstanceOf<JSEvalString>( o );
                Assert.That( o.ToString(), Is.EqualTo( result ) );
            } );
        }


    }
}
