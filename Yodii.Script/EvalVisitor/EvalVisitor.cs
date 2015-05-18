#region LGPL License
/*----------------------------------------------------------------------------
* This file (Yodii.Script\EvalVisitor\EvalVisitor.cs) is part of Yodii-Script. 
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

    internal partial class EvalVisitor : IExprVisitor<PExpr>
    {
        internal readonly DynamicScope ScopeManager;
        readonly GlobalContext _global;
        readonly Func<Expr,bool> _breakpoints;
        Frame _firstFrame;
        Frame _currentFrame;
        bool _breakOnNext;
        bool _hasTimeout;
        DateTime _timeLimit;
        TimeSpan _timeout;
        RuntimeError _firstChanceError;
        bool _enableFirstChanceError;

        public EvalVisitor( GlobalContext context, Func<Expr, bool> breakpoints = null, DynamicScope scopeManager = null )
        {
            if( context == null ) throw new ArgumentNullException( "context" );
            _global = context;
            _breakpoints = breakpoints ?? (e => false);
            ScopeManager = scopeManager ?? new DynamicScope();
        }

        [DebuggerStepThrough]
        public PExpr VisitExpr( Expr e )
        {
            return e.Accept( this );
        }

        public GlobalContext Global 
        {
            get { return _global; }
        }

        public IDeferredExpr CurrentFrame
        {
            get { return _currentFrame; }
        }

        public IDeferredExpr FirstFrame
        {
            get { return _firstFrame; }
        }

        public IEnumerable<IDeferredExpr> Frames
        {
            get 
            { 
                var f = _firstFrame; 
                while( f != null )
                {
                    yield return f;
                    f = f.NextFrame;
                }
            }
        }

        public bool EnableFirstChanceError
        {
            get { return _enableFirstChanceError && _firstChanceError == null; }
            set { _enableFirstChanceError = value; }
        }

        /// <summary>
        /// Gets or sets the timeout limit: <see cref="TimeSpan.Zero"/> and 
        /// </summary>
        public TimeSpan Timeout
        {
            get { return _timeout; }
            set 
            { 
                if( _timeout != value )
                {
                    _timeout = value;
                    _hasTimeout = _timeout != TimeSpan.Zero && _timeout != TimeSpan.MaxValue;
                }
            }
        }

        public void ResetCurrentEvaluation()
        {
            _firstFrame = _currentFrame = null;
            _firstChanceError = null;
        }

        public RuntimeError FirstChanceError
        {
            get { return _firstChanceError; }
            internal set
            {
                Debug.Assert( value == null || _enableFirstChanceError );
                Debug.Assert( value == null || _firstChanceError == null );
                _firstChanceError = value;
            }
        }

        internal enum StepOverKind
        {
            InternalStepOver = 0,
            ExternalStepOver = -1,
            ExternalStepIn = 1
        }

        internal PExpr StepOver( Frame f, StepOverKind kind )
        {
            if( !f.IsResolved )
            {
                RuntimeError fError = null;
                if( kind != StepOverKind.InternalStepOver )
                {
                    fError = _firstChanceError;
                    _breakOnNext = kind == StepOverKind.ExternalStepIn;
                    if( _hasTimeout ) _timeLimit = DateTime.UtcNow + _timeout;
                }
                do
                {
                    Debug.Assert( Frames.Contains( f ) );
                    if( fError != null )
                    {
                        PExpr r = _currentFrame.SetResult( fError );
                        _currentFrame.DoDispose();
                        fError = null;
                    }
                    else
                    {
                        PExpr r = _currentFrame.VisitAndClean();
                        if( r.IsPending ) return r;
                    }
                }
                while( !f.IsResolved );
            }
            return new PExpr( f.Result );
        }

        internal PExpr Run( Frame f )
        {
            Debug.Assert( !f.IsResolved );
            if( f.Expr.IsBreakable )
            {
                if( _breakOnNext || _breakpoints( f.Expr ) )
                {
                    _breakOnNext = false;
                    return new PExpr( f, PExpr.DeferredKind.Breakpoint );
                }
                if( _hasTimeout && _timeLimit <= DateTime.UtcNow )
                {
                    return new PExpr( f, PExpr.DeferredKind.Timeout );
                }
            }
            return f.VisitAndClean();
        }
    }
}
