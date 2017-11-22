#region LGPL License
/*----------------------------------------------------------------------------
* This file (Yodii.Script\EvalVisitor\JSEvalFunction.cs) is part of Yodii-Script. 
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
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;


namespace Yodii.Script
{
    public class ExternalObjectObj : RuntimeObj
    {
        readonly object _o;
        readonly GlobalContext _context;
        readonly List<Property> _properties;
        readonly List<Method> _methods;


        /// <summary>
        /// Indexed properties are not cached.
        /// </summary>
        public class IndexedProperty : RefRuntimeObj
        {
        }

        /// <summary>
        /// Properties are cached.
        /// </summary>
        public class Property : RefRuntimeObj
        {
            readonly ExternalObjectObj _eo;
            readonly ExternalTypeHandler.IHandler _handler;

            internal Property( ExternalObjectObj eo, ExternalTypeHandler.IHandler handler )
            {
                _eo = eo;
                _handler = handler;
            }

            public string Name => _handler.Name;

            internal PExpr Read( IAccessorFrame frame )
            {
                try
                {
                    object val = _handler.PropertyGetter( _eo._o );
                    RuntimeObj obj = val == _eo ? _eo : frame.Global.Create( val );
                    if( _handler.PropertySetter != null )
                    {
                        base.SetValue( frame.Expr, obj );
                        return frame.SetResult( this );
                    }
                    return frame.SetResult( obj );
                }
                catch( Exception ex )
                {
                    return frame.SetError( ex.Message );
                }
            }

            public override RuntimeObj SetValue( Expr e, RuntimeObj value )
            {
                Debug.Assert( _handler.PropertySetter != null );
                try
                {
                    object v = Convert.ChangeType( value.ToNative( _eo._context ), _handler.PropertyOrFieldType ); 
                    _handler.PropertySetter( _eo._o, v );
                }
                catch( Exception ex )
                {
                    return new RuntimeError( e, ex.Message );
                }
                return base.SetValue( e, value );
            }

        }

        public class Method : RuntimeObj
        {
            readonly ExternalObjectObj _eo;
            readonly ExternalTypeHandler.IHandler _handler;

            internal Method( ExternalObjectObj eo, ExternalTypeHandler.IHandler handler )
            {
                _eo = eo;
                _handler = handler;
            }

            public override string Type => TypeFunction;

            public string Name => _handler.Name;

            public override bool ToBoolean() => true;

            public override double ToDouble() => 0.0;

            public override object ToNative( GlobalContext c ) => this;

            public override PExpr Visit( IAccessorFrame frame )
            {
                AccessorCallExpr cE = frame.Expr as AccessorCallExpr;
                if( cE != null )
                {
                    var s = frame.GetCallState( cE.Arguments, DoCall );
                    if( s != null ) return s.Visit();
                }
                return frame.SetError();
            }

            PExpr DoCall( IAccessorFrame frame, IReadOnlyList<RuntimeObj> parameters )
            {
                try
                {
                    var m = _handler.FindMethod( _eo._context, parameters );
                    if( m.Method == null ) return frame.SetError( $"Method {_handler.Name} can not be called with {parameters.Count} parameters." );
                    object result = m.Method.Invoke( _eo._o, m.Parameters );
                    return m.Method.ReturnType == typeof(void) 
                            ? frame.SetResult( RuntimeObj.Undefined )
                            : frame.SetResult( _eo._context.Create( result ) );
                }
                catch( Exception ex )
                {
                    return frame.SetError( ex.Message );
                }
            }
        }

        internal ExternalObjectObj( GlobalContext c, object o )
        {
            if( o == null ) throw new ArgumentNullException();
            _o = o;
            _context = c;
            _properties = new List<Property>();
            _methods = new List<Method>();
        }

        public override string Type => RuntimeObj.TypeObject;

        public override object ToNative( GlobalContext c ) => _o;

        public override bool ToBoolean() => true;

        public override double ToDouble() => double.NaN;

        public override string ToString() => _o.ToString();

        public override bool Equals( object obj )
        {
            ExternalObjectObj o = obj as ExternalObjectObj;
            return o != null && _o.Equals( o._o );
        }

        public override int GetHashCode() => _o.GetHashCode();

        public override PExpr Visit( IAccessorFrame frame )
        {
            if( frame.Expr is AccessorCallExpr cE && cE.IsIndexer )
            {
                return frame.SetError( $"Indexer is not yet supported. Use get_Item() and set_Item() for the moment." );
            }
            else if( frame.Expr is AccessorMemberExpr mE )
            {
                bool funcRequired = frame.NextAccessor?.Expr is AccessorCallExpr a && !a.IsIndexer;
                if( funcRequired )
                {
                    ExternalTypeHandler.IHandler handler;
                    PExpr m = FindOrCreateMethod( frame, mE.Name, out handler );
                    if( m.IsUnknown )
                        m = frame.SetError( $"Member {mE.Name} on '{_o.GetType().FullName}' is not a function." );
                    return m;
                }
                else
                {
                    return FindOrCreatePropertyOrMethod( frame, mE.Name );
                }
            }
            return frame.SetError();
        }

        PExpr FindOrCreatePropertyOrMethod( IAccessorFrame frame, string name )
        {
            foreach( var p in _properties )
            {
                if( p.Name == name ) return p.Read( frame );
            }
            ExternalTypeHandler.IHandler handler;
            PExpr m = FindOrCreateMethod( frame, name, out handler );
            if( m.IsUnknown )
            {
                Debug.Assert( handler != null && handler.PropertyGetter != null );
                var prop = new Property( this, handler );
                _properties.Add( prop );
                m = prop.Read( frame );
            }
            return m;
        }

        private PExpr FindOrCreateMethod( IAccessorFrame frame, string name, out ExternalTypeHandler.IHandler handler )
        {
            handler = null;
            foreach( var m in _methods )
            {
                if( m.Name == name ) return frame.SetResult( m );
            }
            ExternalTypeHandler type = _context.FindType( _o.GetType() );
            if( type == null ) return frame.SetError( $"Unhandled type '{_o.GetType().FullName}'." );
            handler = type.GetHandler( name );
            if( handler == null ) return frame.SetError( $"Missing member {name} on '{_o.GetType().FullName}'." );
            if( handler.PropertyGetter == null )
            {
                var meth = new Method( this, handler );
                _methods.Add( meth );
                return frame.SetResult( meth );
            }
            return new PExpr();
        }

    }
}
