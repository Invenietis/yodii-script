#region LGPL License
/*----------------------------------------------------------------------------
* This file (Yodii.Script\EvalVisitor\EvalVisitor.Expr.While.cs) is part of Yodii-Script. 
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
using System.Collections;

namespace Yodii.Script
{

    internal partial class EvalVisitor
    {
        class ForeachExprFrame : Frame<ForeachExpr>
        {
            PExpr _generator;
            PExpr _code;
            IEnumerator _nativeEnum;
            RefRuntimeObj _currentVariable;
            int _index;

            public ForeachExprFrame( EvalVisitor evaluator, ForeachExpr e )
                : base( evaluator, e )
            {
            }

            protected override PExpr DoVisit()
            {
                if( _nativeEnum == null )
                {
                    if( IsPendingOrSignal( ref _generator, Expr.Generator ) ) return PendingOrSignal( _generator );
                    var a = _generator.Result.ToNative( Global ) as IEnumerable;
                    if( a == null ) return new PExpr( new RuntimeError( Expr.Generator, "foreach generator is not an IEnumerable." ) );
                    try
                    {
                        _nativeEnum = a.GetEnumerator();
                    }
                    catch( Exception ex )
                    {
                        return new PExpr( new RuntimeError( Expr.Generator, ex.Message ) );
                    }
                }
                for( ; ; )
                {
                    if( _currentVariable == null )
                    {
                        bool hasNext;
                        try
                        {
                            hasNext = _nativeEnum.MoveNext();
                        }
                        catch( Exception ex )
                        {
                            return new PExpr( new RuntimeError( Expr.Generator, ex.Message ) );
                        }
                        if( !hasNext ) break;
                        _currentVariable = _visitor.ScopeManager.Register( Expr.Variable, _index++ );
                        _currentVariable.SetValue( Expr.Variable, Global.Create( _nativeEnum.Current ) );
                    }
                    if( IsPendingOrSignal( ref _code, Expr.Code ) ) return PendingOrSignal( _code );
                    _code = new PExpr();
                    _visitor.ScopeManager.Unregister( Expr.Variable );
                    _currentVariable = null;
                }
                _nativeEnum = null;
                return SetResult( RuntimeObj.Undefined );
            }
        }

        public PExpr Visit( ForeachExpr e ) => Run( new ForeachExprFrame( this, e ) );

    }
}
