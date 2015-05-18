#region LGPL License
/*----------------------------------------------------------------------------
* This file (Yodii.Script\EvalVisitor\EvalVisitor.Expr.Binary.cs) is part of Yodii-Script. 
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
        class BinaryExprFrame : Frame<BinaryExpr>
        {
            PExpr _left;
            PExpr _right;
            
            public BinaryExprFrame( EvalVisitor evaluator, BinaryExpr e )
                : base( evaluator, e )
            {
            }

            protected override PExpr DoVisit()
            {
                if( IsPendingOrSignal( ref _left, Expr.Left ) ) return PendingOrSignal( _left );

                // Do not evaluate right expression if it is useless: short-circuit boolean evaluation.
                if( (Expr.BinaryOperatorToken == JSTokenizerToken.And && !_left.Result.ToBoolean())
                    || (Expr.BinaryOperatorToken == JSTokenizerToken.Or && _left.Result.ToBoolean()) )
                {
                    return SetResult( _left.Result );
                }

                if( IsPendingOrSignal( ref _right, Expr.Right ) ) return PendingOrSignal( _right );

                RuntimeObj left = _left.Result;
                RuntimeObj right = _right.Result;

                // Right value is the result for And and Or.
                RuntimeObj result = right;

                if( Expr.BinaryOperatorToken != JSTokenizerToken.And && Expr.BinaryOperatorToken != JSTokenizerToken.Or )
                {
                    if( (Expr.BinaryOperatorToken & JSTokenizerToken.IsCompareOperator) != 0 )
                    {
                        #region ==, <, >, <=, >=, !=, === and !==
                        int compareValue;
                        switch( (int)Expr.BinaryOperatorToken & 15 )
                        {
                            case (int)JSTokenizerToken.StrictEqual & 15:
                                {
                                    result = Global.CreateBoolean( new RuntimeObjComparer( left, right ).AreEqualStrict( Global ) );
                                    break;
                                }
                            case (int)JSTokenizerToken.StrictDifferent & 15:
                                {
                                    result = Global.CreateBoolean( !new RuntimeObjComparer( left, right ).AreEqualStrict( Global ) );
                                    break;
                                }
                            case (int)JSTokenizerToken.Greater & 15:
                                {
                                    result = Global.CreateBoolean( new RuntimeObjComparer( left, right ).Compare( Global, out compareValue ) && compareValue > 0 );
                                    break;
                                }
                            case (int)JSTokenizerToken.GreaterOrEqual & 15:
                                {
                                    result = Global.CreateBoolean( new RuntimeObjComparer( left, right ).Compare( Global, out compareValue ) && compareValue >= 0 );
                                    break;
                                }
                            case (int)JSTokenizerToken.Less & 15:
                                {
                                    result = Global.CreateBoolean( new RuntimeObjComparer( left, right ).Compare( Global, out compareValue ) && compareValue < 0 );
                                    break;
                                }
                            case (int)JSTokenizerToken.LessOrEqual & 15:
                                {
                                    result = Global.CreateBoolean( new RuntimeObjComparer( left, right ).Compare( Global, out compareValue ) && compareValue <= 0 );
                                    break;
                                }
                            case (int)JSTokenizerToken.Equal & 15:
                                {
                                    result = Global.CreateBoolean( new RuntimeObjComparer( left, right ).AreEqual( Global ) );
                                    break;
                                }
                            case (int)JSTokenizerToken.Different & 15:
                                {
                                    result = Global.CreateBoolean( !new RuntimeObjComparer( left, right ).AreEqual( Global ) );
                                    break;
                                }
                            default: throw UnsupportedOperatorException();
                        }
                        #endregion
                    }
                    else if( (Expr.BinaryOperatorToken & JSTokenizerToken.IsBinaryOperator) != 0 )
                    {
                        #region |, ^, &, >>, <<, >>>, +, -, /, * and %.
                        switch( (int)Expr.BinaryOperatorToken & 15 )
                        {
                            case (int)JSTokenizerToken.Plus & 15:
                                {
                                    if( ReferenceEquals( left.Type, RuntimeObj.TypeNumber ) && ReferenceEquals( right.Type, RuntimeObj.TypeNumber ) )
                                    {
                                        result = Global.CreateNumber( left.ToDouble() + right.ToDouble() );
                                    }
                                    else
                                    {
                                        result = Global.CreateString( String.Concat( left.ToString(), right.ToString() ) );
                                    }
                                    break;
                                }
                            case (int)JSTokenizerToken.Minus & 15:
                                {
                                    result = Global.CreateNumber( left.ToDouble() - right.ToDouble() );
                                    break;
                                }
                            case (int)JSTokenizerToken.Mult & 15:
                                {
                                    result = Global.CreateNumber( left.ToDouble() * right.ToDouble() );
                                    break;
                                }
                            case (int)JSTokenizerToken.Divide & 15:
                                {
                                    result = Global.CreateNumber( left.ToDouble() / right.ToDouble() );
                                    break;
                                }
                            case (int)JSTokenizerToken.Modulo & 15:
                                {
                                    if( right == JSEvalNumber.Zero || left == JSEvalNumber.NegativeInfinity || left == JSEvalNumber.Infinity )
                                    {
                                        result = JSEvalNumber.NaN;
                                    }
                                    else if( left == JSEvalNumber.NegativeInfinity || left == JSEvalNumber.Infinity )
                                    {
                                        result = right;
                                    }
                                    else
                                    {
                                        result = Global.CreateNumber( left.ToDouble() % right.ToDouble() );
                                    }
                                    break;
                                }
                            case (int)JSTokenizerToken.BitwiseAnd & 15:
                                {
                                    Int64 l = JSSupport.ToInt64( left.ToDouble() );
                                    Int64 rO = JSSupport.ToInt64( right.ToDouble() );
                                    result = Global.CreateNumber( l & rO );
                                    break;
                                }
                            case (int)JSTokenizerToken.BitwiseOr & 15:
                                {
                                    Int64 l = JSSupport.ToInt64( left.ToDouble() );
                                    Int64 rO = JSSupport.ToInt64( right.ToDouble() );
                                    result = Global.CreateNumber( l | rO );
                                    break;
                                }
                            case (int)JSTokenizerToken.BitwiseXOr & 15:
                                {
                                    Int64 l = JSSupport.ToInt64( left.ToDouble() );
                                    Int64 rO = JSSupport.ToInt64( right.ToDouble() );
                                    result = Global.CreateNumber( l ^ rO );
                                    break;
                                }
                            case (int)JSTokenizerToken.BitwiseShiftLeft & 15:
                                {
                                    result = BitwiseShift( left, right, false );
                                    break;
                                }
                            case (int)JSTokenizerToken.BitwiseShiftRight & 15:
                                {
                                    result = BitwiseShift( left, right, true );
                                    break;
                                }
                            case (int)JSTokenizerToken.BitwiseShiftRightNoSignBit & 15:
                                {
                                    result = BitwiseShiftRightUnsigned( left, right );
                                    break;
                                }
                            default: throw UnsupportedOperatorException();
                        }
                        #endregion
                    }
                    else throw UnsupportedOperatorException();
                }
                return SetResult( result );
            }

            NotImplementedException UnsupportedOperatorException()
            {
                string msg = String.Format( "Unsupported binary operator: '{0}' ({1}).", JSTokenizer.Explain( Expr.BinaryOperatorToken ), (int)Expr.BinaryOperatorToken );
                return new NotImplementedException( msg );
            }

            RuntimeObj BitwiseShift( RuntimeObj val, RuntimeObj shift, bool right )
            {
                if( val == JSEvalNumber.Zero ) return val;
                double dR = shift.ToDouble();
                int iShift;
                if( Double.IsNaN( dR ) || (iShift = (dR < 0 ? (int)Math.Ceiling( dR ) : (int)Math.Floor( dR )) % 64) == 0 ) return Global.CreateNumber( val );
                if( right && iShift < 0 ) return JSEvalNumber.Zero;
                Int32 lN = JSSupport.ToInt32( val.ToDouble() );
                if( lN == 0 ) return JSEvalNumber.Zero;
                return Global.CreateNumber( right ? lN >> iShift : lN << iShift );
            }

            RuntimeObj BitwiseShiftRightUnsigned( RuntimeObj left, RuntimeObj right )
            {
                if( left == JSEvalNumber.Zero ) return left;
                
                double dR = right.ToDouble();
                if( Double.IsNaN( dR ) ) return Global.CreateNumber( left );
                int iShift = (dR < 0 ? (int)Math.Ceiling( dR ) : (int)Math.Floor( dR )) % 64;
                if( iShift < 0 ) return JSEvalNumber.Zero;

                UInt32 lN = (UInt32)JSSupport.ToInt64( left.ToDouble() );
                if( lN == 0 ) return JSEvalNumber.Zero;

                return Global.CreateNumber( lN >> iShift );
            }
        }

        public PExpr Visit( BinaryExpr e )
        {
            return Run( new BinaryExprFrame( this, e ) );
        }

    }
}
