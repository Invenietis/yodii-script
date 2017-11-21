#region LGPL License
/*----------------------------------------------------------------------------
* This file (Yodii.Script\EvalVisitor\EvalVisitor.Frame.cs) is part of Yodii-Script. 
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
    internal partial class EvalVisitor
    {
        /// <summary>
        /// This is a basic frame object that captures an evaluation step. 
        /// The "stack" is implemented with links to a previous and next frames.
        /// </summary>
        internal abstract class Frame : IDeferredExpr
        {          
            internal readonly EvalVisitor Visitor;
            readonly Expr _expr;
            Frame _prev;
            Frame _next;
            RuntimeObj _result;
            bool _isClosedForNext;

            protected Frame( EvalVisitor visitor, Expr e )
            {
                Visitor = visitor;
                _prev = visitor._currentFrame;
                if( _prev != null ) _prev._next = this;
                else visitor._firstFrame = this;
                visitor._currentFrame = this;
                _expr = e;
            }

            internal void CloseForNext( bool close )
            {
                _isClosedForNext = close;
            }

            public Expr Expr => _expr; 

            public RuntimeObj Result => _result; 

            public bool IsResolved => _result != null; 

            public PExpr StepOver() => Visitor.StepOver( this, StepOverKind.ExternalStepOver );

            public PExpr StepIn() => Visitor.StepOver( this, StepOverKind.ExternalStepIn );

            internal PExpr VisitAndClean()
            {
                PExpr r = DoVisit();
                Debug.Assert( r.Result == _result || (r.Result == null && r.Deferred != null) );
                if( _result != null )
                {
                    r = new PExpr( _result );
                    DoDispose();
                }
                return r;
            }

            internal void DoDispose()
            {
                OnDispose();
                Visitor._currentFrame = _prev;
                if( _prev != null ) _prev._next = null;
                else Visitor._firstFrame = null;
            }

            protected abstract PExpr DoVisit();

            public PExpr PendingOrSignal( PExpr sub )
            {
                Debug.Assert( sub.IsPendingOrSignal );
                return sub.IsResolved ? SetResult( sub.Result ) : new PExpr( this, sub.PendingStatus );
            }

            public bool IsPendingOrSignal( ref PExpr current, Expr e )
            {
                if( current.IsResolved ) return false;
                if( current.IsUnknown ) current = Visitor.VisitExpr( e );
                else current = Visitor.StepOver( current.Frame, StepOverKind.InternalStepOver );
                return current.IsPendingOrSignal;
            }

            public virtual PExpr SetResult( RuntimeObj result )
            {
                Debug.Assert( _result == null );
                RuntimeError e = result as RuntimeError;
                if( e != null && !(e.Expr is SyntaxErrorExpr) && Visitor.EnableFirstChanceError )
                {
                    Visitor.FirstChanceError = e;
                    return new PExpr( this, PExprKind.FirstChanceError );
                }
                return new PExpr( (_result = result) );
            }

            public Frame NextFrame => _next; 

            public Frame PrevFrame => _prev == null || _prev._isClosedForNext  ? null : _prev; 

            public GlobalContext Global => Visitor.Global; 
            
            protected virtual void OnDispose()
            {
            }
        }

        internal abstract class Frame<T> : Frame where T : Expr
        {
            protected Frame( EvalVisitor evaluator, T e )
                : base( evaluator, e )
            {
            }

            public new T Expr { get { return (T)base.Expr; } }
        }

    }
}
