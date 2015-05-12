#region LGPL License
/*----------------------------------------------------------------------------
* This file (Yodii.Script\Tokenizer\JSTokeniserToken.cs) is part of Yodii-Script. 
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

namespace Yodii.Script
{
    /// <summary>
    /// Tokens definition.
    /// </summary>
    /// <remarks>
    /// 
    /// From http://msdn.microsoft.com/en-us/library/z3ks45k7%28v=vs.94%29.aspx
    /// 
    /// Operator                                    Description
    ///
    /// 15  . [ (                                   Field access, array indexing, function calls, and expression grouping
    /// 14  ++ -- - ~ ! delete new typeof void      Unary operators, return data type, object creation, undefined values
    /// 13  * / %                                   Multiplication, division, modulo division
    /// 12  + - +                                   Addition, subtraction, string concatenation
    /// 11  &lt;&lt; &gt;&gt; &gt;&gt;&gt;          Bit shifting
    /// 10  &lt; &lt;= &gt; &gt;= instanceof        Less than, less than or equal, greater than, greater than or equal, instanceof
    ///  9  == != === !==                           Equality, inequality, strict equality, and strict inequality
    ///  8  &amp;                                   Bitwise AND
    ///  7  ^                                       Bitwise XOR
    ///  6  |                                       Bitwise OR
    ///  5  &amp;&amp;                              Logical AND
    ///  4  ||                                      Logical OR
    ///  3  ?                                       Conditional (?:)
    ///  2  = OP=                                   Assignment, assignment with operation (such as += and &=)
    ///  1  ,                                       Multiple evaluation
    /// </remarks>
    [Flags]
    public enum JSTokenizerToken
    {
        None = 0,

        #region JSParserError values bits n°31 to 26.
        IsErrorOrEndOfInput = JSTokenizerError.IsErrorOrEndOfInput,
        EndOfInput = JSTokenizerError.EndOfInput,
        ErrorMask = JSTokenizerError.ErrorMask,
        ErrorInvalidChar = JSTokenizerError.ErrorInvalidChar,
        ErrorStringMask = JSTokenizerError.ErrorStringMask,
        ErrorNumberMask = JSTokenizerError.ErrorNumberMask,
        ErrorRegexMask = JSTokenizerError.ErrorRegexMask,
        ErrorStringUnterminated = JSTokenizerError.ErrorStringUnterminated,
        ErrorStringEmbeddedUnicodeValue = JSTokenizerError.ErrorStringEmbeddedUnicodeValue,
        ErrorStringEmbeddedHexaValue = JSTokenizerError.ErrorStringEmbeddedHexaValue,
        ErrorStringUnexpectedCRInLineContinuation = JSTokenizerError.ErrorStringUnexpectedCRInLineContinuation,
        ErrorNumberUnterminatedValue = JSTokenizerError.ErrorNumberUnterminatedValue,
        ErrorNumberValue = JSTokenizerError.ErrorNumberValue,
        ErrorRegexUnterminated = JSTokenizerError.ErrorRegexUnterminated,
        #endregion

        #region Operator precedence bits n°25 to 21 (levels from 0 to 15).
        OpLevelShift = 21,
        OpLevelMask = 15 << OpLevelShift,

        OpLevel00 = 0,
        OpLevel01 = 1 << OpLevelShift,
        OpLevel02 = 2 << OpLevelShift,
        OpLevel03 = 3 << OpLevelShift,
        OpLevel04 = 4 << OpLevelShift,
        OpLevel05 = 5 << OpLevelShift,
        OpLevel06 = 6 << OpLevelShift,
        OpLevel07 = 7 << OpLevelShift,
        OpLevel08 = 8 << OpLevelShift,
        OpLevel09 = 9 << OpLevelShift,
        OpLevel10 = 10 << OpLevelShift,
        OpLevel11 = 11 << OpLevelShift,
        OpLevel12 = 12 << OpLevelShift,
        OpLevel13 = 13 << OpLevelShift,
        OpLevel14 = 14 << OpLevelShift,
        OpLevel15 = 15 << OpLevelShift,
        #endregion

        #region Token discriminators bits n°19 to 8 (IsAssignOperator to IsUnaryOperator).
        /// <summary>
        /// Covers =, ~=, |=, &amp;=, &lt;&lt;=, ^=, &gt;&gt;=, &gt;&gt;&gt;=, +=, -=, /=, *= and %=.
        /// </summary>
        IsAssignOperator = 1 << 19,
        
        /// <summary>
        /// Covers |, ^, &amp;, &gt;&gt;, &lt;&lt;, &gt;&gt;&gt;, +, -, /, * and %.
        /// </summary>
        IsBinaryOperator = 1 << 18,

        /// <summary>
        /// Covers [], () and {}.
        /// </summary>
        IsBracket = 1 << 17,

        /// <summary>
        /// Covers ==, &lt;, &gt;, &lt;=, &gt;=, !=, === and !==.
        /// </summary>
        IsCompareOperator = 1 << 16,

        /// <summary>
        /// Covers /* ... */ block as well as // line comment.
        /// </summary>
        IsComment = 1 << 15,

        /// <summary>
        /// Covers identifiers.
        /// </summary>
        IsIdentifier = 1 << 14,

        /// <summary>
        /// Covers || and &amp;&amp;.
        /// </summary>
        IsLogical = 1 << 13,

        /// <summary>
        /// Covers float and integer (octal or hexadecimal). 
        /// </summary>
        IsNumber = 1 << 12,

        /// <summary>
        /// Covers ".", ",", "?", ":" and ";".
        /// </summary>
        IsPunctuation = 1 << 11,

        /// <summary>
        /// Inline /regex/ definition.
        /// </summary>
        IsRegex = 1 << 10,

        /// <summary>
        /// Covers strings.
        /// </summary>
        IsString = 1 << 9,

        /// <summary>
        /// Covers !, ~, ++ and --.
        /// </summary>
        IsUnaryOperator = 1 << 8,
        #endregion

        BinaryOperatorCount = 12,
        #region IsBinaryOperator: instanceof, |, &, <<, ^, >>, >>>, +, -, /, *, %.

        /// <summary>
        /// The "a instanceof CtorFunc" operator.
        /// </summary>
        InstanceOf = IsBinaryOperator | OpLevel10 | 1,

        /// <summary>
        /// Single pipe (|) bitwise OR operator.
        /// </summary>
        BitwiseOr = IsBinaryOperator | OpLevel06 | 2,
        /// <summary>
        /// Single ampersand (&) binary And operator.
        /// </summary>
        BitwiseAnd = IsBinaryOperator | OpLevel08 | 3,
        /// <summary>
        /// Double &lt; (&lt;&lt;) binary shift left operator.
        /// </summary>
        BitwiseShiftLeft = IsBinaryOperator | OpLevel11 | 4,
        /// <summary>
        /// Xor binary (^) operator.
        /// </summary>
        BitwiseXOr = IsBinaryOperator | OpLevel07 | 5,
        /// <summary>
        /// Double &gt; (&gt;&gt;) binary shift operator.
        /// </summary>
        BitwiseShiftRight = IsBinaryOperator | OpLevel11 | 6,
        /// <summary>
        /// Triple &gt; (&gt;&gt;&gt;) binary shift operator (zero padding).
        /// </summary>
        BitwiseShiftRightNoSignBit = IsBinaryOperator | OpLevel11 | 7,
        /// <summary>
        /// Single plus character (+).
        /// </summary>
        Plus = IsBinaryOperator | OpLevel12 | 8,
        /// <summary>
        /// Single minus character (-).
        /// </summary>
        Minus = IsBinaryOperator | OpLevel12 | 9,
        /// <summary>
        /// Single divide character (/).
        /// </summary>
        Divide = IsBinaryOperator | OpLevel13 | 10,
        /// <summary>
        /// Single star character (*).
        /// </summary>
        Mult = IsBinaryOperator | OpLevel13 | 11,
        /// <summary>
        /// Modulo %.
        /// </summary>
        Modulo = IsBinaryOperator | OpLevel13 | BinaryOperatorCount,

        #endregion

        AssignOperatorCount = 12,
        #region IsAssignOperator: =, |=, &=, <<=, ^=, >>=, >>>=, +=, -=, /=, *= and %=.
        /// <summary>
        /// Single equal character (=).
        /// </summary>
        Assign = IsAssignOperator | OpLevel02 | 1,

        /// <summary>
        /// Bitwise Or assignment (|=).
        /// </summary>
        BitwiseOrAssign = IsAssignOperator | OpLevel02 | 2,

        /// <summary>
        /// Bitwise And assignment (&=).
        /// </summary>
        BitwiseAndAssign = IsAssignOperator | OpLevel02 | 3,

        /// <summary>
        /// Bitwise shift left operator assignment (&lt;&lt;=).
        /// </summary>
        BitwiseShiftLeftAssign = IsAssignOperator | OpLevel02 | 4,

        /// <summary>
        /// Xor binary (^) operator assignment (^=).
        /// </summary>
        BitwiseXOrAssign = IsAssignOperator | OpLevel02 | 5,

        /// <summary>
        /// Bitwise shift right operator assignment (&gt;gt;=).
        /// </summary>
        BitwiseShiftRightAssign = IsAssignOperator | OpLevel02 | 6,

        /// <summary>
        /// Zero padded bitwise shift right operator assignment (&gt;gt;gt;=).
        /// </summary>
        BitwiseShiftRightNoSignBitAssign = IsAssignOperator | OpLevel02 | 7,

        /// <summary>
        /// Add assignment (+=).
        /// </summary>
        PlusAssign = IsAssignOperator | OpLevel02 | 8,

        /// <summary>
        /// Substract assignment (-=).
        /// </summary>
        MinusAssign = IsAssignOperator | OpLevel02 | 9,

        /// <summary>
        /// Divide assignment (/=).
        /// </summary>
        DivideAssign = IsAssignOperator | OpLevel02 | 10,

        /// <summary>
        /// Multiplication assignment (*=).
        /// </summary>
        MultAssign = IsAssignOperator | OpLevel02 | 11,

        /// <summary>
        /// Modulo assignment (%=).
        /// </summary>
        ModuloAssign = IsAssignOperator | OpLevel02 | AssignOperatorCount,

        #endregion

        CompareOperatorCount = 8,
        #region IsCompareOperator: ==, <, >, <=, >=, !=, === and !==.
        /// <summary>
        /// Double = character (==).
        /// </summary>
        Equal = IsCompareOperator | OpLevel09 | 1,
        /// <summary>
        /// One single &lt; character.
        /// </summary>
        Less = IsCompareOperator | OpLevel10 | 2,
        /// <summary>
        /// One single &gt; character.
        /// </summary>
        Greater = IsCompareOperator | OpLevel10 | 3,
        /// <summary>
        /// Less than or equal (&lt;=) 
        /// </summary>
        LessOrEqual = IsCompareOperator | OpLevel10 | 4,
        /// <summary>
        /// Greater than or equal (&gt;)
        /// </summary>
        GreaterOrEqual = IsCompareOperator | OpLevel10 | 5,
        /// <summary>
        /// C-like difference operator !=.
        /// </summary>
        Different = IsCompareOperator | OpLevel09 | 6,
        /// <summary>
        /// Strict equality is ===.
        /// </summary>
        StrictEqual = IsCompareOperator | OpLevel09 | 7,
        /// <summary>
        /// Strict difference is !===.
        /// </summary>
        StrictDifferent = IsCompareOperator | OpLevel09 | 8,
        #endregion

        LogicalCount = 2,
        #region IsLogical: && and ||
        /// <summary>
        /// Two pipes (||) logical or operator.
        /// </summary>
        Or = IsLogical | OpLevel04 | 1,
        /// <summary>
        /// Two ampersands (&amp;&amp;) logical and operator.
        /// </summary>
        And = IsLogical | OpLevel05 | 2,
        #endregion

        UnaryOperatorCount = 8,
        #region IsUnaryOperator: !, ~, --, ++, Delete, New, TypeOf, Void.
        /// <summary>
        /// Unary (!). Logical not operator.
        /// </summary>
        Not = IsUnaryOperator | OpLevel14 | 1,

        /// <summary>
        /// Single tilde ~ bitwise unary NOT operator.
        /// </summary>
        BitwiseNot = IsUnaryOperator | OpLevel14 | 2,

        /// <summary>
        /// Double minus character (--) (pre/postfix decrement).
        /// </summary>
        MinusMinus = IsUnaryOperator | OpLevel14 | 3,

        /// <summary>
        /// Double plus character (++) (pre/postfix increment).
        /// </summary>
        PlusPlus = IsUnaryOperator | OpLevel14 | 4,

        Delete = IsUnaryOperator | OpLevel14 | 5,
        New = IsUnaryOperator | OpLevel14 | 6,
        TypeOf = IsUnaryOperator | OpLevel14 | 7,
        Void = IsUnaryOperator | OpLevel14 | 8,

        #endregion

        /// <summary>
        /// String token.
        /// </summary>
        String = IsString | 1,

        /// <summary>
        /// A float number.
        /// </summary>
        Float = IsNumber | 1,
        /// <summary>
        /// Integer number.
        /// </summary>
        Integer = IsNumber | 2,
        /// <summary>
        /// Integer expressed in hexa.
        /// </summary>
        HexNumber = Integer | 4,

        /// <summary>
        /// The NaN identifier.
        /// </summary>
        NaN = Float | 8,
        /// <summary>
        /// The Infinity identifier.
        /// </summary>
        Infinity = Float | 16,

        /// <summary>
        /// Identifier token.
        /// </summary>
        Identifier = IsIdentifier | 1,
        
        /// <summary>
        /// Star comment: /*...*/
        /// </summary>
        StarComment = IsComment | 1,
        /// <summary>
        /// Line comment: //... 
        /// </summary>
        LineComment = IsComment | 2,

        Regex = IsRegex | 1,

        Dot = IsPunctuation | OpLevel15 | 1,
        Comma = IsPunctuation | OpLevel01 | 2,
        QuestionMark = IsPunctuation | OpLevel03 | 3,
        Colon = IsPunctuation | 4,
        SemiColon = IsPunctuation | 5,

        RoundBracket = IsBracket | 1,
        SquareBracket = IsBracket | 2,
        CurlyBracket = IsBracket | 4,
        OpenBracket = IsBracket | 8,
        CloseBracket = IsBracket | 16,

        OpenPar = RoundBracket | OpenBracket | OpLevel15,
        ClosePar = RoundBracket | CloseBracket,
        OpenCurly = CurlyBracket | OpenBracket,
        CloseCurly = CurlyBracket | CloseBracket,
        OpenSquare = SquareBracket | OpenBracket | OpLevel15,
        CloseSquare = SquareBracket | CloseBracket,

    }

}
