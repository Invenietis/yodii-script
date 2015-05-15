#region LGPL License
/*----------------------------------------------------------------------------
* This file (Yodii.Script\Analyser\Expressions\WhileExpr.cs) is part of Yodii-Script. 
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

    public class WhileExpr : Expr
    {
        public WhileExpr( SourceLocation location, Expr condition, Expr code )
            : this( location, false, condition, code )
        {
        }

        public WhileExpr( SourceLocation location, bool doWhile, Expr condition, Expr code )
            : base( location, true, false )
        {
            Condition = condition;
            Code = code;
            DoWhile = doWhile;
        }

        public bool DoWhile { get; private set; }

        public Expr Condition { get; private set; }

        public Expr Code { get; private set; }

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
            return "while(" + Condition.ToString() + ") {" + Code.ToString() + "}";
        }
    }


}
