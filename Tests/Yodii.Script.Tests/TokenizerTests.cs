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

namespace Yodii.Script.Tests
{
    [TestFixture]
    public class TokenizerTests
    {
        [Test]
        public void RoundtripParsing()
        {
            JSTokenizer p = new JSTokenizer();
            Assert.That( JSTokenizer.Explain( JSTokenizerToken.Integer ), Is.EqualTo( "42" ) );

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

            Assert.That( recompose, Is.EqualTo( s ) );
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
            Assert.That( p.IsErrorOrEndOfInput, Is.True );
            Assert.That( p.ErrorCode, Is.EqualTo( JSTokenizerError.ErrorNumberIdentifierStartsImmediately ) );
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
            Assert.That( p.IsEndOfInput );
            Assert.That( r, Is.EqualTo( expected ) );
        }

    }
}
