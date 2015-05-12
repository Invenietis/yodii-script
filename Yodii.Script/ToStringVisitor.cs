#region LGPL License
/*----------------------------------------------------------------------------
* This file (Yodii.Script\ToStringVisitor.cs) is part of Yodii-Script. 
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
using System.Diagnostics;

namespace Yodii.Script
{
    public class ToStringVisitor : ExprVisitor
    {
        static public readonly string DefaultExprPrefix = "‹";
        static public readonly string DefaultExprSuffix = "›";
        
        StringBuilder _b;
        string _exprPrefix;
        string _exprSuffix;

        public ToStringVisitor( StringBuilder b = null, string exprPrefix = null, string exprSuffix = null )
        {
            _exprPrefix = exprPrefix ?? DefaultExprPrefix;
            _exprSuffix = exprSuffix ?? DefaultExprSuffix;
            _b = b ?? new StringBuilder();
        }

        static public string ToString( Expr e, string exprPrefix = null, string exprSuffix = null )
        {
            var v = new ToStringVisitor( new StringBuilder(), exprPrefix, exprSuffix );
            v.VisitExpr( e );
            return v.ToString();
        }

        public override Expr Visit( AccessorMemberExpr e )
        {
            _b.Append( _exprPrefix );
            if( e.IsUnbound )
            {
                _b.Append( "[[Global]]" );
            }
            else VisitExpr( e.Left );
            _b.Append( '.' ).Append( e.Name );
            _b.Append( _exprSuffix );
            return e;
        }

        public override Expr Visit( AccessorIndexerExpr e )
        {
            _b.Append( _exprPrefix );
            VisitExpr( e.Left );
            _b.Append( '[' );
            VisitExpr( e.Index );
            _b.Append( ']' );
            _b.Append( _exprSuffix );
            return e;
        }

        public override Expr Visit( AccessorCallExpr e )
        {
            _b.Append( _exprPrefix );
            VisitExpr( e.Left );
            _b.Append( '(' );
            e.Arguments.Select( ( p, i ) => 
            { 
                if( i > 0 ) _b.Append( ',' ); 
                return VisitExpr( p ); 
            }).LastOrDefault();
            _b.Append( ')' );
            _b.Append( _exprSuffix );
            return e;
        }

        public override Expr Visit( BinaryExpr e )
        {
            _b.Append( _exprPrefix );
            VisitExpr( e.Left );
            _b.Append( JSTokenizer.Explain( e.BinaryOperatorToken ) );
            VisitExpr( e.Right );
            _b.Append( _exprSuffix );
            return e;
        }

        public override Expr Visit( ConstantExpr e )
        {
            _b.Append( _exprPrefix );
            _b.Append( e.Value );
            _b.Append( _exprSuffix );
            return e;
        }

        public override Expr Visit( IfExpr e )
        {
            _b.Append( _exprPrefix );
            if( !e.IsTernaryOperator ) _b.Append( "if" );
            VisitExpr( e.Condition );
            if( e.IsTernaryOperator ) _b.Append( '?' );
            VisitExpr( e.WhenTrue );
            if( e.IsTernaryOperator )
            {
                _b.Append( ':' );
                VisitExpr( e.WhenFalse );
            }
            else if( e.WhenFalse != null )
            {
                _b.Append( "else" );
                VisitExpr( e.WhenFalse );
            }
            _b.Append( _exprSuffix );
            return e;
        }

        public override Expr Visit( UnaryExpr e )
        {
            _b.Append( _exprPrefix );
            _b.Append( JSTokenizer.Explain( e.TokenType ) );
            _b.Append( ' ' );
            VisitExpr( e.Expression );
            _b.Append( _exprSuffix );
            return e;
        }

        public override Expr Visit( SyntaxErrorExpr e )
        {
            _b.Append( _exprPrefix );
            _b.AppendFormat( "Syntax Error: {0}", e.ErrorMessage );
            _b.Append( _exprSuffix );
            return e;
        }

        public override string ToString()
        {
            return _b.ToString();
        }

    }
}
