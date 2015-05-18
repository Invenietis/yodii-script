#region LGPL License
/*----------------------------------------------------------------------------
* This file (Yodii.Script\EvalVisitor\RuntimeError.cs) is part of Yodii-Script. 
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
    public class RuntimeError : RuntimeSignal
    {
        /// <summary>
        /// Initializes a new syntax error.
        /// </summary>
        /// <param name="culprit">The expression. Can not be null.</param>
        /// <param name="syntaxErrorMessage">A message describing the syntax error. Can not be null.</param>
        public RuntimeError( Expr culprit, string syntaxErrorMessage )
            : base( culprit )
        {
            if( syntaxErrorMessage == null ) throw new ArgumentNullException();
            Message = syntaxErrorMessage;
        }

        /// <summary>
        /// Initializes a new runtime error.
        /// </summary>
        /// <param name="culprit">The expression. Can not be null.</param>
        /// <param name="thrownValue">The error value. Can not be null.</param>
        public RuntimeError( Expr culprit, RuntimeObj thrownValue )
            : base( culprit )
        {
            if( thrownValue == null ) throw new ArgumentNullException();
            if( thrownValue is RefRuntimeObj ) throw new ArgumentException();
            Message = "Runtime error.";
            ThrownValue = thrownValue;
        }

        /// <summary>
        /// Gets whether this is a syntax error.
        /// Syntax errors are not recoverables (they are not <see cref="IsCatchable"/>).
        /// </summary>
        public bool IsSyntaxError
        {
            get { return ThrownValue == null; }
        }

        /// <summary>
        /// Gets whether this error can be caught.
        /// </summary>
        public bool IsCatchable
        {
            get { return ThrownValue != null; }
        }

        /// <summary>
        /// Gets the message for syntax error. 
        /// This is "Runtime Error" when <see cref="ThrownValue"/> is not null.
        /// </summary>
        public string Message { get; private set; }

        /// <summary>
        /// Gets the thrown value. Null when <see cref="IsSyntaxError"/> is true.
        /// </summary>
        public RuntimeObj ThrownValue { get; private set; }

        public override PExpr Visit( IAccessorFrame frame )
        {
            if( frame.Expr.IsMember( "message" ) ) return frame.SetResult( frame.Global.CreateString( Message ) );
            return frame.SetError();
        }

        public override string ToString()
        {
            return IsSyntaxError 
                    ? String.Format( "Syntax Error: {0} at {1}.", Message, Expr.Location.ToString() )
                    : String.Format( "Runtime Error at {0}. Error: {1}", Expr.Location.ToString(), ThrownValue.ToString() );
        }
    }

}
