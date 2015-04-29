#region LGPL License
/*----------------------------------------------------------------------------
* This file (Yodii.Script\EvalVisitor\EvalVisitor.Expr.Binary.cs) is part of CiviKey. 
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
using System.Diagnostics;
using CK.Core;
using System.Collections.ObjectModel;

namespace Yodii.Script
{

    public partial class EvalVisitor
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
                if( (Expr.BinaryOperatorToken == JSTokeniserToken.And && !_left.Result.ToBoolean())
                    || (Expr.BinaryOperatorToken == JSTokeniserToken.Or && _left.Result.ToBoolean()) )
                {
                    return SetResult( _left.Result );
                }

                if( IsPendingOrSignal( ref _right, Expr.Right ) ) return PendingOrSignal( _right );

                RuntimeObj left = _left.Result;
                RuntimeObj right = _right.Result;

                // Right value is the result for And and Or.
                RuntimeObj result = right;

                if( Expr.BinaryOperatorToken != JSTokeniserToken.And && Expr.BinaryOperatorToken != JSTokeniserToken.Or )
                {
                    if( (Expr.BinaryOperatorToken & JSTokeniserToken.IsCompareOperator) != 0 )
                    {
                        #region ==, <, >, <=, >=, !=, === and !==
                        int compareValue;
                        switch( (int)Expr.BinaryOperatorToken & 15 )
                        {
                            case (int)JSTokeniserToken.StrictEqual & 15:
                                {
                                    result = Global.CreateBoolean( new RuntimeObjComparer( left, right ).AreEqualStrict( Global ) );
                                    break;
                                }
                            case (int)JSTokeniserToken.StrictDifferent & 15:
                                {
                                    result = Global.CreateBoolean( !new RuntimeObjComparer( left, right ).AreEqualStrict( Global ) );
                                    break;
                                }
                            case (int)JSTokeniserToken.Greater & 15:
                                {
                                    result = Global.CreateBoolean( new RuntimeObjComparer( left, right ).Compare( Global, out compareValue ) && compareValue > 0 );
                                    break;
                                }
                            case (int)JSTokeniserToken.GreaterOrEqual & 15:
                                {
                                    result = Global.CreateBoolean( new RuntimeObjComparer( left, right ).Compare( Global, out compareValue ) && compareValue >= 0 );
                                    break;
                                }
                            case (int)JSTokeniserToken.Less & 15:
                                {
                                    result = Global.CreateBoolean( new RuntimeObjComparer( left, right ).Compare( Global, out compareValue ) && compareValue < 0 );
                                    break;
                                }
                            case (int)JSTokeniserToken.LessOrEqual & 15:
                                {
                                    result = Global.CreateBoolean( new RuntimeObjComparer( left, right ).Compare( Global, out compareValue ) && compareValue <= 0 );
                                    break;
                                }
                            case (int)JSTokeniserToken.Equal & 15:
                                {
                                    result = Global.CreateBoolean( new RuntimeObjComparer( left, right ).AreEqual( Global ) );
                                    break;
                                }
                            case (int)JSTokeniserToken.Different & 15:
                                {
                                    result = Global.CreateBoolean( !new RuntimeObjComparer( left, right ).AreEqual( Global ) );
                                    break;
                                }
                            default: throw UnsupportedOperatorException();
                        }
                        #endregion
                    }
                    else if( (Expr.BinaryOperatorToken & JSTokeniserToken.IsBinaryOperator) != 0 )
                    {
                        #region |, ^, &, >>, <<, >>>, +, -, /, * and %.
                        switch( (int)Expr.BinaryOperatorToken & 15 )
                        {
                            case (int)JSTokeniserToken.Plus & 15:
                                {
                                    RuntimeObj l = left.ToPrimitive( Global );
                                    RuntimeObj rO = right.ToPrimitive( Global );

                                    if( ReferenceEquals( l.Type, RuntimeObj.TypeString ) || ReferenceEquals( rO.Type, RuntimeObj.TypeString ) )
                                    {
                                        result = Global.CreateString( String.Concat( l.ToString(), rO.ToString() ) );
                                    }
                                    else
                                    {
                                        result = Global.CreateNumber( l.ToDouble() + rO.ToDouble() );
                                    }
                                    break;
                                }
                            case (int)JSTokeniserToken.Minus & 15:
                                {
                                    result = Global.CreateNumber( left.ToDouble() - right.ToDouble() );
                                    break;
                                }
                            case (int)JSTokeniserToken.Mult & 15:
                                {
                                    result = Global.CreateNumber( left.ToDouble() * right.ToDouble() );
                                    break;
                                }
                            case (int)JSTokeniserToken.Divide & 15:
                                {
                                    result = Global.CreateNumber( left.ToDouble() / right.ToDouble() );
                                    break;
                                }
                            case (int)JSTokeniserToken.Modulo & 15:
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
                            case (int)JSTokeniserToken.BitwiseAnd & 15:
                                {
                                    Int64 l = JSSupport.ToInt64( left.ToDouble() );
                                    Int64 rO = JSSupport.ToInt64( right.ToDouble() );
                                    result = Global.CreateNumber( l & rO );
                                    break;
                                }
                            case (int)JSTokeniserToken.BitwiseOr & 15:
                                {
                                    Int64 l = JSSupport.ToInt64( left.ToDouble() );
                                    Int64 rO = JSSupport.ToInt64( right.ToDouble() );
                                    result = Global.CreateNumber( l | rO );
                                    break;
                                }
                            case (int)JSTokeniserToken.BitwiseXOr & 15:
                                {
                                    Int64 l = JSSupport.ToInt64( left.ToDouble() );
                                    Int64 rO = JSSupport.ToInt64( right.ToDouble() );
                                    result = Global.CreateNumber( l ^ rO );
                                    break;
                                }
                            case (int)JSTokeniserToken.BitwiseShiftLeft & 15:
                                {
                                    result = BitwiseShift( left, right, false );
                                    break;
                                }
                            case (int)JSTokeniserToken.BitwiseShiftRight & 15:
                                {
                                    result = BitwiseShift( left, right, true );
                                    break;
                                }
                            case (int)JSTokeniserToken.BitwiseShiftRightNoSignBit & 15:
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

            CKException UnsupportedOperatorException()
            {
                return new CKException( "Unsupported binary operator: '{0}' ({1}).", JSTokeniser.Explain( Expr.BinaryOperatorToken ), (int)Expr.BinaryOperatorToken );
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
            return new BinaryExprFrame( this, e ).Visit();
        }

    }
}
