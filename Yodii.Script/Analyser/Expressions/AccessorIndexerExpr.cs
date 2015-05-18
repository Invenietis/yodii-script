#region LGPL License
/*----------------------------------------------------------------------------
* This file (Yodii.Script\Analyser\Expressions\AccessorIndexerExpr.cs) is part of Yodii-Script. 
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
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Yodii.Script
{
    /// <summary>
    /// Accessor that captures an indexed access.
    /// </summary>
    public class AccessorIndexerExpr : AccessorExpr
    {
        Expr[] _args;

        /// <summary>
        /// Creates a new <see cref="AccessorIndexerExpr"/>. 
        /// One [Expr] index is enough.
        /// </summary>
        /// <param name="location">Location of this expression.</param>
        /// <param name="left">Left scope. Must not be null.</param>
        /// <param name="index">Index for the indexer.</param>
        public AccessorIndexerExpr( SourceLocation location, Expr left, Expr index, bool isStatement )
            : base( location, left, isStatement, true )
        {
            _args = new Expr[]{ index };
        }

        /// <summary>
        /// Gets the expression of the index.
        /// </summary>
        public Expr Index { get { return _args[0]; } }

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
        /// Gets a one-sized argument list that contains the <see cref="Index"/>.
        /// </summary>
        public override IReadOnlyList<Expr> Arguments
        {
            get { return _args; }
        }

        /// <summary>
        /// This is just to ease debugging.
        /// </summary>
        /// <returns>Readable expression.</returns>
        public override string ToString()
        {
            return Left.ToString() + '[' + Index.ToString() + ']';
        }

    }

}
