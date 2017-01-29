#region LGPL License
/*----------------------------------------------------------------------------
* This file (Yodii.Script\Tokenizer\JSTokeniserError.cs) is part of Yodii-Script. 
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

namespace Yodii.Script
{
    public enum TokenizerError
    {
        None = 0,

        /// <summary>
        /// Sign bit (bit n°31) is 1 to indicate an error 
        /// or the end of the input.
        /// This allows easy and efficient error/end test: any negative token value marks the end.
        /// </summary>
        IsErrorOrEndOfInput = -2147483648,

        /// <summary>
        /// The end of input (the two most sgnificant bit set).
        /// </summary>
        EndOfInput = IsErrorOrEndOfInput | (1 << 30),
        /// <summary>
        /// Error mask.
        /// </summary>
        ErrorMask = IsErrorOrEndOfInput | (1 << 29),

        /// <summary>
        /// Invalid character.
        /// </summary>
        ErrorInvalidChar = ErrorMask | 1,

        /// <summary>
        /// Error string mask.
        /// </summary>
        ErrorStringMask = ErrorMask | (1 << 28),

        /// <summary>
        /// Error number mask.
        /// </summary>
        ErrorNumberMask = ErrorMask | (1 << 27),

        /// <summary>
        /// Error regex mask.
        /// </summary>
        ErrorRegexMask = ErrorMask | (1 << 26),

        /// <summary>
        /// Whenever a non terminated string is encountered.
        /// </summary>
        ErrorStringUnterminated = ErrorStringMask | 1,
        /// <summary>
        /// Bad Unicode value embedded in a string.
        /// </summary>
        ErrorStringEmbeddedUnicodeValue = ErrorStringMask | 2,
        /// <summary>
        /// Bad hexadecimal value embedded in a string.
        /// </summary>
        ErrorStringEmbeddedHexaValue = ErrorStringMask | 4,
        /// <summary>
        /// Line continuation \ followed by a \r without \n after it.
        /// </summary>
        ErrorStringUnexpectedCRInLineContinuation = ErrorStringMask | 8,

        /// <summary>
        /// Unterminated number.
        /// </summary>
        ErrorNumberUnterminatedValue = ErrorNumberMask | 1,
        /// <summary>
        /// Invalid number value.
        /// </summary>
        ErrorNumberValue = ErrorNumberMask | 2,
        /// <summary>
        /// Number value is immediately followed by an identifier: 45D for example.
        /// </summary>
        ErrorNumberIdentifierStartsImmediately = ErrorNumberMask | 4,

        /// <summary>
        /// Whenever a non terminated regular expression.
        /// </summary>
        ErrorRegexUnterminated = ErrorRegexMask | 1,
    }
}
