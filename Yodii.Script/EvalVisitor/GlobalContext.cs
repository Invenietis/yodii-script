#region LGPL License
/*----------------------------------------------------------------------------
* This file (Yodii.Script\EvalVisitor\GlobalContext.cs) is part of Yodii-Script. 
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
using System.Globalization;

namespace Yodii.Script
{
    public class GlobalContext : IAccessorVisitor
    {
        JSEvalDate _epoch;
        readonly Dictionary<Type, ExternalTypeHandler> _types;
        readonly Dictionary<string, RuntimeObj> _objects;

        public GlobalContext()
        {
            _epoch = new JSEvalDate( JSSupport.JSEpoch );
            _types = new Dictionary<Type, ExternalTypeHandler>();
            _objects = new Dictionary<string, RuntimeObj>();
        }

        /// <summary>
        /// Registers a external global object.
        /// </summary>
        /// <param name="name">Name of the object. Must be unique otherwise an exception is thrown.</param>
        /// <param name="o">The object to register. Must not be null.</param>
        public void Register( string name, object o )
        {
            if( string.IsNullOrWhiteSpace( name ) ) throw new ArgumentNullException( nameof( name ) );
            if( o == null ) throw new ArgumentNullException( nameof( o ) );
            var oR = o as RuntimeObj;
            if( oR == null )
            {
                IAccessorVisitor v = o as IAccessorVisitor;
                oR = v != null ? new RuntimeWrapperObj( v ) : Create( o );
            } 
            _objects.Add( name, oR );
        }

        [Obsolete]
        public JSEvalDate Epoch
        {
            get { return _epoch; }
        }

        [Obsolete]
        public RuntimeObj CreateBoolean( bool value )
        {
            return value ? BooleanObj.True : BooleanObj.False;
        }

        [Obsolete]
        public RuntimeObj CreateBoolean( RuntimeObj o )
        {
            if( o == null ) return BooleanObj.False;
            if( o is BooleanObj ) return o;
            return CreateBoolean( o.ToBoolean() );
        }

        [Obsolete]
        public RuntimeObj CreateNumber( double value )
        {
            return DoubleObj.Create( value );
        }

        [Obsolete]
        public RuntimeObj CreateNumber( RuntimeObj o )
        {
            if( o == null ) return DoubleObj.Zero;
            if( o is DoubleObj ) return o;
            return CreateNumber( o.ToDouble() );
        }

        [Obsolete]
        public RuntimeObj CreateString( string value )
        {
            if( value == null ) return RuntimeObj.Null;
            return StringObj.Create( value );
        }

        [Obsolete]
        public RuntimeObj CreateDateTime( DateTime value )
        {
            if( value == JSSupport.JSEpoch ) return _epoch;
            return new JSEvalDate( value );
        }

        public RuntimeObj Create( object o )
        {
            if( o == null ) return RuntimeObj.Null;
            if( o is ValueType )
            {
                if( o is int ) return DoubleObj.Create( (int)o );
                if( o is double ) return DoubleObj.Create( (double)o );
                if( o is float ) return DoubleObj.Create( (float)o );
                if( o is bool ) return (bool)o ? BooleanObj.True : BooleanObj.False;
                if( o is DateTime ) return CreateDateTime( (DateTime)o );
            }
            string s = o as string;
            if( s != null ) return StringObj.Create( s );
            return new ExternalObjectObj( this, o );
        }

        public ExternalTypeHandler FindType( Type type )
        {
            ExternalTypeHandler t;
            if( !_types.TryGetValue( type, out t ) )
            {
                t = new ExternalTypeHandler( type );
                _types.Add( type, t );
            }
            return t;
        }

        /// <summary>
        /// Default implementation of <see cref="IAccessorVisitor.Visit"/> that supports evaluation of intrinsic 
        /// functions Number(), String(), Boolean() and Date().
        /// By overriding this any binding to to external objects can be achieved (recall to call this base
        /// method when overriding).
        /// </summary>
        /// <param name="frame">The current frame (gives access to the next frame if any).</param>
        public virtual PExpr Visit( IAccessorFrame frame )
        {
            AccessorMemberExpr mE = frame.Expr as AccessorMemberExpr;
            if( mE != null )
            {
                RuntimeObj obj;
                if( _objects.TryGetValue( mE.Name, out obj ) ) return frame.SetResult( obj );
            }
            var s = frame.GetImplementationState( c =>
                c.On( "Number" ).OnCall( ( f, args ) =>
                {
                    if( args.Count == 0 ) return f.SetResult( DoubleObj.Zero );
                    return f.SetResult( CreateNumber( args[0] ) );
                }
                )
                .On( "String" ).OnCall( ( f, args ) =>
                {
                    if( args.Count == 0 ) return f.SetResult( StringObj.EmptyString );
                    return f.SetResult( StringObj.Create( args[0].ToString() ) );

                } )
                .On( "Boolean" ).OnCall( ( f, args ) =>
                {
                    return f.SetResult( args.Count == 1 && args[0].ToBoolean() ? BooleanObj.True : BooleanObj.False );
                } )
                .On( "Date" ).OnCall( ( f, args ) =>
                {
                    try
                    {
                        int[] p = new int[7];
                        for( int i = 0; i < args.Count; ++i )
                        {
                            p[i] = (int)args[i].ToDouble();
                            if( p[i] < 0 ) p[i] = 0;
                        }
                        if( p[0] > 9999 ) p[0] = 9999;
                        if( p[1] < 1 ) p[1] = 1;
                        else if( p[1] > 12 ) p[1] = 12;
                        if( p[2] < 1 ) p[2] = 1;
                        else if( p[2] > 31 ) p[2] = 31;
                        DateTime d = new DateTime( p[0], p[1], p[2], p[3], p[4], p[5], p[6], DateTimeKind.Utc );
                        return f.SetResult( CreateDateTime( d ) );
                    }
                    catch( Exception ex )
                    {
                        return f.SetError( ex.Message );
                    }
                } )
            );
            return s != null ? s.Visit() : frame.SetError();
        }
    }

}
