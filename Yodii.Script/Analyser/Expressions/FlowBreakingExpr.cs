#region LGPL License
/*----------------------------------------------------------------------------
* This file (Yodii.Script\Analyser\Expressions\FlowBreakingExpr.cs) is part of CiviKey. 
*  
* CiviKey is free software: you can redistribute it and/or modify 
* it under the terms of the GNU Lesser General Public License as published 
* by the Free Software Foundation, either version 3 of the License, or 
* (at your option) any later version. 
*  
* CiviKey is distributed in the hope that it will be useful, 
* but WITHOUT ANY WARRANTY; without even the implied warranty of
* MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the 
* GNU Lesser General Public License for more details. 
* You should have received a copy of the GNU Lesser General Public License 
* along with CiviKey.  If not, see <http://www.gnu.org/licenses/>. 
*  
* Copyright © 2007-2015, 
*     Invenietis <http://www.invenietis.com>,
* All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;
using CK.Core;
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
            Return
        }

        public FlowBreakingExpr( SourceLocation location, bool isContinue )
            : base( location, true )
        {
            Type = isContinue ? BreakingType.Continue : BreakingType.Break;
        }

        public FlowBreakingExpr( SourceLocation location, Expr returnValue )
            : base( location, true )
        {
            if( returnValue == null ) throw new ArgumentNullException( "returnValue" );
            Type = BreakingType.Return;
            ReturnedValue = returnValue;
        }

        public FlowBreakingExpr( SourceLocation location, BreakingType type, Expr returnValue )
            : base( location, true )
        {
            if( type == BreakingType.None ) throw new ArgumentNullException( "type" );
            if( type == BreakingType.Return && returnValue == null ) throw new ArgumentNullException( "returnValue" );
            Type = type;
            ReturnedValue = returnValue;
        }

        /// <summary>
        /// Gets whether this is a return, a break or a continue statement.
        /// </summary>
        public BreakingType Type { get; private set; }

        /// <summary>
        /// Gets the parameter exprssion. Currently makes senses only for <see cref="BreakingType.Return"/>.
        /// </summary>
        public Expr ReturnedValue { get; private set; }

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
                default: p = "return"; break;
            }
            if( ReturnedValue != null ) p += ' ' + ReturnedValue.ToString();
            return p + ';';
        }
    }


}
