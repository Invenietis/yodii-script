#region LGPL License
/*----------------------------------------------------------------------------
* This file (Yodii.Script\EvalVisitor\EvalVisitor.Expr.Accessors.cs) is part of Yodii-Script. 
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
        class ArgumentResolver : IReadOnlyList<RuntimeObj>
        {
            protected readonly Frame Frame;
            readonly IReadOnlyList<Expr> _arguments;
            readonly PExpr[] _args;
            int _rpCount;

            public ArgumentResolver( Frame frame, IReadOnlyList<Expr> arguments )
            {
                Frame = frame;
                _arguments = arguments;
                _args = new PExpr[_arguments.Count];
            }

            public bool IsArgumentsResolved
            {
                get { return _rpCount == _args.Length; }
            }

            public PExpr VisitArguments()
            {
                while( _rpCount < _args.Length )
                {
                    if( Frame.IsPendingOrSignal( ref _args[_rpCount], _arguments[_rpCount] ) ) return Frame.PendingOrSignal( _args[_rpCount] );
                    ++_rpCount;
                }
                return new PExpr( RuntimeObj.Undefined );
            }

            #region Auto implemented access to resolved arguments (avoids an allocation).
                
            public IReadOnlyList<RuntimeObj> ResolvedParameters { get { return this; } }

            RuntimeObj IReadOnlyList<RuntimeObj>.this[int index]
            {
                get { return _args[index].Result; }
            }

            int IReadOnlyCollection<RuntimeObj>.Count
            {
                get { return _args.Length; }
            }

            IEnumerator<RuntimeObj> IEnumerable<RuntimeObj>.GetEnumerator()
            {
                return _args.Select( e => e.Result ).GetEnumerator();
            }

            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
            {
                return ((IEnumerable<RuntimeObj>)this).GetEnumerator();
            }
            #endregion
        }      
        
        /// <summary>
        /// This frame applies to Indexer and Call. 
        /// AccessorMemberFrame specializes it to lookup the GlobalContext if the member is unbound.
        /// For let, there is no frame: visiting a let accessor simply does the lookup in the dynamic scope.
        /// </summary>
        internal class AccessorFrame : Frame<AccessorExpr>, IAccessorFrame
        {
            class FrameState : ArgumentResolver, IAccessorFrameState
            {
                readonly Func<IAccessorFrame, RuntimeObj, PExpr> _indexCode;
                readonly Func<IAccessorFrame, IReadOnlyList<RuntimeObj>, PExpr> _callCode;

                public FrameState( AccessorFrame winner,
                                   Func<IAccessorFrame, RuntimeObj, PExpr> indexCode,
                                   Func<IAccessorFrame, IReadOnlyList<RuntimeObj>, PExpr> callCode,
                                   IReadOnlyList<Expr> arguments )
                    : base( winner, arguments )
                {
                    _indexCode = indexCode;
                    _callCode = callCode;
                }

                public PExpr Visit()
                {
                    var f = (AccessorFrame)Frame;
                    f._initCount = 0;
                    PExpr args = VisitArguments();
                    if( args.IsPendingOrSignal ) return args;
                    var r = _indexCode != null ? _indexCode( f, ResolvedParameters[0] ) : _callCode( f, this );
                    if( !r.IsResolved && r.Deferred != Frame ) throw new InvalidOperationException( "Implementations must call either SetResult, SetError, or PendigOrSignal frame's method." );
                    return r;
                }
            }

            class FrameInitializer : IAccessorFrameInitializer
            {
                readonly AccessorFrame _frame;
                AccessorFrame _current;
                FrameState _state;

                public FrameInitializer( AccessorFrame f )
                {
                    _current = _frame = f;
                }

                public FrameState State { get { return _state; } }

                public IAccessorFrameInitializer On( string memberName )
                {
                    if( _state == null && _current != null )
                    {
                        _current = (AccessorFrame)(_current.Expr.IsMember( memberName ) ? _current.NextAccessor : null);
                    }
                    return this;
                }

                public IAccessorFrameInitializer OnIndex( Func<IAccessorFrame, RuntimeObj, PExpr> code )
                {
                    if( _state == null )
                    {
                        if( _current != null && _current.Expr is AccessorIndexerExpr )
                        {
                            _state = new FrameState( _current, code, null, _current.Expr.Arguments );
                        }
                        _current = _frame;
                    }
                    return this;
                }

                public IAccessorFrameInitializer OnCall( Func<IAccessorFrame, IReadOnlyList<RuntimeObj>, PExpr> code )
                {
                    if( _state == null )
                    {
                        if( _current != null && _current.Expr is AccessorCallExpr )
                        {
                            _state = new FrameState( _current, null, code, _current.Expr.Arguments );
                        }
                        _current = _frame;
                    }
                    return this;
                }
            }

            IAccessorFrameState _state;
            int _initCount;
            int _realInitCount;
            protected PExpr _left;
            protected PExpr _result;

            internal protected AccessorFrame( EvalVisitor visitor, AccessorExpr e )
                : base( visitor, e )
            {
            }

            /// <summary>
            /// Implementation valid for AccessorIndexerFrame and AccessorCallFrame.
            /// The AccessorMemberFrame substitutes it.
            /// </summary>
            protected override PExpr DoVisit()
            {
                if( IsPendingOrSignal( ref _left, Expr.Left ) ) return ReentrantPendingOrSignal( _left );
                // A visit of the left may have set a result farther in the accessor chain:
                // if Result is set, we leave.
                if( Result != null ) return new PExpr( Result );

                Debug.Assert( _left.Result != null, "We are not pendig..." );
                var left = _left.Result;
                Debug.Assert( !_result.IsResolved );
                if( (_result = left.Visit( this )).IsPendingOrSignal ) return ReentrantPendingOrSignal( _result );
                return ReentrantSetResult( _result.Result );
            }

            public IAccessorFrameState GetState( Action<IAccessorFrameInitializer> configuration )
            {
                if( ++_initCount < _realInitCount ) return null;
                if( _state == null )
                {
                    var init = new FrameInitializer( this );
                    configuration( init );
                    _state = init.State;
                    ++_realInitCount;
                }
                return _state;
            }
            
            public IAccessorFrame NextAccessor
            {
                get { return PrevFrame as IAccessorFrame; }
            }

            IAccessorFrame PrevAccessor
            {
                get { return NextFrame as IAccessorFrame; }
            }

            public override PExpr SetResult( RuntimeObj result )
            {
                IAccessorFrame p = PrevAccessor;
                if( p != null && !p.IsResolved ) p.SetResult( result );
                return base.SetResult( result );
            }

            public PExpr SetError( string message = null )
            {
                if( message != null ) return SetResult( _visitor._global.CreateSyntaxError( Expr, message ) );
                return SetResult( _visitor._global.CreateAccessorSyntaxError( Expr ) );
            }

            AccessorExpr IAccessorFrame.Expr 
            { 
                get { return base.Expr; } 
            }

            protected PExpr ReentrantPendingOrSignal( PExpr sub )
            {
                Debug.Assert( sub.IsPendingOrSignal );
                if( Result != null )
                {
                    Debug.Assert( Result == sub.Result );
                    return sub;
                }
                return sub.IsResolved ? SetResult( sub.Result ) : new PExpr( this, sub.DeferredStatus );
            }

            protected PExpr ReentrantSetResult( RuntimeObj result )
            {
                Debug.Assert( result != null );
                if( Result != null )
                {
                    Debug.Assert( Result == result );
                    return new PExpr( result );
                }
                return SetResult( result );
            }

            public PExpr ReentrantSetError( string message = null )
            {
                Debug.Assert( Result == null || Result is RuntimeError );
                return Result == null ? SetError( message ) : new PExpr( Result ); 
            }

        }

        class AccessorMemberFrame : AccessorFrame
        {
            internal protected AccessorMemberFrame( EvalVisitor visitor, AccessorMemberExpr e )
                : base( visitor, e )
            {
            }

            protected override PExpr DoVisit()
            {
                if( !_left.IsResolved )
                {
                    if( ((AccessorMemberExpr)Expr).IsUnbound )
                    {
                        if( (_left = Global.Visit( this )).IsPendingOrSignal ) return ReentrantPendingOrSignal( _left );
                    }
                    else
                    {
                        if( IsPendingOrSignal( ref _left, Expr.Left ) ) return ReentrantPendingOrSignal( _left );
                    }
                }
                if( Result != null ) return new PExpr( Result );

                Debug.Assert( !_result.IsResolved );
                if( (_result = _left.Result.Visit( this )).IsPendingOrSignal ) return ReentrantPendingOrSignal( _result );
                return ReentrantSetResult( _result.Result );
            }

        }

        public PExpr Visit( AccessorMemberExpr e )
        {
            return Run( new AccessorMemberFrame( this, e ) );
        }

        public PExpr Visit( AccessorIndexerExpr e )
        {
            return Run( new AccessorFrame( this, e ) );
        }

        public PExpr Visit( AccessorCallExpr e )
        {
            return Run( new AccessorFrame( this, e ) );
        }

        public PExpr Visit( AccessorLetExpr e )
        {
            return new PExpr( ScopeManager.FindRegistered( e ) );
        }
    }
}
