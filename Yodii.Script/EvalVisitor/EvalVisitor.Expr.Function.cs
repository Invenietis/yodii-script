#region LGPL License
/*----------------------------------------------------------------------------
* This file (Yodii.Script\EvalVisitor\EvalVisitor.Expr.Function.cs) is part of Yodii-Script. 
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
        internal class FunctionExprFrame : Frame<FunctionExpr>
        {
            readonly ArgumentResolver _arguments;
            readonly IReadOnlyList<Closure> _closures;
            PExpr _body;

            public FunctionExprFrame( AccessorFrame callFrame, FunctionExpr e, IReadOnlyList<Closure> closures )
                : base( callFrame._visitor, e )
            {
                _arguments = new ArgumentResolver( this, callFrame.Expr.Arguments );
                _closures = closures;
                // Registering closed variables.
                foreach( var c in _closures )
                {
                    _visitor.ScopeManager.Register( c );
                }
            }

            protected override PExpr DoVisit()
            {
                if( !_arguments.IsArgumentsResolved )
                {
                    PExpr args = _arguments.VisitArguments();
                    if( args.IsPendingOrSignal ) return PendingOrSignal( args );

                    // Registering parameters.
                    Debug.Assert( _arguments.IsArgumentsResolved );
                    int iParam = 0;
                    foreach( var parameter in Expr.Parameters )
                    {
                        var r = _visitor.ScopeManager.Register( parameter );
                        if( iParam < _arguments.ResolvedParameters.Count ) r.Value = _arguments.ResolvedParameters[iParam];
                        ++iParam;
                    }
                }

                if( IsPendingOrSignal( ref _body, Expr.Body ) )
                {
                    if( _body.IsSignal )
                    {
                        RuntimeFlowBreaking r = _body.Result as RuntimeFlowBreaking;
                        if( r != null && r.Expr.Type == FlowBreakingExpr.BreakingType.Return )
                        {
                            return SetResult( r.Value );
                        }
                    }
                    return PendingOrSignal( _body );
                }
                return SetResult( RuntimeObj.Undefined );
            }

            public override PExpr SetResult( RuntimeObj result )
            {
                Debug.Assert( PrevFrame is IAccessorFrame );
                PrevFrame.SetResult( result );
                return base.SetResult( result );
            }

            protected override void OnDispose()
            {
                foreach( var c in _closures )
                {
                    _visitor.ScopeManager.Unregister( c.Variable );
                }
                if( _arguments.IsArgumentsResolved )
                {
                    foreach( var local in Expr.Parameters )
                    {
                        _visitor.ScopeManager.Unregister( local );
                    }
                }
            }
        }

        public PExpr Visit( FunctionExpr e )
        {
            Closure[] c = new Closure[e.Closures.Count];
            for( int i = 0; i < c.Length; ++i )
            {
                var v = e.Closures[i];
                c[i] = new Closure( v, ScopeManager.FindRegistered( v ) );
            }
            return new PExpr( new JSEvalFunction( e, c ) );
        }

    }
}
