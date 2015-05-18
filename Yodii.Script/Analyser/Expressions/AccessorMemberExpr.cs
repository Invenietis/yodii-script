#region LGPL License
/*----------------------------------------------------------------------------
* This file (Yodii.Script\Analyser\Expressions\AccessorMemberExpr.cs) is part of Yodii-Script. 
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
    public class AccessorMemberExpr : AccessorExpr
    {
        /// <summary>
        /// Creates a new <see cref="AccessorMemberExpr"/> for a field or a variable.
        /// </summary>
        /// <param name="left">Left scope. Can be null for unbound reference.</param>
        /// <param name="fieldOrVariableName">Field, variable or function name.</param>
        public AccessorMemberExpr( SourceLocation location, Expr left, string fieldOrVariableName, bool isStatement )
            : base( location, left, isStatement, false )
        {
            Name = fieldOrVariableName;
        }

        public string Name { get; private set; }

        public bool IsUnbound { get { return Left == null; } }

        public override bool IsMember( string memberName )
        {
            return memberName == Name;
        }

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
            return Left == null ? Name : Left.ToString() + '.' + Name;
        }

    }
}
