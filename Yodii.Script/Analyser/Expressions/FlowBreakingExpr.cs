#region LGPL License
/*----------------------------------------------------------------------------
* This file (Yodii.Script\Analyser\Expressions\FlowBreakingExpr.cs) is part of Yodii-Script. 
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
using System.Linq.Expressions;

using System.Diagnostics;

namespace Yodii.Script
{

    public class FlowBreakingExpr : Expr
    {
        public enum BreakingType
        {
            None,
            Break,
            Continue,
            Throw,
            Return
        }

        /// <summary>
        /// Initializes a new <see cref="BreakingType.Break"/> or <see cref="BreakingType.Continue"/>.
        /// </summary>
        /// <param name="location">Source location.</param>
        /// <param name="isContinue">True for Continue, false for Break.</param>
        public FlowBreakingExpr( SourceLocation location, bool isContinue )
            : base( location, true, true )
        {
            Type = isContinue ? BreakingType.Continue : BreakingType.Break;
        }

        /// <summary>
        /// Initializes a new <see cref="BreakingType.Return"/> or <see cref="BreakingType.Throw"/>.
        /// </summary>
        /// <param name="location">Source location.</param>
        /// <param name="returnValue">Returned or thrown value. Must not be null (<see cref="NopExpr"/> must be used for return without value).</param>
        /// <param name="isThrow">True for Throw, false for Return.</param>
        public FlowBreakingExpr( SourceLocation location, Expr returnValue, bool isThrow )
            : base( location, true, true )
        {
            if( returnValue == null ) throw new ArgumentNullException( "returnValue" );
            Type = isThrow ? BreakingType.Throw : BreakingType.Return;
            ReturnedValue = returnValue;
        }

        public FlowBreakingExpr( SourceLocation location, BreakingType type, Expr returnValue )
            : base( location, true, true )
        {
            if( type == BreakingType.None ) throw new ArgumentNullException( "type" );
            if( (type == BreakingType.Return || type == BreakingType.Throw) && returnValue == null ) throw new ArgumentNullException( "returnValue" );
            Type = type;
            ReturnedValue = returnValue;
        }

        /// <summary>
        /// Gets whether this is a return, a break or a continue statement.
        /// </summary>
        public BreakingType Type { get; private set; }

        /// <summary>
        /// Gets the parameter expression. Applies to <see cref="BreakingType.Return"/> and <see cref="BreakingType.Throw"/>.
        /// </summary>
        public Expr ReturnedValue { get; private set; }

        /// <summary>
        /// Parametrized implementation of the visitor's double dispatch.
        /// </summary>
        /// <typeparam name="T">Type of the visitor's returned data.</typeparam>
        /// <param name="visitor">visitor.</param>
        /// <returns>The result of the visit.</returns>
        [DebuggerStepThrough]
        internal protected override T Accept<T>( IExprVisitor<T> visitor )
        {
            return visitor.Visit( this );
        }

        public override string ToString()
        {
            string p;
            switch( Type )
            {
                case BreakingType.Break: p = "break"; break;
                case BreakingType.Continue: p = "continue"; break;
                case BreakingType.Throw: p = "throw"; break;
                default: p = "return"; break;
            }
            if( ReturnedValue != null ) p += ' ' + ReturnedValue.ToString();
            return p + ';';
        }
    }


}
