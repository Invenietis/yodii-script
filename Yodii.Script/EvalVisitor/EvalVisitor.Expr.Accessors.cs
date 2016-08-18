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
using System.Threading.Tasks;

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

            public bool IsArgumentsResolved => _rpCount == _args.Length; 

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

            RuntimeObj IReadOnlyList<RuntimeObj>.this[int index] => _args[index].Result; 

            int IReadOnlyCollection<RuntimeObj>.Count => _args.Length; 

            IEnumerator<RuntimeObj> IEnumerable<RuntimeObj>.GetEnumerator() => _args.Select( e => e.Result ).GetEnumerator();

            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => ((IEnumerable<RuntimeObj>)this).GetEnumerator();

            #endregion
        }

        /// <summary>
        /// This is the base frame for accessors. 
        /// AccessorMemberFrame specializes it to lookup the GlobalContext if the member is unbound.
        /// For let, there is no frame: visiting a let accessor simply does the lookup in the dynamic scope.
        /// AccessorCallFrame specializes it to register any named functions declared as parameters.
        /// </summary>
        internal abstract class AccessorFrame : Frame<AccessorExpr>, IAccessorFrame
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
                        AccessorCallExpr c = _current?.Expr as AccessorCallExpr;
                        if( c != null && c.IsIndexer && c.Arguments.Count == 1 )
                        {
                            _state = new FrameState( _current, code, null, c.Arguments );
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
            /// Implementation valid for AccessorCallFrame.
            /// The AccessorMemberFrame substitutes it.
            /// </summary>
            protected override PExpr DoVisit()
            {
                if( IsPendingOrSignal( ref _left, Expr.Left ) ) return ReentrantPendingOrSignal( _left );
                // A visit of the left may have set a result farther in the accessor chain:
                // if Result is set, we leave.
                if( Result != null ) return new PExpr( Result );

                Debug.Assert( _left.Result != null, "We are not pending..." );
                var left = _left.Result;
                Debug.Assert( !_result.IsResolved );
                if( (_result = left.Visit( this )).IsPendingOrSignal ) return ReentrantPendingOrSignal( _result );
                return ReentrantSetResult( _result.Result );
            }

            public IAccessorFrameState GetImplementationState( Action<IAccessorFrameInitializer> configuration )
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
            
            public IAccessorFrameState GetCallState( IReadOnlyList<Expr> arguments, Func<IAccessorFrame,IReadOnlyList<RuntimeObj>,PExpr> call )
            {
                if( ++_initCount < _realInitCount ) return null;
                if( _state == null )
                {
                    _state = new FrameState( this, null, call, arguments );
                    ++_realInitCount;
                }
                return _state;
            }

            public IAccessorFrame NextAccessor => PrevFrame as IAccessorFrame; 

            public override PExpr SetResult( RuntimeObj result )
            {
                // NextFrame is actualy the PreviousAccessor
                IAccessorFrame p = NextFrame as IAccessorFrame;
                if( p != null && !p.IsResolved ) p.SetResult( result );
                return base.SetResult( result );
            }

            public PExpr SetError( string message = null )
            {
                if( message != null ) return SetResult( new RuntimeError( Expr, message ) );
                if( NextAccessor != null )
                {
                    IAccessorFrame deepest = NextAccessor;
                    while( deepest.NextAccessor != null ) deepest = deepest.NextAccessor;
                    return SetResult( new RuntimeError( Expr, "Accessor chain not found: " + deepest.Expr.ToString(), true ) );
                }
                return SetResult( new RuntimeError( Expr, GetAccessErrorMessage(), true ) );
            }

            protected abstract string GetAccessErrorMessage();

            protected PExpr ReentrantPendingOrSignal( PExpr sub )
            {
                Debug.Assert( sub.IsPendingOrSignal );
                if( Result != null )
                {
                    Debug.Assert( Result == sub.Result );
                    return sub;
                }
                return sub.IsResolved ? SetResult( sub.Result ) : new PExpr( this, sub.PendingStatus );
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

        class AccessorMemberFrame : AccessorFrame, IAccessorMemberFrame
        {
            internal protected AccessorMemberFrame( EvalVisitor visitor, AccessorMemberExpr e )
                : base( visitor, e )
            {
            }

            public new AccessorMemberExpr Expr => (AccessorMemberExpr)base.Expr;

            public IAccessorMemberFrame PrevMemberAccessor => NextFrame as IAccessorMemberFrame; 

            protected override string GetAccessErrorMessage() => Expr.IsUnbound 
                                                                    ? "Undefined in scope: " + Expr.Name 
                                                                    : "Unknown property: " + Expr.Name;

            protected override PExpr DoVisit()
            {
                if( !_left.IsResolved )
                {
                    if( Expr.IsUnbound )
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

        class AccessorCallFrame : AccessorFrame
        {
            internal protected AccessorCallFrame( EvalVisitor visitor, AccessorCallExpr e )
                : base( visitor, e )
            {
                var declaredFunc = Expr.DeclaredFunctions;
                if( declaredFunc != null )
                {
                    foreach( var df in declaredFunc )
                    {
                        _visitor.ScopeManager.Register( df );
                    }
                }
            }

            public new AccessorCallExpr Expr => (AccessorCallExpr)base.Expr;

            protected override string GetAccessErrorMessage() => Expr.IsIndexer ? "Indexer is not supported." : "Not a function.";

            protected override void OnDispose()
            {
                var declaredFunc = Expr.DeclaredFunctions;
                if( declaredFunc != null )
                {
                    foreach( var df in declaredFunc )
                    {
                        _visitor.ScopeManager.Unregister( df );
                    }
                }
            }
        }

        public PExpr Visit( AccessorMemberExpr e ) => Run( new AccessorMemberFrame( this, e ) );

        public PExpr Visit( AccessorCallExpr e ) => Run( new AccessorCallFrame( this, e ) );

        public PExpr Visit( AccessorLetExpr e ) => new PExpr( ScopeManager.FindRegistered( e ) );
    }
}
