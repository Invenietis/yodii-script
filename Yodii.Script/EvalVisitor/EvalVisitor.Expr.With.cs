using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Yodii.Script
{
    internal partial class EvalVisitor
    {
        /// <summary>
        /// This class implements a "fake" IAccessorMemberFrame on a real one but without the next accessor.
        /// This is used to lookup the existence of a member name via IAccessorVisitor.Visit( IAccessorFrame frame )
        /// without the need to support an explicit lookup method on members.
        /// </summary>
        internal class AccessorMemberFrameLookup : Frame, IAccessorMemberFrame
        {
            readonly RuntimeObj _o;
            readonly AccessorMemberFrame _f;
            PExpr _lookup;

            AccessorMemberFrameLookup( EvalVisitor visitor, RuntimeObj o, AccessorMemberFrame f )
                : base( visitor, f.Expr )
            {
                _o = o;
                _f = f;
            }

            public new AccessorMemberExpr Expr => _f.Expr;

            public IAccessorFrame NextAccessor => null;

            public IAccessorMemberFrame PrevMemberAccessor => null;

            AccessorExpr IAccessorFrame.Expr => _f.Expr;

            public IAccessorFrameState GetCallState( IReadOnlyList<Expr> arguments, Func<IAccessorFrame, IReadOnlyList<RuntimeObj>, PExpr> call )
            {
                return null;
            }

            public IAccessorFrameState GetImplementationState( Action<IAccessorFrameInitializer> configuration )
            {
                return null;
            }

            public PExpr SetError( string message = null )
            {
                return SetResult( new RuntimeError( Expr, message ?? _f.GetAccessErrorMessage() ) );
            }

            protected override PExpr DoVisit()
            {
                if( _lookup.IsResolved ) return _lookup;
                if( (_lookup = _o.Visit( this )).IsPendingOrSignal ) return PendingOrSignal( _lookup );
                return SetResult( _lookup.Result );
            }
        }

        class WithExprFrame : Frame<WithExpr>
        {
            PExpr _obj;
            PExpr _code;
            IDisposable _withScope;

            public WithExprFrame( EvalVisitor evaluator, WithExpr e )
                : base( evaluator, e )
            {
            }

            protected override PExpr DoVisit()
            {
                if( IsPendingOrSignal( ref _obj, Expr.Obj ) ) return PendingOrSignal( _obj );
                if( _withScope == null ) _withScope = Global.OpenWithScope( _obj.Result );
                if( IsPendingOrSignal( ref _code, Expr.Code ) ) return PendingOrSignal( _code );
                return SetResult( _code.Result );
            }

            protected override void OnDispose()
            {
                if( _withScope != null ) _withScope.Dispose();
            }
        }


        public PExpr Visit( WithExpr e )
        {
            return Run( new WithExprFrame( this, e ) );
        }
    }
}
