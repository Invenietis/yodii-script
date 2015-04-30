#region LGPL License
/*----------------------------------------------------------------------------
* This file (Yodii.Script\Analyser\Expressions\Expr.cs) is part of CiviKey. 
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
using System.Threading.Tasks;

namespace Yodii.Script
{
    /// <summary>
    /// Abstract base class of <see cref="ExprAnalysis"/> production.
    /// </summary>
    public abstract class Expr
    {
        /// <summary>
        /// Empty array of <see cref="Expr"/>.
        /// </summary>
        public static readonly Expr[] EmptyArray = new Expr[0];

        /// <summary>
        /// Initializes a new <see cref="Expr"/>.
        /// </summary>
        /// <param name="location">Location of this expression.</param>
        /// <param name="isbreakable">True to allow breaking on this type of expession.</param>
        protected Expr( SourceLocation location, bool isbreakable = false )
        {
            Location = location;
            IsBreakable = isbreakable;
        }

        /// <summary>
        /// Gets whether this expression is breakable.
        /// </summary>
        public readonly bool IsBreakable;

        /// <summary>
        /// Gets the location of this expression.
        /// </summary>
        public readonly SourceLocation Location;

        /// <summary>
        /// Parametrized implementation of the visitor's double dispatch.
        /// </summary>
        /// <typeparam name="T">Type of the visitor's returned data.</typeparam>
        /// <param name="visitor">visitor.</param>
        /// <returns>The result of the visit.</returns>
        internal protected abstract T Accept<T>( IExprVisitor<T> visitor );
    }
}
