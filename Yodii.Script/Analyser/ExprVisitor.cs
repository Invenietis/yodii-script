#region LGPL License
/*----------------------------------------------------------------------------
* This file (Yodii.Script\Analyser\ExprVisitor.cs) is part of Yodii-Script. 
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


namespace Yodii.Script
{
    public class ExprVisitor : IExprVisitor<Expr>
    {
        public virtual Expr VisitExpr( Expr e )
        {
            return e.Accept( this );
        }

        public virtual Expr Visit( AccessorMemberExpr e )
        {
            Expr lV = e.Left != null ? VisitExpr( e.Left ) : null;
            return lV == e.Left ? e : new AccessorMemberExpr( e.Location, lV, e.Name, e.IsStatement );
        }

        public virtual Expr Visit( AccessorCallExpr e )
        {
            var lV = VisitExpr( e.Left );
            var aV = Visit( e.Arguments );
            return lV == e.Left && aV == e.Arguments ? e : new AccessorCallExpr( e.Location, lV, aV, e.DeclaredFunctions, e.IsStatement, e.IsIndexer );
        }

        public IReadOnlyList<Expr> Visit( IReadOnlyList<Expr> multi )
        {
            Expr[] newMulti = null;
            for( int i = 0; i < multi.Count; ++i )
            {
                Expr p = multi[i];
                Expr sp = VisitExpr( p );
                if( newMulti != null ) newMulti[i] = sp;
                else if( p != sp )
                {
                    newMulti = new Expr[multi.Count];
                    int j = i;
                    while( --j >= 0 ) newMulti[j] = multi[j];
                    newMulti[i] = sp;
                }
            }
            if( newMulti != null ) multi = newMulti.ToArray();
            return multi;
        }

        public virtual Expr Visit( BinaryExpr e )
        {
            Expr lV = VisitExpr( e.Left );
            Expr rV = VisitExpr( e.Right );
            return lV == e.Left && rV == e.Right ? e : new BinaryExpr( e.Location, lV, e.BinaryOperatorToken, rV );
        }

        public virtual Expr Visit( ConstantExpr e )
        {
            return e;
        }

        public virtual Expr Visit( IfExpr e )
        {
            Expr cV = VisitExpr( e.Condition );
            Expr tV = VisitExpr( e.WhenTrue );
            Expr fV = e.WhenFalse != null ? VisitExpr( e.WhenFalse ) : null;
            return cV == e.Condition && tV == e.WhenTrue && fV == e.WhenFalse ? e : new IfExpr( e.Location, e.IsTernaryOperator, cV, tV, fV );
        }

        public virtual Expr Visit( UnaryExpr e )
        {
            Expr eV = VisitExpr( e.Expression );
            return eV == e.Expression ? e : new UnaryExpr( e.Location, e.TokenType, eV );
        }

        public virtual Expr Visit( SyntaxErrorExpr e )
        {
            return e;
        }

        public virtual Expr Visit( ListOfExpr e )
        {
            var lV = Visit( e.List );
            return lV == e.List ? e : new ListOfExpr( lV );
        }

        public virtual Expr Visit( BlockExpr e )
        {
            var sV = Visit( e.List );
            var lV = (IReadOnlyList<AccessorLetExpr>)Visit( e.Locals );
            return sV == e.List && lV == e.Locals ? e : new BlockExpr( sV, lV );
        }

        public virtual Expr Visit( AssignExpr e )
        {
            var lV = (AccessorExpr)VisitExpr( e.Left );
            var rV = VisitExpr( e.Right );
            return lV == e.Left && rV == e.Right ? e : new AssignExpr( e.Location, lV, rV );
        }

        public virtual Expr Visit( AccessorLetExpr e )
        {
            return e;
        }

        public virtual Expr Visit( NopExpr e )
        {
            return e;
        }

        public virtual Expr Visit( PrePostIncDecExpr e )
        {
            var oV = VisitExpr( e.Operand );
            return oV == e.Operand ? e : new PrePostIncDecExpr( e.Location, (AccessorExpr)oV, e.Plus, e.Prefix, e.IsStatement );
        }

        public virtual Expr Visit( WhileExpr e )
        {
            var cV = VisitExpr( e.Condition );
            var oV = VisitExpr( e.Code );
            return cV == e.Condition && oV == e.Code ? e : new WhileExpr( e.Location, cV, oV );
        }

        public virtual Expr Visit( ForeachExpr e )
        {
            var vV = (AccessorLetExpr)VisitExpr( e.Variable );
            var gV = VisitExpr( e.Generator );
            var cV = VisitExpr( e.Code );
            return vV == e.Variable && gV == e.Generator && cV == e.Code ? e : new ForeachExpr( e.Location, vV, gV, cV );
        }

        public virtual Expr Visit( FlowBreakingExpr e )
        {
            var rV = e.ReturnedValue != null ? VisitExpr( e.ReturnedValue ) : null;
            return rV == e.ReturnedValue ? e : new FlowBreakingExpr( e.Location, e.Type, rV );
        }

        public virtual Expr Visit( FunctionExpr e )
        {
            var nV = (AccessorLetExpr)(e.Name != null ? Visit( e.Name ) : null);
            var pV = (IReadOnlyList<AccessorLetExpr>)Visit( e.Parameters );
            var bV = VisitExpr( e.Body );
            var cV = (IReadOnlyList<AccessorLetExpr>)Visit( e.Closures );
            return nV == e.Name && pV == e.Parameters && bV == e.Body && cV == e.Closures ? e : new FunctionExpr( e.Location, pV, bV, cV, nV );
        }

        public virtual Expr Visit( TryCatchExpr e )
        {
            var tV = VisitExpr( e.TryExpr );
            var pV = (AccessorLetExpr)Visit( e.ExceptionParameter );
            var cV = VisitExpr( e.CatchExpr );
            return tV == e.TryExpr && pV == e.ExceptionParameter && cV == e.CatchExpr ? e : new TryCatchExpr( e.Location, tV, pV, cV );
        }

        public virtual Expr Visit( WithExpr e )
        {
            var oV = VisitExpr( e.Obj );
            var cV = VisitExpr( e.Code );
            return oV == e.Obj && cV == e.Code ? e : new WithExpr( e.Location, oV, cV );
        }

    }

}
