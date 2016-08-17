#region LGPL License
/*----------------------------------------------------------------------------
* This file (Yodii.Script\Analyser\Expressions\AccessorExpr.cs) is part of Yodii-Script. 
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
    /// There are 4 type of concrete Accessors: <see cref="AccessorMemberExpr"/> for member access, 
    /// <see cref="AccessorCallExpr"/> that handles calls with parens (or square brackets: this handles indexer
    /// as well as actual calls) that contain zero or more arguments and <see cref="AccessorLetExpr"/> 
    /// that is the definition of a variable.
    /// </summary>
    public abstract class AccessorExpr : Expr
    {
        /// <summary>
        /// Initializes a new <see cref="AccessorExpr"/>.
        /// </summary>
        /// <param name="location">Location of this expression.</param>
        /// <param name="left">Left access.</param>
        /// <param name="isStatement">True if this expression is a statement.</param>
        /// <param name="isBreakable">True to allow breaking on this type of expession.</param>
        protected AccessorExpr( SourceLocation location, Expr left, bool isStatement, bool isBreakable )
            : base( location, isStatement, isBreakable )
        {
            Left = left;
        }

        /// <summary>
        /// Gets the left expression.
        /// It can be null: accessor chains are defined with other AccessorExpr and null signals an access to the context.
        /// </summary>
        public Expr Left { get; }

        /// <summary>
        /// Gets whether this accessor is a member name: only <see cref="AccessorMemberExpr"/>
        /// overrides this to be able to return true if the name matches.
        /// </summary>
        /// <param name="memberName">Member name to challenge.</param>
        /// <returns>True if this is an AccessorMemberExpr with the given name.</returns>
        public virtual bool IsMember( string memberName ) => false;

        /// <summary>
        /// Gets the argument list: null for <see cref="AccessorMemberExpr"/> and <see cref="AccessorLetExpr"/> (a member, field 
        /// or variable declaration is not callable).
        /// </summary>
        public virtual IReadOnlyList<Expr> Arguments => null; 
    }

}
