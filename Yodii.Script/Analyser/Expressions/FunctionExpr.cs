#region LGPL License
/*----------------------------------------------------------------------------
* This file (Yodii.Script\Analyser\Expressions\FunctionExpr.cs) is part of CiviKey. 
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

    public class FunctionExpr : Expr
    {
        public FunctionExpr( SourceLocation location, IReadOnlyList<AccessorDeclVarExpr> parameters, Expr body, IReadOnlyList<AccessorDeclVarExpr> closures, AccessorDeclVarExpr name = null )
            : base( location, false )
        {
            if( parameters == null ) throw new ArgumentNullException();
            if( body == null ) throw new ArgumentNullException();
            if( closures == null ) throw new ArgumentNullException();
            Parameters = parameters;
            Name = name;
            Body = body;
            Closures = closures;
        }

        public Expr Body { get; private set; }

        public AccessorDeclVarExpr Name { get; private set; }

        public IReadOnlyList<AccessorDeclVarExpr> Parameters { get; private set; }

        public IReadOnlyList<AccessorDeclVarExpr> Closures { get; private set; }

        [DebuggerStepThrough]
        internal protected override T Accept<T>( IExprVisitor<T> visitor )
        {
            return visitor.Visit( this );
        }

        public override string ToString()
        {
            string r = "function";
            if( Name != null ) r += ' ' + Name.Name;
            r += '(' + String.Join( ", ", Parameters.Select( e => e.Name ) ) + ')';
            return r + Body.ToString();
        }

    }

}
