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
    public partial class EvalVisitor
    {
        /// <summary>
        /// This is a basic frame object that captures an evaluation step. 
        /// The "stack" is implemented with links to a previous and next frames.
        /// </summary>
        internal abstract class Frame : IDeferredExpr
        {          
            internal readonly EvalVisitor _visitor;
            readonly Expr _expr;
            Frame _prev;
            Frame _next;
            RuntimeObj _result;

            protected Frame( EvalVisitor visitor, Expr e )
            {
                _visitor = visitor;
                _prev = visitor._currentFrame;
                if( _prev != null ) _prev._next = this;
                else visitor._firstFrame = this;
                visitor._currentFrame = this;
                _expr = e;
            }

            public Expr Expr
            {
                get { return _expr; }
            }

            public RuntimeObj Result
            {
                get { return _result; }
            }

            public bool IsResolved
            {
                get { return Result != null; }
            }

            public PExpr StepOver()
            {
                return _result == null ? DoVisit() : new PExpr( _result );
            }

            public PExpr StepIn()
            {
                _visitor.BreakOnNext = true;
                return StepOver();
            }

            internal PExpr Visit()
            {
                Debug.Assert( _result == null );
                if( Expr.IsBreakable && (_visitor.BreakOnNext || _visitor._breakpoints( Expr )) )
                {
                    _visitor.BreakOnNext = false;
                    return new PExpr( this );
                }
                PExpr r = DoVisit();
                Debug.Assert( r.Result == _result && (r.Deferred == null || r.Deferred == this ) );
                if( _result != null )
                {
                    if( !(_result is RuntimeSignal) || OnSignal( ref _result ) )
                    {
                        r = new PExpr( _result );
                        OnDispose();
                        _visitor._currentFrame = _prev;
                        if( _prev != null ) _prev._next = null;
                        else _visitor._firstFrame = null;
                    }
                }
                return r;
            }

            protected abstract PExpr DoVisit();

            public PExpr PendingOrSignal( PExpr sub )
            {
                return sub.IsSignal ? SetResult( sub.Result ) : new PExpr( this );
            }

            public bool IsPendingOrSignal( ref PExpr current, Expr e )
            {
                if( current.IsResolved ) return false;
                if( current.IsUnknown ) current = _visitor.VisitExpr( e );
                else current = current.Deferred.StepOver();
                return current.IsPendingOrSignal;
            }

            public virtual PExpr SetResult( RuntimeObj result )
            {
                Debug.Assert( _result == null );
                return new PExpr( (_result = result) );
            }

            public Frame NextFrame
            {
                get { return _next; }
            }

            public Frame PrevFrame
            {
                get { return _prev; }
            }

            public IEvalVisitor Visitor
            {
                get { return _visitor; }
            }

            public GlobalContext Global
            {
                get { return _visitor.Global; }
            }

            /// <summary>
            /// Must return true to dispose the frame, false to keep the frame alive.
            /// By default, when result is a <see cref="RuntimeError"/>, the frame is disposed (unless KeepStackOnError is true).
            /// </summary>
            /// <param name="result">The result of the frame (initially a <see cref="RuntimeSignal"/>) that can be updated.</param>
            /// <returns>True to dispose the frame. False to keep it alive.</returns>
            protected virtual bool OnSignal( ref RuntimeObj result )
            {
                return !(_visitor._keepStackOnError && _result is RuntimeError);
            }
            
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
