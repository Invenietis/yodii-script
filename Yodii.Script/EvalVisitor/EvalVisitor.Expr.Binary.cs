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
                if( (Expr.BinaryOperatorToken == TokenizerToken.And && !_left.Result.ToBoolean())
                    || (Expr.BinaryOperatorToken == TokenizerToken.Or && _left.Result.ToBoolean()) )
                {
                    return SetResult( _left.Result );
                }

                if( IsPendingOrSignal( ref _right, Expr.Right ) ) return PendingOrSignal( _right );

                RuntimeObj left = _left.Result;
                RuntimeObj right = _right.Result;

                // Right value is the result for And and Or.
                RuntimeObj result = right;

                if( Expr.BinaryOperatorToken != TokenizerToken.And && Expr.BinaryOperatorToken != TokenizerToken.Or )
                {
                    if( (Expr.BinaryOperatorToken & TokenizerToken.IsCompareOperator) != 0 )
                    {
                        #region ==, <, >, <=, >=, !=
                        int compareValue;
                        switch( (int)Expr.BinaryOperatorToken & 15 )
                        {
                            case (int)TokenizerToken.Greater & 15:
                                {
                                    result = new RuntimeObjComparer( left, right ).Compare( Global, out compareValue ) && compareValue > 0 ? BooleanObj.True : BooleanObj.False;
                                    break;
                                }
                            case (int)TokenizerToken.GreaterOrEqual & 15:
                                {
                                    result = new RuntimeObjComparer( left, right ).Compare( Global, out compareValue ) && compareValue >= 0 ? BooleanObj.True : BooleanObj.False;
                                    break;
                                }
                            case (int)TokenizerToken.Less & 15:
                                {
                                    result = new RuntimeObjComparer( left, right ).Compare( Global, out compareValue ) && compareValue < 0 ? BooleanObj.True : BooleanObj.False;
                                    break;
                                }
                            case (int)TokenizerToken.LessOrEqual & 15:
                                {
                                    result = new RuntimeObjComparer( left, right ).Compare( Global, out compareValue ) && compareValue <= 0 ? BooleanObj.True : BooleanObj.False;
                                    break;
                                }
                            case (int)TokenizerToken.Equal & 15:
                                {
                                    result = new RuntimeObjComparer( left, right ).AreEqualStrict( Global ) ? BooleanObj.True : BooleanObj.False;
                                    break;
                                }
                            case (int)TokenizerToken.Different & 15:
                                {
                                    result = !new RuntimeObjComparer( left, right ).AreEqualStrict( Global ) ? BooleanObj.True : BooleanObj.False;
                                    break;
                                }
                            default: throw UnsupportedOperatorException();
                        }
                        #endregion
                    }
                    else if( (Expr.BinaryOperatorToken & TokenizerToken.IsBinaryOperator) != 0 )
                    {
                        #region |, ^, &, >>, <<, >>>, +, -, /, * and %.
                        switch( (int)Expr.BinaryOperatorToken & 15 )
                        {
                            case (int)TokenizerToken.Plus & 15:
                                {
                                    if( ReferenceEquals( left.Type, RuntimeObj.TypeNumber ) && ReferenceEquals( right.Type, RuntimeObj.TypeNumber ) )
                                    {
                                        result = DoubleObj.Create( left.ToDouble() + right.ToDouble() );
                                    }
                                    else
                                    {
                                        result = StringObj.Create( left.ToString() + right.ToString() );
                                    }
                                    break;
                                }
                            case (int)TokenizerToken.Minus & 15:
                                {
                                    result = DoubleObj.Create( left.ToDouble() - right.ToDouble() );
                                    break;
                                }
                            case (int)TokenizerToken.Mult & 15:
                                {
                                    result = DoubleObj.Create( left.ToDouble() * right.ToDouble() );
                                    break;
                                }
                            case (int)TokenizerToken.Divide & 15:
                                {
                                    result = DoubleObj.Create( left.ToDouble() / right.ToDouble() );
                                    break;
                                }
                            case (int)TokenizerToken.Modulo & 15:
                                {
                                    if( right == DoubleObj.Zero || left == DoubleObj.NegativeInfinity || left == DoubleObj.Infinity )
                                    {
                                        result = DoubleObj.NaN;
                                    }
                                    else if( left == DoubleObj.NegativeInfinity || left == DoubleObj.Infinity )
                                    {
                                        result = right;
                                    }
                                    else
                                    {
                                        result = DoubleObj.Create( left.ToDouble() % right.ToDouble() );
                                    }
                                    break;
                                }
                            case (int)TokenizerToken.BitwiseAnd & 15:
                                {
                                    long l = JSSupport.ToInt64( left.ToDouble() );
                                    long rO = JSSupport.ToInt64( right.ToDouble() );
                                    result = DoubleObj.Create( l & rO );
                                    break;
                                }
                            case (int)TokenizerToken.BitwiseOr & 15:
                                {
                                    long l = JSSupport.ToInt64( left.ToDouble() );
                                    long rO = JSSupport.ToInt64( right.ToDouble() );
                                    result = DoubleObj.Create( l | rO );
                                    break;
                                }
                            case (int)TokenizerToken.BitwiseXOr & 15:
                                {
                                    long l = JSSupport.ToInt64( left.ToDouble() );
                                    long rO = JSSupport.ToInt64( right.ToDouble() );
                                    result = DoubleObj.Create( l ^ rO );
                                    break;
                                }
                            case (int)TokenizerToken.BitwiseShiftLeft & 15:
                                {
                                    result = BitwiseShift( left, right, false );
                                    break;
                                }
                            case (int)TokenizerToken.BitwiseShiftRight & 15:
                                {
                                    result = BitwiseShift( left, right, true );
                                    break;
                                }
                            case (int)TokenizerToken.BitwiseShiftRightNoSignBit & 15:
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
                string msg = String.Format( "Unsupported binary operator: '{0}' ({1}).", Expr.BinaryOperatorToken.Explain(), (int)Expr.BinaryOperatorToken );
                return new NotImplementedException( msg );
            }

            RuntimeObj BitwiseShift( RuntimeObj val, RuntimeObj shift, bool right )
            {
                if( val == DoubleObj.Zero ) return val;
                double dR = shift.ToDouble();
                int iShift;
                if( double.IsNaN( dR ) || (iShift = (dR < 0 ? (int)Math.Ceiling( dR ) : (int)Math.Floor( dR )) % 64) == 0 )
                {
                    return val.ToValue() as DoubleObj ?? DoubleObj.Create( val.ToDouble() );
                }
                if( right && iShift < 0 ) return DoubleObj.Zero;
                int lN = JSSupport.ToInt32( val.ToDouble() );
                if( lN == 0 ) return DoubleObj.Zero;
                return DoubleObj.Create( right ? lN >> iShift : lN << iShift );
            }

            RuntimeObj BitwiseShiftRightUnsigned( RuntimeObj left, RuntimeObj right )
            {
                if( left == DoubleObj.Zero ) return left;
                
                double dR = right.ToDouble();
                if( double.IsNaN( dR ) ) return left is DoubleObj ? left : DoubleObj.Create( left.ToDouble() );
                int iShift = (dR < 0 ? (int)Math.Ceiling( dR ) : (int)Math.Floor( dR )) % 64;
                if( iShift < 0 ) return DoubleObj.Zero;

                uint lN = (uint)JSSupport.ToInt64( left.ToDouble() );
                if( lN == 0 ) return DoubleObj.Zero;

                return DoubleObj.Create( lN >> iShift );
            }
        }

        public PExpr Visit( BinaryExpr e ) => Run( new BinaryExprFrame( this, e ) );

    }
}
