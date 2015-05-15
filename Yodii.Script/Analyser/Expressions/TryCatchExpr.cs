#region LGPL License
/*----------------------------------------------------------------------------
* This file (Yodii.Script\Analyser\Expressions\IfExpr.cs) is part of Yodii-Script. 
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

    public class TryCatchExpr : Expr
    {
        public TryCatchExpr( SourceLocation location, Expr tryExpr, AccessorLetExpr exceptionParameter, Expr catchExpr )
            : base( location, true, true )
        {
            if( tryExpr == null ) throw new ArgumentException( "tryExpr" );
            if( catchExpr == null ) throw new ArgumentNullException( "catchExpr" );
            TryExpr = tryExpr;
            ExceptionParameter = exceptionParameter;
            CatchExpr = catchExpr;
        }

        /// <summary>
        /// Gets the try expression.
        /// </summary>
        public Expr TryExpr { get; private set; }

        /// <summary>
        /// Gets the exception parameter. Can be null.
        /// </summary>
        public AccessorLetExpr ExceptionParameter { get; private set; }

        /// <summary>
        /// Gets the catch expression.
        /// </summary>
        public Expr CatchExpr { get; private set; }

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

        /// <summary>
        /// This is just to ease debugging.
        /// </summary>
        /// <returns>Readable expression.</returns>
        public override string ToString()
        {
            string s = "[try " + TryExpr.ToString() + " catch " + CatchExpr.ToString() + "]";
            return s;
        }
    }


}
