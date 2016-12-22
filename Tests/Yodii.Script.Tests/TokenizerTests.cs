#region LGPL License
/*----------------------------------------------------------------------------
* This file (Tests\Yodii.Script.Tests\JSAnalyserTests.cs) is part of Yodii-Script. 
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
using CK.Core;
using System.Globalization;
using FluentAssertions;

namespace Yodii.Script.Tests
{
    [TestFixture]
    public class JSTokenizerTests
    {
        [Test]
        public void round_trip_parsing()
        {
            JSTokenizer p = new JSTokenizer();
            JSTokenizer.Explain( JSTokenizerToken.Integer ).Should().Be( "42" );

            string s = " function ( x , z ) ++ -- { if ( x != z || x && z % x - x >>> z >> z << x | z & x ^ z -- = x ++ ) return x + ( z * 42 ) / 42 ; } void == typeof += new -= delete >>= instanceof >>>= x % z %= x === z !== x ! z ~ = x |= z &= x <<= z ^= x /= z *= x %=";
            p.Reset( s );
            string recompose = "";
            while( !p.IsEndOfInput )
            {
                recompose += " " + JSTokenizer.Explain( p.CurrentToken );
                p.Forward();
            }
            s = s.Replace( "if", "identifier" )
                .Replace( "function", "identifier" )
                .Replace( "x", "identifier" )
                .Replace( "z", "identifier" )
                .Replace( "return", "identifier" );

            recompose.Should().Be( s );
        }

        [TestCase( "45DD" )]
        [TestCase( "45.member" )]
        [TestCase( ".45.member" )]
        [TestCase( "45.01member" )]
        [TestCase( ".45.member" )]
        [TestCase( ".45.01member" )]
        [TestCase( "45.01e23member" )]
        public void bad_literal_numbers_are_ErrorNumberIdentifierStartsImmediately( string num )
        {
            JSTokenizer p = new JSTokenizer( num );
            p.IsErrorOrEndOfInput.Should().Be( true );
            p.ErrorCode.Should().Be( JSTokenizerError.ErrorNumberIdentifierStartsImmediately );
        }


        [TestCase( "45.98" )]
        [TestCase( ".0" )]
        [TestCase( ".0e4" )]
        [TestCase( "876.098E-3" )]
        public void parsing_floats( string num )
        {
            JSTokenizer p = new JSTokenizer( num );
            p.CurrentToken.Should().Be( JSTokenizerToken.Float );
            p.ReadDouble().Should().Be( double.Parse( num, NumberStyles.Float, CultureInfo.InvariantCulture ) );
            p.Forward().Should().Be( false );
            p.IsEndOfInput.Should().Be( true );
        }


        [TestCase( @"""a""", "a" )]
        [TestCase( @"""a""""b""", @"a""b" )]
        [TestCase( @"'a'", "a" )]
        [TestCase( @"'a''b'", @"a'b" )]
        [TestCase( @"'\u3713'", "\u3713" )]
        [TestCase( @"'a\u3712b'", "a\u3712b" )]
        public void successful_string_parsing( string s, string expected )
        {
            JSTokenizer p = new JSTokenizer( s );
            string r = p.ReadString();
            p.IsEndOfInput.Should().Be( true );
            r.Should().Be( expected );
        }

    }
}
