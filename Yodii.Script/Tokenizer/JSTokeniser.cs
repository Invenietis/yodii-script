#region LGPL License
/*----------------------------------------------------------------------------
* This file (Yodii.Script\Tokenizer\JSTokeniser.cs) is part of Yodii-Script. 
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
using System.Diagnostics;
using System.Text;
using System.IO;

using System.Globalization;

namespace Yodii.Script
{

    /// <summary>
    ///	Small tokenizer to handle javascript based language (ECMAScript).
    /// </summary>
    public class JSTokenizer
    {
        #region Private fields

        TextReader		_inner;
        int             _prevCharPosTokenEnd;
        int             _charPosTokenBeg;
        int             _charPos;
        int				_nextC;
        SourceLocation  _prevNonCommentLocation;
        SourceLocation  _location;
        bool			_lineInc;

        bool			_skipComments;

        int             _integerValue;
        string          _identifierValue;

        StringBuilder	_buffer;
        int				_token;
        int             _prevNonCommentToken;

        #endregion

        public JSTokenizer()
        {
            _skipComments = true;
            _buffer = new StringBuilder( 512 );
        }

        public JSTokenizer( string input, string source = SourceLocation.NoSource, int startLineNumber = 0, int startColumnNumber = 0 )
        {
            _skipComments = true;
            _buffer = new StringBuilder( 512 );
            Reset( input, source, startLineNumber, startColumnNumber );
        }

        public bool Reset( string input, string source = SourceLocation.NoSource, int startLineNumber = 0, int startColumnNumber = 0 )
        {
            return Reset( new StringReader( input ), source, startLineNumber, startColumnNumber );
        }

        public bool Reset( TextReader input, string source, int startLineNumber, int startColumnNumber )
        {
            _inner = input;
            _location.Source = source ?? SourceLocation.NoSource;
            _location.Line = startLineNumber;
            _location.Column = startColumnNumber;

            _charPosTokenBeg = 0;
            _prevCharPosTokenEnd = 0;
            _charPos = 0;
            _nextC = 0;
            _token = 0;
            NextToken2();
            return _token >= 0;
        }

        /// <summary>
        /// Defaults to true.
        /// </summary>
        public bool SkipComments
        {
            get { return _skipComments; }
            set { _skipComments = value; }
        }

        /// <summary>
        /// Gets the current precedence level from <see cref="CurrentToken"/> with a provision of 1 bit
        /// to ease the handling of right associative infix operators (this level is even).
        /// </summary>
        /// <remarks>
        /// This uses <see cref="JSTokenizerToken.OpLevelMask"/> and <see cref="JSTokenizerToken.OpLevelShift"/>.
        /// </remarks>
        public int CurrentPrecedenceLevel
        {
            get { return PrecedenceLevel( CurrentToken ); }
        }

        /// <summary>
        /// Computes the precedence with a provision of 1 bit to ease the handling of right associative infix operators.
        /// </summary>
        /// <returns>An even precedence level between 30 and 2. 0 if the token has <see cref="JSTokenizerError.IsErrorOrEndOfInput"/> bit set.</returns>
        /// <remarks>
        /// This uses <see cref="JSTokenizerToken.OpLevelMask"/> and <see cref="JSTokenizerToken.OpLevelShift"/>.
        /// </remarks>
        public static int PrecedenceLevel( JSTokenizerToken t )
        {
            return t > 0 ? ( ((int)(t & JSTokenizerToken.OpLevelMask)) >> (int)JSTokenizerToken.OpLevelShift ) << 1 : 0;
        }

        /// <summary>
        /// Gets the current <see cref="JSTokenizerToken"/> code.
        /// </summary>
        public JSTokenizerToken CurrentToken
        {
            get { return (JSTokenizerToken)_token; }
        }

        /// <summary>
        /// Gets the <see cref="JSTokenizerError"/> code if the parser is in error
        /// (or the end of the input is reached). <see cref="JSTokenizerError.None"/> if
        /// no error occured.
        /// </summary>
        public JSTokenizerError ErrorCode
        {
            get { return _token < 0 ? (JSTokenizerError)_token : JSTokenizerError.None; }
        }

        #region IsErrorOrEndOfInput, IsEndOfInput, IsAssignOperator, ..., IsUnaryOperator
        /// <summary>
        /// True if an error or the end of the stream is reached.
        /// </summary>
        /// <returns></returns>
        public bool IsErrorOrEndOfInput
        {
            get { return _token < 0; }
        }

        /// <summary>
        /// True if <see cref="ErrorCode"/> is <see cref="JSTokenizerError.EndOfInput"/>.
        /// </summary>
        /// <returns></returns>
        public bool IsEndOfInput
        {
            get { return _token == (int)JSTokenizerError.EndOfInput; }
        }

        public bool IsAssignOperator
        {
            get { return (_token&(int)JSTokenizerToken.IsAssignOperator) != 0; }
        }

        public bool IsBinaryOperator
        {
            get { return (_token & (int)JSTokenizerToken.IsBinaryOperator) != 0; }
        }

        public bool IsBracket
        {
            get { return (_token & (int)JSTokenizerToken.IsBracket) != 0; }
        }

        public bool IsCompareOperator
        {
            get { return (_token & (int)JSTokenizerToken.IsCompareOperator) != 0; }
        }

        public bool IsComment
        {
            get { return (_token & (int)JSTokenizerToken.IsComment) != 0; }
        }

        /// <summary>
        /// True if the current token is an identifier. <see cref="ReadIdentifier"/> can be called to get 
        /// the actual value.
        /// </summary>
        public bool IsIdentifier
        {
            get { return (_token & (int)JSTokenizerToken.IsIdentifier) != 0; }
        }

        public bool IsLogical
        {
            get { return (_token & (int)JSTokenizerToken.IsLogical) != 0; }
        }

        #region IsNumber, IsNumberFloat and IsNumberInteger
        /// <summary>
        /// True if the current token is a number. <see cref="ReadNumber"/> can be called to get 
        /// the actual value.
        /// </summary>
        public bool IsNumber
        {
            get { return (_token & (int)JSTokenizerToken.IsNumber) != 0; }
        }

        /// <summary>
        /// True if the current token is a float number (i.e. with a fractional and/or 
        /// exponent part). <see cref="ReadNumber"/> can be called to get the value.
        /// </summary>
        public bool IsNumberFloat
        {
            get { return _token == (int)JSTokenizerToken.Float; }
        }

        /// <summary>
        /// True if the current token is an integer number (i.e. without any fractional nor 
        /// exponent part). <see cref="ReadNumber"/> can be called to get the value.
        /// </summary>
        public bool IsNumberInteger
        {
            get { return (_token & (int)JSTokenizerToken.Integer) == (int)JSTokenizerToken.Integer; }
        }
        #endregion

        public bool IsPunctuation
        {
            get { return (_token & (int)JSTokenizerToken.IsPunctuation) != 0; }
        }

        public bool IsRegex
        {
            get { return (_token & (int)JSTokenizerToken.IsRegex) != 0; }
        }

        /// <summary>
        /// True if the current token is a string. <see cref="ReadString"/> can be called to get 
        /// the actual value.
        /// </summary>
        public bool IsString
        {
            get { return (_token & (int)JSTokenizerToken.IsString) != 0; }
        }

        /// <summary>
        /// True when <see cref="CurrentToken"/> is "++", "--", "-", "~", "!", "delete", "new", "typeof" or "void".
        /// </summary>
        public bool IsUnaryOperator
        {
            get { return (_token & (int)JSTokenizerToken.IsUnaryOperator) != 0; }
        }
        
        /// <summary>
        /// Gets whether the current token is one of the <see cref="IsUnaryOperator"/> or <see cref="JSTokenizerToken.Plus"/> or <see cref="JSTokenizerToken.Minus"/>.
        /// </summary>
        public bool IsUnaryOperatorOrPlusOrMinus
        {
            get { return IsUnaryOperator || _token == (int)JSTokenizerToken.Plus || _token == (int)JSTokenizerToken.Minus; }
        }
        
        #endregion

        /// <summary>
        /// Forwards the head to the next token.
        /// </summary>
        /// <returns>True if a token is available. False if the end of the stream is encountered
        /// or an error occured.</returns>
        public bool Forward()
        {
            return NextToken2() >= 0;
        }

        /// <summary>
        /// Gets the character index in the input stream of the current token.
        /// </summary>
        public int CharPosTokenBeg
        {
            get { return _charPosTokenBeg; }
        }

        /// <summary>
        /// Gets the current character index in the input stream: it corresponds to the
        /// end of the current token.
        /// </summary>
        public int CharPosTokenEnd
        {
            get { return _charPos; }
        }

        /// <summary>
        /// Gets the current source location. A <see cref="SourceLocation"/> is a value type.
        /// </summary>
        public SourceLocation Location
        {
            get { return _location; }
        }

        /// <summary>
        /// Gets the previous token (ignoring any comments that may have occured).
        /// </summary>
        public JSTokenizerToken PrevNonCommentToken
        {
            get { return (JSTokenizerToken)_prevNonCommentToken; }
        }

        /// <summary>
        /// Gets the previous token source location. A <see cref="SourceLocation"/> is a value type.
        /// </summary>
        public SourceLocation PrevNonCommentLocation
        {
            get { return _prevNonCommentLocation; }
        }

        /// <summary>
        /// Gets the character index in the input stream before the current token.
        /// Since it is the end of the previous token, separators (white space, comments if <see cref="SkipComments"/> is 
        /// true) before the current token are included.
        /// If SkipComments is false and a comment exists before the current token, this is the index of 
        /// the end of the comment.
        /// </summary>
        public int PrevCharPosTokenEnd
        {
            get { return _prevCharPosTokenEnd; }
        }

        static string[] _binaryOperator = { "instanceof", "|",  "&",  "<<",  "^",  ">>",  ">>>",  "+",  "-",  "/",  "*",  "%",  };
        static string[] _assignOperator = { "=",          "|=", "&=", "<<=", "^=", ">>=", ">>>=", "+=", "-=", "/=", "*=", "%=" };
        static string[] _compareOperator = { "==", "<", ">", "<=", ">=", "!=", "===", "!==" };
        static string[] _punctuations = { ".", ",", "?", ":", ";" };
        static string[] _specialIdentifiers = { "delete", "new", "typeof", "void" };
        static string[] _unaryOperator = { "!", "~", "--", "++", "delete", "new", "typeof", "void" };
        static JSTokenizerToken[] _assignBinaryMap = 
        { 
            JSTokenizerToken.BitwiseOr, 
            JSTokenizerToken.BitwiseAnd, 
            JSTokenizerToken.BitwiseShiftLeft, 
            JSTokenizerToken.BitwiseXOr,
            JSTokenizerToken.BitwiseShiftRight,
            JSTokenizerToken.BitwiseShiftRightNoSignBit,
            JSTokenizerToken.Plus,
            JSTokenizerToken.Minus,
            JSTokenizerToken.Divide,
            JSTokenizerToken.Mult,
            JSTokenizerToken.Modulo
        };

        static internal JSTokenizerToken FromAssignOperatorToBinary( JSTokenizerToken assignment )
        {
            Debug.Assert( (assignment & JSTokenizerToken.IsAssignOperator) != 0 && assignment != JSTokenizerToken.Assign );
            return _assignBinaryMap[((int)assignment & 15)-2];
        }

        public static string Explain( JSTokenizerToken t )
        {
            if( t < 0 )
            {
                return ((JSTokenizerError)t).ToString();
            }
            if( (t & JSTokenizerToken.IsAssignOperator) != 0 ) return _assignOperator[((int)t & 15) - 1];
            if( (t & JSTokenizerToken.IsBinaryOperator) != 0 ) return _binaryOperator[((int)t & 15) - 1];
            if( (t & JSTokenizerToken.IsCompareOperator) != 0 ) return _compareOperator[((int)t & 15) - 1];
            if( (t & JSTokenizerToken.IsPunctuation) != 0 ) return _punctuations[((int)t & 15) - 1];
            if( (t & JSTokenizerToken.IsUnaryOperator) != 0 ) return _unaryOperator[((int)t & 15) - 1];
            
            if( t == JSTokenizerToken.Identifier ) return "identifier";
            if( t == JSTokenizerToken.And ) return "&&";
            if( t == JSTokenizerToken.Or ) return "||";
            if( t == JSTokenizerToken.PlusPlus ) return "++";
            if( t == JSTokenizerToken.MinusMinus ) return "--";
            
            if( t == JSTokenizerToken.String ) return "\"string\"";

            if( t == JSTokenizerToken.Float ) return "6.02214129e+23";
            if( t == JSTokenizerToken.Integer ) return "42";
            if( t == JSTokenizerToken.HexNumber ) return "0x00CF12A4";
            if( t == JSTokenizerToken.NaN ) return "NaN";
            if( t == JSTokenizerToken.Infinity ) return "Infinity";

            if( t == JSTokenizerToken.StarComment ) return "/* ... */";
            if( t == JSTokenizerToken.LineComment ) return "// ..." + Environment.NewLine;
            
            if( t == JSTokenizerToken.Regex ) return "/regex/gi";

            if( t == JSTokenizerToken.OpenPar ) return "(";
            if( t == JSTokenizerToken.ClosePar ) return ")";
            if( t == JSTokenizerToken.OpenBracket ) return "[";
            if( t == JSTokenizerToken.CloseBracket ) return "]";
            if( t == JSTokenizerToken.OpenCurly ) return "{";
            if( t == JSTokenizerToken.CloseCurly ) return "}";


            return JSTokenizerToken.None.ToString();
        }

        /// <summary>
        /// Reads a comment (with its opening and closing tags) and forwards head. ReturnedValue null and 
        /// does not forward the head if current token is not a comment. 
        /// To be able to read comments (ie. returning not null here) requires <see cref="SkipComments"/> to be false.
        /// </summary>
        /// <returns></returns>
        public string ReadComment()
        {
            return (_token & (int)JSTokenizerToken.IsComment) != 0 ? ReadBuffer() : null;
        }

        /// <summary>
        /// Reads a string value and forwards head. ReturnedValue null and 
        /// does not forward the head if current token is not a string. 
        /// </summary>
        /// <returns></returns>
        public string ReadString()
        {
            return _token == (int)JSTokenizerToken.String ? ReadBuffer() : null;
        }


        /// <summary>
        /// Reads an identifier and forwards head. ReturnedValue null and 
        /// does not forward the head if current token is not an identifier. 
        /// </summary>
        /// <returns></returns>
        public string ReadIdentifier()
        {
            string id = null;
            if( IsIdentifier )
            {
                id = _identifierValue;
                Forward();
            }
            return id;
        }

        /// <summary>
        /// Reads a dotted identifier and forwards head (stops on any non identifier nor dot token). 
        /// ReturnedValue null and does not forward the head if current token is not an identifier. 
        /// </summary>
        /// <remarks>
        /// If the identifier ends with a dot, this last dot is kept in the result.
        /// </remarks>
        /// <returns>The dotted identifier or null if not found.</returns>
        public string ReadDottedIdentifier()
        {
            string multiId = null;
            string id = ReadIdentifier();
            if( id != null )
            {
                multiId = id;
                while( _token == (int)JSTokenizerToken.Dot )
                {
                    multiId += '.';
                    Forward();
                    id = ReadIdentifier();
                    if( id == null ) break;
                    multiId += id;
                }
            }
            return multiId;
        }

        /// <summary>
        /// Reads an identifier that may be a string or a number (i.e. <c>myId</c> or <c>'My Identifier'</c> or <c>0.112E3</c>) and forwards head. 
        /// ReturnedValue null and does not forward the head if current token is not an identifier nor a string nor a number.
        /// Useful for reading javascript objects since a Javascript key can be any of these tokens.
        /// </summary>
        /// <returns></returns>
        public string ReadExtendedIdentifierAsString()
        {
            if( (_token & (int)JSTokenizerToken.IsIdentifier) != 0 ) return _identifierValue;
            if( (_token & (int)(JSTokenizerToken.IsString | JSTokenizerToken.IsNumber)) != 0 ) return ReadBuffer();
            return null;
        }

        /// <summary>
        /// Reads an identifier and forwards head. ReturnedValue false and 
        /// does not forward the head if current token is not an identifier. 
        /// </summary>
        /// <returns></returns>
        public bool MatchIdentifier( string identifier )
        {
            if( (_token & (int)JSTokenizerToken.IsIdentifier) != 0
                && String.CompareOrdinal( _identifierValue, identifier ) == 0 )
            {
                Forward();
                return true;
            }
            return false;
        }

        /// <summary>
        /// Matches a token. Forwards the head on success.
        /// </summary>
        /// <param name="token">Must be one of <see cref="JSTokenizerToken"/> value (not an Error one).</param>
        /// <returns>True if the given token matches.</returns>
        public bool Match( JSTokenizerToken token )
        {
            if( token < 0 ) throw new ArgumentException( "Token must not be an Error token." );
            if( _token == (int)token )
            {
                Forward();
                return true;
            }
            return false;
        }

        /// <summary>
        /// Reads a number and forwards head on success. 
        /// May return <see cref="Double.NaN"/> and does not forward the head if current token is not a number (<see cref="IsNumber"/> is false)
        /// or if the double can not be parsed by <see cref="Double.TryParse"/>. 
        /// </summary>
        /// <returns>The number or <see cref="Double.NaN"/> if <see cref="IsNumber"/> is false.</returns>
        public bool IsDouble( out double d )
        {
            d = 0;
            if( (_token & (int)JSTokenizerToken.IsNumber) == 0 ) return false;
            d = ReadDouble();
            return true;
        }

        /// <summary>
        /// Reads the current number and forwards head. Throws an <see cref="InvalidOperationException"/> if <see cref="IsNumber"/> is false.
        /// </summary>
        /// <returns>The number. It can be <see cref="Double.NaN"/> or <see cref="Double.PositiveInfinity"/>.</returns>
        public double ReadDouble()
        {
            Double d;
            if( _token == (int)JSTokenizerToken.NaN ) d = Double.NaN;
            else if( _token == (int)JSTokenizerToken.Infinity ) d = Double.PositiveInfinity;
            else if( _token == (int)JSTokenizerToken.Float )
            {
                // This is not compliant with Javascript rules: it returns 0 four huge or very small numbers.
                // It should return Infinity for huge numbers.
                Double.TryParse( _buffer.ToString(), NumberStyles.AllowExponent | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out d );
            }
            else d = _integerValue;
            Forward();
            return d;
        }

        private string ReadBuffer()
        {
            Debug.Assert( _token > 0 );
            string r = _buffer.ToString();
            Forward();
            return r;
        }

        #region Basic input
        int Peek()
        {
            return _nextC == 0 ? (_nextC = _inner.Read()) : _nextC;
        }

        bool Read( int c )
        {
            if( Peek() == c )
            {
                Read();
                return true;
            }
            return false;
        }

        int Read()
        {
            int ret;
            if( _nextC != 0 )
            {
                ret = _nextC;
                _nextC = 0;
            }
            else ret = _inner.Read();

            _charPos++;

            if( _lineInc )
            {
                _location.Line++;
                _location.Column = 1;
                _lineInc = false;
            }
            if( ret != '\r' )
            {
                // Line Separator \u2028 and Paragraph Separator \u2029
                // are mapped to \n.
                if( ret == '\n' || ret == '\u2028' || ret == '\u2029' )
                {
                    ret = '\n';
                    _lineInc = true;
                }
                _location.Column++;
            }
            return ret;
        }

        int ReadFirstNonWhiteSpace()
        {
            int c;
            for( ; ; )
            {
                switch( (c = Read()) )
                {
                    case ' ':
                    case '\t':
                    case '\r':
                    case '\n': continue;
                    default: return c;
                }
            }
        }

        static private bool IsIdentifierStartChar( int c )
        {
            return c == '_' || c == '$' || (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z');
        }

        static private bool IsIdentifierChar( int c )
        {
            return IsIdentifierStartChar( c ) || (c >= '0' && c <= '9');
        }

        static private int FromHexDigit( int c )
        {
            Debug.Assert( '0' < 'A' && 'A' < 'a' );
            c -= '0';
            if( c < 0 ) return -1;
            if( c <= 9 ) return c;
            c -= 'A' - '0';
            if( c < 0 ) return -1;
            if( c <= 5 ) return 10 + c;
            c -= 'a' - 'A';
            if( c >= 0 && c <= 5 ) return 10 + c;
            return -1;
        }

        static private int FromDecDigit( int c )
        {
            c -= '0';
            return c >= 0 && c <= 9 ? c : -1;
        }

        private bool HandleStdComment()
        {
            int ic = Read();
            if( _skipComments )
                for( ; ; )
                {
                    do { if( ic == -1 ) return false; ic = Read(); }
                    while( ic != '*' );
                    ic = Read();
                    if( ic == '/' ) return true;
                }
            else
                for( ; ; )
                {
                    do { if( ic == -1 ) return false; ic = Read(); _buffer.Append( (char)ic ); }
                    while( ic != '*' );
                    ic = Read();
                    if( ic == '/' )
                    {
                        --_buffer.Length; // Removes added *.
                        return true;
                    }
                }
        }

        private void HandleEndOfLineComment()
        {
            int ic = Read();
            if( _skipComments )
            {
                do { ic = Peek(); }
                while( ic != '\n' && Read() != -1 );
            }
            else
            {
                do { ic = Peek(); }
                while( ic != '\n' && ic != '\u2028' && ic != '\u2029'
                    && Read() != -1
                    && _buffer.Append( (char)ic ) != null );
            }
        }


        #endregion

        int HandleStarComment()
        {
            int ic;
            while( (ic = Read()) != -1 )
            {
                if( ic == '*' && Read( '/' ) ) return (int)JSTokenizerToken.StarComment;
            }
            return (int)JSTokenizerError.EndOfInput;
        }

        int HandleLineComment()
        {
            int ic;
            while( (ic = Read()) != -1 )
            {
                if( ic == '\n' ) return (int)JSTokenizerToken.LineComment;
            }
            return (int)JSTokenizerError.EndOfInput;
        }

        int HandleRegex()
        {
            int ic;
            while( (ic = Read()) != -1 )
            {
                if( ic == '\\' )
                {
                    ic = Read();
                    if( ic == -1 ) break;
                }
                else if( ic == '/' )
                {
                    while( (ic = Read()) == 'g' || ic == 'i' || ic == 'm' ) ;
                    if( ic == -1 ) break;
                    return (int)JSTokenizerToken.Regex;
                }
            }
            return (int)JSTokenizerError.ErrorRegexUnterminated;
        }

        int NextToken2()
        {
            if( _token >= 0 )
            {
                // Current char position is the end of the previous token.
                _prevCharPosTokenEnd = _charPos;

                if( (_token & (int)JSTokenizerToken.IsComment) == 0 )
                {
                    // Previous token and token location are preserved.
                    _prevNonCommentLocation = _location;
                    _prevNonCommentToken = _token;
                }

                // A cute goto loop :-)
                readToken:

                _token = NextTokenLowLevel();
                if( (_token & (int)JSTokenizerToken.IsComment) != 0 && _skipComments ) goto readToken;
            }
            return _token;
        }

        int NextTokenLowLevel()
        {
            int ic = ReadFirstNonWhiteSpace();
            // Current char position is the beginning of the new current token.
            _charPosTokenBeg = _charPos;

            if( ic == -1 ) return (int)JSTokenizerError.EndOfInput;
            switch( ic )
            {
                case '\'':
                case '\"': return ReadString( ic );
                case '=': return Read( '=' ) ? (Read('=') ? (int)JSTokenizerToken.StrictEqual : (int)JSTokenizerToken.Equal) : (int)JSTokenizerToken.Assign;
                case '*': return Read( '=' ) ? (int)JSTokenizerToken.MultAssign : (int)JSTokenizerToken.Mult;
                case '!': return Read( '=' ) ? (Read( '=' ) ? (int)JSTokenizerToken.StrictDifferent : (int)JSTokenizerToken.Different) : (int)JSTokenizerToken.Not;
                case '^':
                    if( Read( '=' ) ) return (int)JSTokenizerToken.BitwiseXOrAssign;
                    return (int)JSTokenizerToken.BitwiseXOr;
                case '&':
                    if( Read( '&' ) ) return (int)JSTokenizerToken.And;
                    if( Read( '=' ) ) return (int)JSTokenizerToken.BitwiseAndAssign;
                    return (int)JSTokenizerToken.BitwiseAnd;
                case '|':
                    if( Read( '|' ) ) return (int)JSTokenizerToken.Or;
                    if( Read( '=' ) ) return (int)JSTokenizerToken.BitwiseOrAssign;
                    return (int)JSTokenizerToken.BitwiseOr;
                case '>':
                    if( Read( '=' ) ) return (int)JSTokenizerToken.GreaterOrEqual;
                    if( Read( '>' ) )
                    {
                        if( Read( '=' ) ) return (int)JSTokenizerToken.BitwiseShiftRightAssign;
                        if( Read( '>' ) )
                        {
                            if( Read( '=' ) ) return (int)JSTokenizerToken.BitwiseShiftRightNoSignBitAssign;
                            return (int)JSTokenizerToken.BitwiseShiftRightNoSignBit;
                        }
                        return (int)JSTokenizerToken.BitwiseShiftRight;
                    }
                    return (int)JSTokenizerToken.Greater;
                case '<':
                    if( Read( '=' ) ) return (int)JSTokenizerToken.LessOrEqual;
                    if( Read( '<' ) )
                    {
                        if( Read( '=' ) ) return (int)JSTokenizerToken.BitwiseShiftLeftAssign;
                        return (int)JSTokenizerToken.BitwiseShiftLeft;
                    }
                    return (int)JSTokenizerToken.Less;
                case '.':
                    // A number can start with a dot.
                    ic = FromDecDigit( Peek() );
                    if( ic >= 0 )
                    {
                        Read();
                        return ReadNumber( ic, true );
                    }
                    return (int)JSTokenizerToken.Dot;
                case '{': return (int)JSTokenizerToken.OpenCurly;
                case '}': return (int)JSTokenizerToken.CloseCurly;
                case '(': return (int)JSTokenizerToken.OpenPar;
                case ')': return (int)JSTokenizerToken.ClosePar;
                case '[': return (int)JSTokenizerToken.OpenSquare;
                case ']': return (int)JSTokenizerToken.CloseSquare;
                case ':': return (int)JSTokenizerToken.Colon;
                case ';': return (int)JSTokenizerToken.SemiColon;
                case ',': return (int)JSTokenizerToken.Comma;
                case '?': return (int)JSTokenizerToken.QuestionMark;
                case '/':
                    {
                        if( Read( '*' ) ) return HandleStarComment();
                        if( Read( '/' ) ) return HandleLineComment();
                        if( Read( '=' ) ) return (int)JSTokenizerToken.DivideAssign;
                        if( (_prevNonCommentToken & (int)(JSTokenizerToken.IsIdentifier | JSTokenizerToken.IsString | JSTokenizerToken.IsNumber)) != 0
                            || _prevNonCommentToken == (int)JSTokenizerToken.ClosePar
                            || _prevNonCommentToken == (int)JSTokenizerToken.CloseSquare
                            || _prevNonCommentToken == (int)JSTokenizerToken.PlusPlus
                            || _prevNonCommentToken == (int)JSTokenizerToken.MinusMinus ) return (int)JSTokenizerToken.Divide;
                        return HandleRegex();
                    }
                case '-':
                    if( Read( '-' ) ) return (int)JSTokenizerToken.MinusMinus;
                    if( Read( '=' ) ) return (int)JSTokenizerToken.MinusAssign;
                    return (int)JSTokenizerToken.Minus;
                case '+':
                    if( Read( '+' ) ) return (int)JSTokenizerToken.PlusPlus;
                    if( Read( '=' ) ) return (int)JSTokenizerToken.PlusAssign;
                    return (int)JSTokenizerToken.Plus;
                case '%':
                    if( Read( '=' ) ) return (int)JSTokenizerToken.ModuloAssign;
                    return (int)JSTokenizerToken.Modulo;
                case '~':
                    return (int)JSTokenizerToken.BitwiseNot;
                default:
                    {
                        int digit = FromDecDigit( ic );
                        if( digit >= 0 ) return ReadAllKindOfNumber( digit );
                        if( IsIdentifierStartChar( ic ) ) return ReadIdentifier( ic );
                        return (int)JSTokenizerError.ErrorInvalidChar;
                    }
            }
        }

        private int ReadAllKindOfNumber( int firstDigit )
        {
            Debug.Assert( firstDigit >= 0 && firstDigit <= 9 );
            if( firstDigit == 0 && Read( 'x' ) ) return ReadHexNumber();
            return ReadNumber( firstDigit, false );
        }

        private int ReadHexNumber()
        {
            ulong uValue;
            int nbD = IsPositiveHexNumber( out uValue, -1 );
            if( nbD == 0 ) return (int)JSTokenizerError.ErrorNumberUnterminatedValue;
            _integerValue = (int)uValue;
            return (int)JSTokenizerToken.HexNumber;
        }

        /// <summary>
        /// ReturnedValue the number of processed digits.
        /// </summary>
        private int IsPositiveHexNumber( out ulong val, int maxNbDigit )
        {
            unchecked
            {
                int nbDigit = 0;
                val = 0;
                int vHex;
                while( (vHex = FromHexDigit( Peek() )) >= 0 )
                {
                    Debug.Assert( vHex < 16 );
                    if( nbDigit < 16 )
                    {
                        val *= 16;
                        val += (uint)vHex;
                    }
                    Read();
                    if( ++nbDigit == maxNbDigit ) break;
                }
                return nbDigit;
            }
        }

        /// <summary>
        /// May return an error code or a number token.
        /// Whatever the read result is, the buffer contains the token.
        /// </summary>
        private int ReadNumber( int firstDigit, bool hasDot )
        {
            bool hasExp = false;
            int nextRequired = 0;
            _buffer.Length = 0;
            if( hasDot ) _buffer.Append( "0." );
            else _integerValue = firstDigit;
            _buffer.Append( (char)(firstDigit+'0') );
            for( ; ; )
            {
                int ic = Peek();
                if( ic >= '0' && ic <= '9' )
                {
                    Read();
                    _buffer.Append( (char)ic );
                    if( !hasDot ) _integerValue = _integerValue * 10 + (ic - '0');
                    nextRequired = 0;
                    continue;
                }
                if( !hasExp && (ic == 'e' || ic == 'E') )
                {
                    Read();
                    hasExp = hasDot = true;
                    _buffer.Append( 'E' );
                    if( Read( '-' ) ) _buffer.Append( '-' ); 
                    else Read( '+' );
                    // At least a digit is required.
                    nextRequired = 1;
                    continue;
                }
                if( ic == '.' )
                {
                    if( !hasDot )
                    {
                        Read();
                        hasDot = true;
                        _buffer.Append( '.' );
                        // Dot can be the last character. 
                        // Use 2 to remember that dot has been found: we consider it as an integer value.
                        nextRequired = 2;
                        continue;
                    }
                    return (int)JSTokenizerError.ErrorNumberIdentifierStartsImmediately;
                }

                if( nextRequired == 1 ) return (int)JSTokenizerError.ErrorNumberUnterminatedValue;
                // To be valid, the number must be followed by an operator, a punctuation or a statement separator (the ';')
                // or a line ending (recall that awful javascript "feature": lines without ending ';' 
                // are automagically corrected if 'needed').
                // We do not handle all cases here, except the 45DD.
                if( IsIdentifierStartChar( ic ) ) return (int)JSTokenizerError.ErrorNumberIdentifierStartsImmediately;
                break;
            }
            if( hasDot )
            {
                // Consider number terminated by dot as integer.
                if( nextRequired != 2 ) return (int)JSTokenizerToken.Float;
            }
            return (int)JSTokenizerToken.Integer;
        }

        private int ReadString( int quote )
        {
            _buffer.Length = 0;
            ulong icu;
            for( ; ; )
            {
                int ic = Read();
                if( ic == -1 ) return (int)JSTokenizerError.ErrorStringUnterminated;
                if( ic == quote ) break;
                else if( ic == '\\' )
                {
                    ic = Read();
                    switch( ic )
                    {
                        case '"': break;
                        case '\'': break;
                        case '\\': break;
                        case 'r': ic = '\r'; break;
                        case 'n': ic = '\n'; break;
                        case 't': ic = '\t'; break;
                        case 'b': ic = '\b'; break;
                        case 'v': ic = '\v'; break;
                        case 'f': ic = '\f'; break;
                        case 'u':
                            // Reads an Unicode Char like \uXXXX
                            icu = 0;
                            unchecked
                            {
                                int vHex;
                                for( int x = 0; x < 4; ++x )
                                {
                                    vHex = FromHexDigit( Peek() );
                                    if( vHex < 0 ) return (int)JSTokenizerError.ErrorStringEmbeddedUnicodeValue;
                                    Debug.Assert( vHex < 16 );
                                    icu *= 16;
                                    icu += (uint)vHex;
                                    Read();
                                }
                            }
                            ic = (int)icu;
                            break;
                        case 'x':
                            // Allow only \xNN (2 digits): this is the norm.
                            if( IsPositiveHexNumber( out icu, 2 ) != 2 ) return (int)JSTokenizerError.ErrorStringEmbeddedHexaValue;
                            ic = (int)icu;
                            break;
                        case '\r':  // Read transforms Line Separator '\u2028' and Paragraph Separator '\u2029' in '\n' 
                            // New JS (1.5?) supports the \ as a line continuation: we can just continue our loop...
                            // If a \n follows, we eat it. If no '\n' follows, this is an error.
                            if( !Read( '\n' ) ) return (int)JSTokenizerError.ErrorStringUnexpectedCRInLineContinuation;
                            ic = '\n';
                            break;
                        case '\n':
                            // Read transforms Line Separator '\u2028' and Paragraph Separator '\u2029' in '\n' 
                            // New JS (1.5?) supports the \ as a line continuation: we can just continue our loop...
                            break;
                        case -1: return (int)JSTokenizerError.ErrorStringUnterminated;
                        default: break;
                    }
                }
                _buffer.Append( (char)ic );
            }
            return (int)JSTokenizerToken.String;
        }

        private int ReadIdentifier( int ic )
        {
            Debug.Assert( IsIdentifierStartChar( ic ) );
            _buffer.Length = 0;
            for( ; ; )
            {
                _buffer.Append( (char)ic );
                if( (IsIdentifierChar( ic = Peek() )) ) Read();
                else break;
            }
            _identifierValue = _buffer.ToString();
            switch( _identifierValue )
            {
                case "instanceof": return (int)JSTokenizerToken.InstanceOf;
                case "delete": return (int)JSTokenizerToken.Delete;
                case "new": return (int)JSTokenizerToken.New;
                case "typeof": return (int)JSTokenizerToken.TypeOf;
                case "void": return (int)JSTokenizerToken.Void;
                case "NaN": return (int)JSTokenizerToken.NaN;
                case "Infinity": return (int)JSTokenizerToken.Infinity;
            }
            return (int)JSTokenizerToken.Identifier;
        }

        public override string ToString()
        {
            return "CurrentToken = " + Explain( CurrentToken );
        }
    }
}
