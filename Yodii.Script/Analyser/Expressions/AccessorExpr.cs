#region LGPL License
/*----------------------------------------------------------------------------
* This file (Yodii.Script\Analyser\Expressions\AccessorExpr.cs) is part of CiviKey. 
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
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CK.Core;

namespace Yodii.Script
{

    /// <summary>
    /// There are 4 type of concrete Accessors: <see cref="AccessorMemberExpr"/> for member access, <see cref="AccessorIndexerExpr"/>
    /// that handles brackets with one and only one [expression], <see cref="AccessorCallExpr"/> that handles calls with parens that 
    /// contain zero or more arguments and <see cref="AccessorDeclVarExpr"/> that is the definition of a variable.
    /// </summary>
    public abstract class AccessorExpr : Expr
    {
        protected AccessorExpr( SourceLocation location, Expr left, bool isBreakable )
            : base( location, isBreakable )
        {
            Left = left;
        }

        /// <summary>
        /// Gets the left expression.
        /// It can be null: accessor chains are defined with other AccessorExpr and null signals an access to the context.
        /// </summary>
        public Expr Left { get; private set; }

        /// <summary>
        /// Gets whether this accessor is a member name: only <see cref="AccessorMemberExpr"/>
        /// overrides this to be able to return true if the name matches.
        /// </summary>
        /// <param name="memberName">Member name to challenge.</param>
        /// <returns>True if this is an AccessorMemberExpr with the given name.</returns>
        public virtual bool IsMember( string memberName )
        {
            return false;
        }

        /// <summary>
        /// Gets the argument list: null for <see cref="AccessorMemberExpr"/> and <see cref="AccessorDeclVarExpr"/> (a member or field is not callable).
        /// </summary>
        public virtual IReadOnlyList<Expr> Arguments
        {
            get { return null; }
        }
    }

    public class AccessorMemberExpr : AccessorExpr
    {
        /// <summary>
        /// Creates a new <see cref="AccessorMemberExpr"/> for a field or a variable.
        /// </summary>
        /// <param name="left">Left scope. Can be null for unbound reference.</param>
        /// <param name="fieldOrVariableName">Field, variable or function name.</param>
        public AccessorMemberExpr( SourceLocation location, Expr left, string fieldOrVariableName )
            : base( location, left, false )
        {
            Name = fieldOrVariableName;
        }

        public string Name { get; private set; }

        public bool IsUnbound { get { return Left == null; } }

        public override bool IsMember( string memberName )
        {
            return memberName == Name;
        }

        [DebuggerStepThrough]
        internal protected override T Accept<T>( IExprVisitor<T> visitor )
        {
            return visitor.Visit( this );
        }

        public override string ToString()
        {
            return Left == null ? Name : Left.ToString() + '.' + Name;
        }

    }

    public class AccessorDeclVarExpr : AccessorExpr
    {
        public AccessorDeclVarExpr( SourceLocation location, string name )
            : base( location, null, false )
        {
            if( name == null ) throw new ArgumentNullException();
            Name = name;
        }

        public string Name { get; private set; }

        [DebuggerStepThrough]
        internal protected override T Accept<T>( IExprVisitor<T> visitor )
        {
            return visitor.Visit( this );
        }

        public override string ToString()
        {
            return "var " + Name;
        }
    }

    public class AccessorIndexerExpr : AccessorExpr
    {
        CKReadOnlyListMono<Expr> _args;

        /// <summary>
        /// Creates a new <see cref="AccessorIndexerExpr"/>. 
        /// One [Expr] index is enough.
        /// </summary>
        /// <param name="left">Left scope. Must not be null.</param>
        /// <param name="index">Index for the indexer.</param>
        public AccessorIndexerExpr( SourceLocation location, Expr left, Expr index )
            : base( location, left, true )
        {
            _args = new CKReadOnlyListMono<Expr>( index );
        }

        /// <summary>
        /// Gets the expression of the index.
        /// </summary>
        public Expr Index { get { return _args[0]; } }

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

        public override string ToString()
        {
            return Left.ToString() + '[' + Index.ToString() + ']';
        }

    }

    public class AccessorCallExpr : AccessorExpr
    {
        IReadOnlyList<Expr> _args;

        /// <summary>
        /// Creates a new <see cref="AccessorCallExpr"/>: 0 or n arguments can be provided.
        /// </summary>
        /// <param name="left">Left scope. Must not be null.</param>
        /// <param name="arguments">When null, it is normalized to an empty list.</param>
        public AccessorCallExpr( SourceLocation location, Expr left, IReadOnlyList<Expr> arguments = null )
            : base( location, left, true )
        {
            _args = arguments ?? CKReadOnlyListEmpty<Expr>.Empty;
        }

        public override IReadOnlyList<Expr> Arguments { get { return _args; } }

        [DebuggerStepThrough]
        internal protected override T Accept<T>( IExprVisitor<T> visitor )
        {
            return visitor.Visit( this );
        }

        public override string ToString()
        {
            StringBuilder b = new StringBuilder( Left.ToString() );
            b.Append( '(' );
            bool first = true;
            foreach( var e in Arguments )
            {
                if( first ) first = false;
                else b.Append( ',' );
                b.Append( e.ToString() );
            }
            b.Append( ')' );
            return b.ToString();
        }
    }

}
