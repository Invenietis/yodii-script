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
    public class Tokenizer
    {
        #region Private fields

        TextReader _inner;
        int _prevCharPosTokenEnd;
        int _charPosTokenBeg;
        int _charPos;
        int _nextC;
        SourceLocation _prevNonCommentLocation;
        SourceLocation _location;
        bool _lineInc;
        int _integerValue;
        string _identifierValue;

        StringBuilder _buffer;
        int _token;
        int _prevNonCommentToken;
        TokenizerOptions _options;

        readonly static TokenizerOptions _defaultOptions = new TokenizerOptions();

        #endregion

        public Tokenizer( TokenizerOptions options = null )
        {
            _options = options ?? _defaultOptions;
            _buffer = new StringBuilder( 512 );
        }

        public Tokenizer( string input, TokenizerOptions options = null, string source = SourceLocation.NoSource, int startLineNumber = 0, int startColumnNumber = 0 )
            : this( options )
        {
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
        /// Gets the current <see cref="TokenizerToken"/> code.
        /// </summary>
        public TokenizerToken CurrentToken
        {
            get { return (TokenizerToken)_token; }
        }

        /// <summary>
        /// Gets the <see cref="TokenizerError"/> code if the parser is in error
        /// (or the end of the input is reached). <see cref="TokenizerError.None"/> if
        /// no error occured.
        /// </summary>
        public TokenizerError ErrorCode
        {
            get { return _token < 0 ? (TokenizerError)_token : TokenizerError.None; }
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
        /// True if <see cref="ErrorCode"/> is <see cref="TokenizerError.EndOfInput"/>.
        /// </summary>
        /// <returns></returns>
        public bool IsEndOfInput
        {
            get { return _token == (int)TokenizerError.EndOfInput; }
        }

        public bool IsAssignOperator
        {
            get { return (_token & (int)TokenizerToken.IsAssignOperator) != 0; }
        }

        public bool IsBinaryOperator
        {
            get { return (_token & (int)TokenizerToken.IsBinaryOperator) != 0; }
        }

        public bool IsBracket
        {
            get { return (_token & (int)TokenizerToken.IsBracket) != 0; }
        }

        public bool IsCompareOperator
        {
            get { return (_token & (int)TokenizerToken.IsCompareOperator) != 0; }
        }

        public bool IsComment
        {
            get { return (_token & (int)TokenizerToken.IsComment) != 0; }
        }

        /// <summary>
        /// True if the current token is an identifier. <see cref="ReadIdentifier"/> can be called to get 
        /// the actual value.
        /// </summary>
        public bool IsIdentifier
        {
            get { return (_token & (int)TokenizerToken.IsIdentifier) != 0; }
        }

        public bool IsLogical
        {
            get { return (_token & (int)TokenizerToken.IsLogical) != 0; }
        }

        #region IsNumber, IsNumberFloat and IsNumberInteger
        /// <summary>
        /// True if the current token is a number. <see cref="ReadNumber"/> can be called to get 
        /// the actual value.
        /// </summary>
        public bool IsNumber
        {
            get { return (_token & (int)TokenizerToken.IsNumber) != 0; }
        }

        /// <summary>
        /// True if the current token is a float number (i.e. with a fractional and/or 
        /// exponent part). <see cref="ReadNumber"/> can be called to get the value.
        /// </summary>
        public bool IsNumberFloat
        {
            get { return _token == (int)TokenizerToken.Float; }
        }

        /// <summary>
        /// True if the current token is an integer number (i.e. without any fractional nor 
        /// exponent part). <see cref="ReadNumber"/> can be called to get the value.
        /// </summary>
        public bool IsNumberInteger
        {
            get { return (_token & (int)TokenizerToken.Integer) == (int)TokenizerToken.Integer; }
        }
        #endregion

        public bool IsPunctuation
        {
            get { return (_token & (int)TokenizerToken.IsPunctuation) != 0; }
        }

        public bool IsRegex
        {
            get { return (_token & (int)TokenizerToken.IsRegex) != 0; }
        }

        /// <summary>
        /// True if the current token is a string. <see cref="ReadString"/> can be called to get 
        /// the actual value.
        /// </summary>
        public bool IsString
        {
            get { return (_token & (int)TokenizerToken.IsString) != 0; }
        }

        /// <summary>
        /// True when <see cref="CurrentToken"/> is "++", "--", "-", "~", "!", "delete", "new", "typeof" or "void".
        /// </summary>
        public bool IsUnaryOperator
        {
            get { return (_token & (int)TokenizerToken.IsUnaryOperator) != 0; }
        }

        /// <summary>
        /// Gets whether the current token is one of the <see cref="IsUnaryOperator"/> or <see cref="TokenizerToken.Plus"/> or <see cref="TokenizerToken.Minus"/>.
        /// </summary>
        public bool IsUnaryOperatorOrPlusOrMinus
        {
            get { return IsUnaryOperator || _token == (int)TokenizerToken.Plus || _token == (int)TokenizerToken.Minus; }
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
        public TokenizerToken PrevNonCommentToken
        {
            get { return (TokenizerToken)_prevNonCommentToken; }
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

        /// <summary>
        /// Reads a comment (with its opening and closing tags) and forwards head. ReturnedValue null and 
        /// does not forward the head if current token is not a comment. 
        /// To be able to read comments (ie. returning not null here), <see cref="TokenizerOptions.SkipComments"/> 
        /// must be false.
        /// </summary>
        /// <returns></returns>
        public string ReadComment()
        {
            return (_token & (int)TokenizerToken.IsComment) != 0 ? ReadBuffer() : null;
        }

        /// <summary>
        /// Reads a string value and forwards head. ReturnedValue null and 
        /// does not forward the head if current token is not a string. 
        /// </summary>
        /// <returns></returns>
        public string ReadString()
        {
            return _token == (int)TokenizerToken.String ? ReadBuffer() : null;
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
                while( _token == (int)TokenizerToken.Dot )
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
            if( (_token & (int)TokenizerToken.IsIdentifier) != 0 ) return _identifierValue;
            if( (_token & (int)(TokenizerToken.IsString | TokenizerToken.IsNumber)) != 0 ) return ReadBuffer();
            return null;
        }

        /// <summary>
        /// Reads an identifier and forwards head. ReturnedValue false and 
        /// does not forward the head if current token is not an identifier. 
        /// </summary>
        /// <returns></returns>
        public bool MatchIdentifier( string identifier )
        {
            if( (_token & (int)TokenizerToken.IsIdentifier) != 0
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
        /// <param name="token">Must be one of <see cref="TokenizerToken"/> value (not an Error one).</param>
        /// <returns>True if the given token matches.</returns>
        public bool Match( TokenizerToken token )
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
            if( (_token & (int)TokenizerToken.IsNumber) == 0 ) return false;
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
            if( _token == (int)TokenizerToken.NaN ) d = Double.NaN;
            else if( _token == (int)TokenizerToken.Infinity ) d = Double.PositiveInfinity;
            else if( _token == (int)TokenizerToken.Float )
            {
                // This is not compliant with Javascript rules: it returns 0 four huge or very small numbers.
                // It should return Infinity for huge numbers.
                double.TryParse( _buffer.ToString(), NumberStyles.AllowExponent | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out d );
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
            for( ;;)
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
            if( _options.SkipComments )
                for( ;;)
                {
                    do { if( ic == -1 ) return false; ic = Read(); }
                    while( ic != '*' );
                    ic = Read();
                    if( ic == '/' ) return true;
                }
            else
                for( ;;)
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
            if( _options.SkipComments )
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
                if( ic == '*' && Read( '/' ) ) return (int)TokenizerToken.StarComment;
            }
            return (int)TokenizerError.EndOfInput;
        }

        int HandleLineComment()
        {
            int ic;
            while( (ic = Read()) != -1 )
            {
                if( ic == '\n' ) return (int)TokenizerToken.LineComment;
            }
            return (int)TokenizerError.EndOfInput;
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
                    return (int)TokenizerToken.Regex;
                }
            }
            return (int)TokenizerError.ErrorRegexUnterminated;
        }

        int NextToken2()
        {
            if( _token >= 0 )
            {
                // Current char position is the end of the previous token.
                _prevCharPosTokenEnd = _charPos;

                if( (_token & (int)TokenizerToken.IsComment) == 0 )
                {
                    // Previous token and token location are preserved.
                    _prevNonCommentLocation = _location;
                    _prevNonCommentToken = _token;
                }

                // A cute goto loop :-)
                readToken:

                _token = NextTokenLowLevel();
                if( (_token & (int)TokenizerToken.IsComment) != 0 && _options.SkipComments ) goto readToken;
            }
            return _token;
        }

        int NextTokenLowLevel()
        {
            int ic = ReadFirstNonWhiteSpace();
            // Current char position is the beginning of the new current token.
            _charPosTokenBeg = _charPos;

            if( ic == -1 ) return (int)TokenizerError.EndOfInput;
            switch( ic )
            {
                case '\'':
                case '\"': return ReadString( ic );
                case '=':
                    return Read( '=' )
                              ? (int)TokenizerToken.Equal
                              : (_options.UsePascalAssign ? (int)TokenizerToken.Equal : (int)TokenizerToken.Assign);
                case '*': return Read( '=' ) ? (int)TokenizerToken.MultAssign : (int)TokenizerToken.Mult;
                case '!': return Read( '=' ) ? (int)TokenizerToken.Different : (int)TokenizerToken.Not;
                case '^':
                    if( Read( '=' ) ) return (int)TokenizerToken.BitwiseXOrAssign;
                    return (int)TokenizerToken.BitwiseXOr;
                case '&':
                    if( Read( '&' ) ) return (int)TokenizerToken.And;
                    if( Read( '=' ) ) return (int)TokenizerToken.BitwiseAndAssign;
                    return (int)TokenizerToken.BitwiseAnd;
                case '|':
                    if( Read( '|' ) ) return (int)TokenizerToken.Or;
                    if( Read( '=' ) ) return (int)TokenizerToken.BitwiseOrAssign;
                    return (int)TokenizerToken.BitwiseOr;
                case '>':
                    if( Read( '=' ) ) return (int)TokenizerToken.GreaterOrEqual;
                    if( Read( '>' ) )
                    {
                        if( Read( '=' ) ) return (int)TokenizerToken.BitwiseShiftRightAssign;
                        if( Read( '>' ) )
                        {
                            if( Read( '=' ) ) return (int)TokenizerToken.BitwiseShiftRightNoSignBitAssign;
                            return (int)TokenizerToken.BitwiseShiftRightNoSignBit;
                        }
                        return (int)TokenizerToken.BitwiseShiftRight;
                    }
                    return (int)TokenizerToken.Greater;
                case '<':
                    if( Read( '=' ) ) return (int)TokenizerToken.LessOrEqual;
                    if( Read( '<' ) )
                    {
                        if( Read( '=' ) ) return (int)TokenizerToken.BitwiseShiftLeftAssign;
                        return (int)TokenizerToken.BitwiseShiftLeft;
                    }
                    return (int)TokenizerToken.Less;
                case '.':
                    // A number can start with a dot.
                    ic = FromDecDigit( Peek() );
                    if( ic >= 0 )
                    {
                        Read();
                        return ReadNumber( ic, true );
                    }
                    return (int)TokenizerToken.Dot;
                case '{': return (int)TokenizerToken.OpenCurly;
                case '}': return (int)TokenizerToken.CloseCurly;
                case '(': return (int)TokenizerToken.OpenPar;
                case ')': return (int)TokenizerToken.ClosePar;
                case '[': return (int)TokenizerToken.OpenSquare;
                case ']': return (int)TokenizerToken.CloseSquare;
                case ':':
                    if( _options.UsePascalAssign && Read( '=' ) ) return (int)TokenizerToken.Assign;
                    return (int)TokenizerToken.Colon;
                case ';': return (int)TokenizerToken.SemiColon;
                case ',': return (int)TokenizerToken.Comma;
                case '?': return (int)TokenizerToken.QuestionMark;
                case '/':
                    {
                        if( Read( '*' ) ) return HandleStarComment();
                        if( Read( '/' ) ) return HandleLineComment();
                        if( Read( '=' ) ) return (int)TokenizerToken.DivideAssign;
                        if( (_prevNonCommentToken & (int)(TokenizerToken.IsIdentifier | TokenizerToken.IsString | TokenizerToken.IsNumber)) != 0
                            || _prevNonCommentToken == (int)TokenizerToken.ClosePar
                            || _prevNonCommentToken == (int)TokenizerToken.CloseSquare
                            || _prevNonCommentToken == (int)TokenizerToken.PlusPlus
                            || _prevNonCommentToken == (int)TokenizerToken.MinusMinus ) return (int)TokenizerToken.Divide;
                        return HandleRegex();
                    }
                case '-':
                    if( Read( '-' ) ) return (int)TokenizerToken.MinusMinus;
                    if( Read( '=' ) ) return (int)TokenizerToken.MinusAssign;
                    return (int)TokenizerToken.Minus;
                case '+':
                    if( Read( '+' ) ) return (int)TokenizerToken.PlusPlus;
                    if( Read( '=' ) ) return (int)TokenizerToken.PlusAssign;
                    return (int)TokenizerToken.Plus;
                case '%':
                    if( Read( '=' ) ) return (int)TokenizerToken.ModuloAssign;
                    return (int)TokenizerToken.Modulo;
                case '~':
                    return (int)TokenizerToken.BitwiseNot;
                default:
                    {
                        int digit = FromDecDigit( ic );
                        if( digit >= 0 ) return ReadAllKindOfNumber( digit );
                        if( IsIdentifierStartChar( ic ) ) return ReadIdentifier( ic );
                        return (int)TokenizerError.ErrorInvalidChar;
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
            if( nbD == 0 ) return (int)TokenizerError.ErrorNumberUnterminatedValue;
            _integerValue = (int)uValue;
            return (int)TokenizerToken.HexNumber;
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
            _buffer.Append( (char)(firstDigit + '0') );
            for( ;;)
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
                    return (int)TokenizerError.ErrorNumberIdentifierStartsImmediately;
                }

                if( nextRequired == 1 ) return (int)TokenizerError.ErrorNumberUnterminatedValue;
                // To be valid, the number must be followed by an operator, a punctuation or a statement separator (the ';')
                // or a line ending (recall that awful javascript "feature": lines without ending ';' 
                // are automagically corrected if 'needed').
                // We do not handle all cases here, except the 45DD.
                if( IsIdentifierStartChar( ic ) ) return (int)TokenizerError.ErrorNumberIdentifierStartsImmediately;
                break;
            }
            if( hasDot )
            {
                // Consider number terminated by dot as integer.
                if( nextRequired != 2 ) return (int)TokenizerToken.Float;
            }
            return (int)TokenizerToken.Integer;
        }

        private int ReadString( int quote )
        {
            _buffer.Length = 0;
            ulong icu;
            for( ;;)
            {
                int ic = Read();
                if( ic == -1 ) return (int)TokenizerError.ErrorStringUnterminated;
                if( ic == quote )
                {
                    if( Peek() != quote ) break;
                    Read();
                }
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
                                    if( vHex < 0 ) return (int)TokenizerError.ErrorStringEmbeddedUnicodeValue;
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
                            if( IsPositiveHexNumber( out icu, 2 ) != 2 ) return (int)TokenizerError.ErrorStringEmbeddedHexaValue;
                            ic = (int)icu;
                            break;
                        case '\r':  // Read transforms Line Separator '\u2028' and Paragraph Separator '\u2029' in '\n' 
                            // New JS (1.5?) supports the \ as a line continuation: we can just continue our loop...
                            // If a \n follows, we eat it. If no '\n' follows, this is an error.
                            if( !Read( '\n' ) ) return (int)TokenizerError.ErrorStringUnexpectedCRInLineContinuation;
                            ic = '\n';
                            break;
                        case '\n':
                            // Read transforms Line Separator '\u2028' and Paragraph Separator '\u2029' in '\n' 
                            // New JS (1.5?) supports the \ as a line continuation: we can just continue our loop...
                            break;
                        case -1: return (int)TokenizerError.ErrorStringUnterminated;
                        default: break;
                    }
                }
                _buffer.Append( (char)ic );
            }
            return (int)TokenizerToken.String;
        }

        private int ReadIdentifier( int ic )
        {
            Debug.Assert( IsIdentifierStartChar( ic ) );
            _buffer.Length = 0;
            for( ;;)
            {
                _buffer.Append( (char)ic );
                if( (IsIdentifierChar( ic = Peek() )) ) Read();
                else break;
            }
            _identifierValue = _buffer.ToString();
            switch( _identifierValue )
            {
                case "instanceof": return (int)TokenizerToken.InstanceOf;
                case "delete": return (int)TokenizerToken.Delete;
                case "new": return (int)TokenizerToken.New;
                case "typeof": return (int)TokenizerToken.TypeOf;
                case "indexof": return (int)TokenizerToken.IndexOf;
                case "void": return (int)TokenizerToken.Void;
                case "NaN": return (int)TokenizerToken.NaN;
                case "Infinity": return (int)TokenizerToken.Infinity;
            }
            return (int)TokenizerToken.Identifier;
        }

        /// <summary>
        /// This is just to ease debugging.
        /// </summary>
        /// <returns>Readable expression.</returns>
        public override string ToString()
        {
            return "CurrentToken = " + CurrentToken.Explain();
        }
    }
}
