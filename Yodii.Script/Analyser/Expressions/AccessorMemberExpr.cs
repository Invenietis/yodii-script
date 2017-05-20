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
    /// <summary>
    /// Member accessor.
    /// </summary>
    public class AccessorMemberExpr : AccessorExpr
    {
        /// <summary>
        /// Creates a new <see cref="AccessorMemberExpr"/> for a field or a variable.
        /// </summary>
        /// <param name="location">Source location.</param>
        /// <param name="left">Left scope. Can be null for unbound reference.</param>
        /// <param name="fieldOrVariableName">Field, variable or function name.</param>
        /// <param name="isStatement">True for statement, false for expression.</param>
        public AccessorMemberExpr( SourceLocation location, Expr left, string fieldOrVariableName, bool isStatement )
            : base( location, left, isStatement, false )
        {
            Name = fieldOrVariableName;
            var mLeft = left as AccessorMemberExpr;
            MemberFullName = mLeft != null ? mLeft.MemberFullName + '.' + fieldOrVariableName : fieldOrVariableName;
        }

        /// <summary>
        /// Gets the name of this member.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the full name of this member: this is the <see cref="Name"/> or
        /// the dotted separated names of all other AccessorMemberExpr on the <see cref="AccessorExpr.Left"/>.
        /// </summary>
        public string MemberFullName { get; }

        /// <summary>
        /// Gets whether this member is unbound: its <see cref="AccessorExpr.Left"/> is null.
        /// An unbound accessor can be only resolved by the global context.
        /// </summary>
        public bool IsUnbound => Left == null;

        /// <summary>
        /// True is this <see cref="Name"/> matches.
        /// </summary>
        /// <param name="memberName">Member name to match.</param>
        /// <returns>True on success, false otherwise.</returns>
        public override bool IsMember( string memberName ) => memberName == Name;

        /// <summary>
        /// Parametrized implementation of the visitor's double dispatch.
        /// </summary>
        /// <typeparam name="T">Type of the visitor's returned data.</typeparam>
        /// <param name="visitor">visitor.</param>
        /// <returns>The result of the visit.</returns>
        [DebuggerStepThrough]
        internal protected override T Accept<T>( IExprVisitor<T> visitor ) => visitor.Visit( this );

        /// <summary>
        /// This is just to ease debugging: the whole <see cref="AccessorExpr.Left"/> chain of accessor 
        /// is displayed.
        /// </summary>
        /// <returns>No more than a human readable expression.</returns>
        public override string ToString() => Left == null ? Name : Left.ToString() + '.' + Name;

    }
}
