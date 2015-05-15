#region LGPL License
/*----------------------------------------------------------------------------
* This file (Yodii.Script\EvalVisitor\EvalVisitor.Expr.If.cs) is part of Yodii-Script. 
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
        class TryCatchExprFrame : Frame<TryCatchExpr>
        {
            PExpr _try;
            PExpr _catch;
            bool _catching;

            public TryCatchExprFrame( EvalVisitor evaluator, TryCatchExpr e )
                : base( evaluator, e )
            {
            }

            protected override PExpr DoVisit()
            {
                if( IsPendingOrSignal( ref _try, Expr.TryExpr ) )
                {
                    RuntimeError e = _try.AsErrorResult;
                    if( e == null || !e.IsCatchable )
                    {
                        return PendingOrSignal( _try );
                    }
                    if( !_catching )
                    {
                        if( Expr.ExceptionParameter != null )
                        {
                            _visitor.ScopeManager.Register( Expr.ExceptionParameter ).Value = e.ThrownValue;
                        }
                        _visitor.FirstChanceError = null;
                        _catching = true;
                    }
                    if( IsPendingOrSignal( ref _catch, Expr.CatchExpr ) ) return PendingOrSignal( _catch );
                }
                return SetResult( RuntimeObj.Undefined );
            }

            protected override void OnDispose()
            {
                if( _catching && Expr.ExceptionParameter != null )
                {
                    _visitor.ScopeManager.Unregister( Expr.ExceptionParameter );
                }
            }
        }

        public PExpr Visit( TryCatchExpr e )
        {
            return Run( new TryCatchExprFrame( this, e ) );
        }
    }
}
