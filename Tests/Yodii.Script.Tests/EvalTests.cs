#region LGPL License
/*----------------------------------------------------------------------------
* This file (Tests\Yodii.Script.Tests\EvalTests.cs) is part of Yodii-Script. 
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
using Xunit;
using FluentAssertions;

namespace Yodii.Script.Tests
{
    
    public class EvalTests
    {
        [Theory]
        [InlineData( "6", 6 )]
        [InlineData( "(6+6)*3/4*2", (6.0 + 6.0) * 3.0 / 4.0 * 2.0 )]
        [InlineData( "8*5/4+1-(100/5/4)", 8.0 * 5.0 / 4.0 + 1.0 - (100.0 / 5.0 / 4.0) )]
        [InlineData( "8*5/4+1-(100/5/4) > 1 ? 14+56/7/2-4 : (14+13+12)/2*47/3", 14.0 + 56.0 / 7.0 / 2.0 - 4.0 )]
        public void evaluating_basic_numbers_expressions( string expr, double result )
        {
            TestHelper.RunNormalAndStepByStep( expr, o =>
            {
                o.Should().BeOfType<DoubleObj>();
                o.ToDouble().Should().Be( result );
            } );
        }

        [Theory]
        [InlineData( "+2", 2.0 )]
        [InlineData( "-2", -2.0 )]
        [InlineData( "+'8'", 8.0 )]
        [InlineData( "+'8' ? -'2' : -'3'", -2.0 )]
        [InlineData( "+'8'+'7'", "87" )]
        [InlineData( "+'8'+ +'7'", 15.0 )]
        public void operator_plus_or_minus_convert_to_number_just_like_in_javascript( string expr, object result )
        {
            TestHelper.RunNormalAndStepByStep( expr, o =>
            {
                if( result is double )
                {
                    o.Should().BeOfType<DoubleObj>();
                    o.ToDouble().Should().Be( (double)result );
                }
                else
                {
                    o.Should().BeOfType<StringObj>();
                    o.ToString().Should().Be( (string)result );
                }
            } );
        }

        [Fact]
        public void strings_are_automatically_converted_to_numbers_by_arithmetic_operators()
        {
            RuntimeObj o;
            {
                o = ScriptEngine.Evaluate( "7 + '45' / 2 * '10' / '4'" );
                o.Should().BeOfType<DoubleObj>();
                o.ToDouble().Should().Be( 7.0 + 45.0 / 2.0 * 10.0 / 4.0 );
            }
        }

        [Theory]
        [InlineData( "'45' > 454", false )]
        [InlineData( "'45' >= 45", true )]
        [InlineData( "'45' + 4 == '454'", true )]
        [InlineData( "'45' <= '454'", true )]
        [InlineData( "45 <= '454'", true )]
        [InlineData( "'olivier' < 'spi'", true )]
        public void comparing_strings_and_numbers( string expr, bool result )
        {
            TestHelper.RunNormalAndStepByStep( expr, o =>
            {
                o.Should().BeOfType<BooleanObj>();
                o.ToBoolean().Should().Be( result );
            } );
        }

        [Fact]
        public void biwise_operations_on_numbers()
        {
            IsNumber( "7&3 == 3", 1, "=> 7&(3 == 3)" );
            IsBoolean( "(7&3) == 3" );
            IsBoolean( "((7&3)&1)+2 == (1&45)+2*1" );
            IsBoolean( "(1|2|8) == 1+2+8" );
            IsBoolean( "(1|2.56e2) == 257" );
            IsNumber( "~7", -8 );
            IsNumber( "~(7|1)", -8 );
            IsNumber( "~7|1", -7 );
        }

        [Theory]
        [InlineData( "3701 >> 2", 925 )]
        [InlineData( "3701.9 >> 2", 925 )]
        [InlineData( "3701.1 >> 2", 925 )]

        [InlineData( "3701 >> 0",  3701 )]
        [InlineData( "3701 >> -1",  0 )]
        [InlineData( "3701 >> -0.2", 3701 )]
        [InlineData( "3701 >> -0.7",  3701 )]

        [InlineData( "3701 >> -2", 0 )]
        [InlineData( "3701 >> -2.1", 0 )]
        [InlineData( "3701 >> -2.8", 0 )]
        [InlineData( "3701 >> 63", 0 )]
        [InlineData( "3701 >> 64", 3701 )]
        [InlineData( "3701.47 >> 64", 3701.47 )]
        [InlineData( "3701 >> 63.5", 0 )]
        [InlineData( "3701 >> 64.2", 3701 )]
        [InlineData( "3701 >> 64.8", 3701 )]
        [InlineData( "3701 >> 'NaN' ",  3701 )]

        [InlineData( "-3701 >> 1", -1851 )]
        [InlineData( "-3701 >> 65", -1851 )]
        [InlineData( "-3701 >> 2", -926 )]
        [InlineData( "-3701 >> 66", -926 )]
        [InlineData( "-3701.9 >> 2", -926 )]
        [InlineData( "-3701.1 >> 2", -926 )]
        public void biwise_shift_right( string s, double v )
        {
            Console.WriteLine( "{0} == {1}", s, v );
            IsNumber( s, v );
        }

        [Theory]
        [InlineData( "3701 << 2", 14804 )]
        [InlineData( "3701 << 2.1", 14804 )]
        [InlineData( "3701 << 2.8", 14804 )]
        [InlineData( "3701 << 0", 3701 )]
        [InlineData( "3701 << -1", -2147483648 )]
        [InlineData( "3701 << -0.7", 3701 )]
        [InlineData( "3701 << -0.2", 3701 )]
        [InlineData( "3701 << -2 ", 1073741824 )]
        [InlineData( "3701 << 63 ", -2147483648 )]
        [InlineData( "3701 << 64 ", 3701 )]
        [InlineData( "3701 << 'NaN'", 3701 )]
        [InlineData( "-3701 << 2", -14804 )]
        [InlineData( "-3701 << -2", -1073741824 )]
        [InlineData( "-3701 << -3", 1610612736 )]
        [InlineData( "-3701 << -4", -1342177280 )]
        [InlineData( "-3701 << -5", 1476395008 )]
        [InlineData( "-3701 << -6", 738197504 )]
        [InlineData( "-3701 << -7", 369098752 )]
        [InlineData( "-3701 << -8", -1962934272 )]
        [InlineData( "-3701 << -9", -981467136 )]
        [InlineData( "-3701 << -10", 1656750080 )]
        public void biwise_shift_left( string s, double v )
        {
            Console.WriteLine( "{0} == {1}", s, v );
            IsNumber( s, v );
        }

        [Theory]
        [InlineData( "3701 >>> 2 ", 925 )]
        [InlineData( "3701 >>> 0 ", 3701 )]
        [InlineData( "3701 >>> -0.2 ", 3701 )]
        [InlineData( "3701 >>> -0.8 ", 3701 )]
        [InlineData( "3701 >>> -1 ", 0 )]
        [InlineData( "3701 >>> -1.2 ", 0 )]
        [InlineData( "3701 >>> -2 ", 0 )]
        [InlineData( "3701 >>> -64 ", 3701 )]
        [InlineData( "3701 >>> -65 ", 0 )]
        [InlineData( "3701 >>> 'NaN' ", 3701 )]
        [InlineData( "-3701 >>> 2 ", 1073740898 )]
        [InlineData( "-3701 >>> 10 ", 4194300 )]
        [InlineData( "-3701 >>> 63 ", 1 )]
        [InlineData( "-3701 >>> 64 ", 4294963595 )]
        [InlineData( "-3701 >>> 65 ", 2147481797 )]
        [InlineData( "-3701 >>> 66 ", 1073740898 )]
        public void biwise_unsigned_shift_right( string s, double v )
        {
            Console.WriteLine( "{0} == {1}", s, v );
            IsNumber( s, v );
        }

        [Fact]
        public void ternary_operator()
        {
            IsBoolean( "3 ? true : false", true );
            IsBoolean( "0 ? true : false", false );
            IsNumber( "'' ? 1+1 : 3+3", 6 );
            IsNumber( "' ' ? 1+1 : false", 2 );
            IsNumber( "'false' ? ~45*8 : 's'", ~45 * 8.0, "The string 'false' is true." );
        }

        [Fact]
        public void modulo_returns_a_value_with_the_sign_of_the_first_number()
        {
            IsBoolean( "45 % 10 == 5", true );
            IsBoolean( "45 % -10 == 5", true );
            IsBoolean( "-45 % 10 == -5", true );
            IsBoolean( "-45 % -10 == -5", true );
        }

        [Fact]
        public void multiple_inequalities()
        {
            {
                IsBoolean( "45 > 45", false );
                IsBoolean( "45 >= 45", true );
                IsBoolean( "46 > 45", true );
                IsBoolean( "45 > '45'", false );
                IsBoolean( "45 >= '45'", true );
                IsBoolean( "46 > '45'", true );
                IsBoolean( "'45' > 45", false );
                IsBoolean( "'45' >= 45", true );
                IsBoolean( "'45'+3 > 452", true );
                IsBoolean( "'45'+2 > 452", false );
                IsBoolean( "'45'+2 >= 452", true );
                IsBoolean( "'45DD' > 45", false );
                IsBoolean( "'45DD' >= 45", false );
                IsBoolean( "Infinity > 45", true );
                IsBoolean( "Infinity >= 45", true );
                IsBoolean( "Infinity > Infinity", false );
                IsBoolean( "Infinity >= Infinity", true );

                IsBoolean( "Infinity >= NaN", false );
                IsBoolean( "Infinity > NaN", false );
                IsBoolean( "0 >= NaN", false );
                IsBoolean( "0 > NaN", false );

                IsBoolean( "'z' > 'z'", false );
                IsBoolean( "'z' >= 'z'", true );
                IsBoolean( "'z' > 'a'", true );
                IsBoolean( "'z' > 'a'", true );
            }
            {
                IsBoolean( "45 < 45", false );
                IsBoolean( "45 <= 45", true );
                IsBoolean( "44 < 45", true );
                IsBoolean( "45 < '45'", false );
                IsBoolean( "45 <= '45'", true );
                IsBoolean( "'44' < 45", true );
                IsBoolean( "'45' < 45", false );
                IsBoolean( "'45' <= 45", true );
                IsBoolean( "'45'+1 < 452", true );
                IsBoolean( "'45'+2 < 452", false );
                IsBoolean( "'45'+2 <= 452", true );
                IsBoolean( "'45DD' < 45", false );
                IsBoolean( "'45DD' <= 45", false );
                IsBoolean( "-Infinity < 45", true );
                IsBoolean( "-Infinity <= 45", true );
                IsBoolean( "-Infinity < -Infinity", false );
                IsBoolean( "-Infinity <= -Infinity", true );

                IsBoolean( "-Infinity <= NaN", false );
                IsBoolean( "-Infinity < NaN", false );
                IsBoolean( "0 <= NaN", false );
                IsBoolean( "0 < NaN", false );

                IsBoolean( "'z' < 'z'", false );
                IsBoolean( "'z' <= 'z'", true );
                IsBoolean( "'a' < 'z'", true );
                IsBoolean( "'a' < 'z'", true );
            }

        }

        [Fact]
        public void multiple_equalities()
        {
            IsBoolean( "45 == 45", true );
            IsBoolean( "45 == '45'", false );
            IsBoolean( "'45' == 45", false );
            IsBoolean( "'45'+2 == 452", false );
            IsBoolean( "'45DD' != 45", true );

            IsBoolean( "45 != 45", false );
            IsBoolean( "45 != '45'", true );
            IsBoolean( "'45' != 45", true );
            IsBoolean( "'45'+2 != 452", true );

            IsBoolean( "Infinity == Infinity", true );
            IsBoolean( "Infinity == 45/0", true );
            IsBoolean( "-Infinity == -45/0", true );
            IsBoolean( "NaN == NaN", false );
            IsBoolean( "NaN != NaN", true );
            IsBoolean( "Infinity != NaN", true );
        }

        [Theory]
        [InlineData( "(400+50+3).ToString() == '453'", true )]
        [InlineData( "(-98979).ToString(2) == '-11000001010100011'", true )]
        [InlineData( "(14714).ToString(3) == '202011222'", true )]
        [InlineData( "(-1.47e12).ToString(9) == '-5175284306313'", true )]
        [InlineData( "(1.4756896725e12).ToString(30) == '27e7t31k0'", true )]
        [InlineData( "(1.4756896725e12).ToString(31) == '1mjn02pj9'", true )]
        [InlineData( "(1.4756896725e12).ToString(32) == '1auavarpk'", true )]
        [InlineData( "(1.4756896725e12).ToString(33) == '11kl9kf8l'", true )]
        [InlineData( "(1.4756896725e12).ToString(34) == 's38se3kg'", true )]
        [InlineData( "(1.4756896725e12).ToString(35) == 'mwqnd0lf'", true )]
        [InlineData( "(1.4756896725e12).ToString(36) == 'itx7j2no'", true )]
        public void number_toString_method_supports_base_from_2_to_36( string s, bool v)
        {
            IsBoolean( s, v );
        }

        static void IsBoolean( string s, bool v = true, string msg = null )
        {
            RuntimeObj o = ScriptEngine.Evaluate( s );
            o.Should().BeOfType<BooleanObj>();
            o.ToBoolean().Should().Be( v, msg ?? s );
        }

        static void IsNumber( string s, double v, string msg = null )
        {
            RuntimeObj o = ScriptEngine.Evaluate( s );
            o.Should().BeOfType<DoubleObj>();
            o.ToDouble().Should().Be( v, msg ?? s );
        }

        static void IsString( string s, string v, string msg = null )
        {
            RuntimeObj o = ScriptEngine.Evaluate( s );
            o.Should().BeOfType<StringObj>();
            o.ToString().Should().Be( v, msg ?? s );
        }

    }
}
