#region LGPL License
/*----------------------------------------------------------------------------
* This file (Yodii.Script\EvalVisitor\EvalVisitor.Expr.Unary.cs) is part of Yodii-Script. 
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

using System.Collections.ObjectModel;

namespace Yodii.Script
{
    internal partial class EvalVisitor
    {
        class UnaryExprFrame : Frame<UnaryExpr>
        {
            PExpr _expression;

            public UnaryExprFrame( EvalVisitor evaluator, UnaryExpr e )
                : base( evaluator, e )
            {
            }

            protected override PExpr DoVisit()
            {
                if( IsPendingOrSignal( ref _expression, Expr.Expression ) ) return PendingOrSignal( _expression );

                RuntimeObj result = _expression.Result;
                // Minus and Plus are classified as a binary operator.
                // Handle those special cases here.
                if( Expr.TokenType == JSTokenizerToken.Minus )
                {
                    result = Global.CreateNumber( -result.ToDouble() );
                }
                else if( Expr.TokenType == JSTokenizerToken.Plus )
                {
                    result = Global.CreateNumber( result.ToDouble() );
                }
                else
                {
                    switch( (int)Expr.TokenType & 15 )
                    {
                        case (int)JSTokenizerToken.Not & 15:
                            {
                                result = Global.CreateBoolean( !result.ToBoolean() );
                                break;
                            }
                        case (int)JSTokenizerToken.BitwiseNot & 15:
                            {
                                result = Global.CreateNumber( ~JSSupport.ToInt64( result.ToDouble() ) );
                                break;
                            }
                        case (int)JSTokenizerToken.TypeOf & 15:
                            {
                                // Well known Javascript bug: typeof null === "object".
                                if( result == RuntimeObj.Null ) result = Global.CreateString( RuntimeObj.TypeObject );
                                else result = Global.CreateString( result.Type );
                                break;
                            }
                        case (int)JSTokenizerToken.Void & 15:
                            {
                                result = RuntimeObj.Undefined;
                                break;
                            }
                        default: throw UnsupportedOperatorException();
                    }
                }
                return SetResult( result );
            }

            NotSupportedException UnsupportedOperatorException()
            {
                string msg = String.Format( "Unsupported unary operator: '{0}' ({1}).", JSTokenizer.Explain( Expr.TokenType ), (int)Expr.TokenType );
                return new NotSupportedException( msg );
            }

        }


        public PExpr Visit( UnaryExpr e )
        {
            return Run( new UnaryExprFrame( this, e ) );
        }
    }
}
