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

        public void Register( string name, object o )
        {
            if( name == null ) throw new ArgumentNullException( nameof( name ) );
            if( o == null ) throw new ArgumentNullException( nameof( o ) );
            _objects.Add( name, Create( o ) );
        }


        public JSEvalDate Epoch
        {
            get { return _epoch; }
        }

        public RuntimeObj CreateBoolean( bool value )
        {
            return value ? BooleanObj.True : BooleanObj.False;
        }

        public RuntimeObj CreateBoolean( RuntimeObj o )
        {
            if( o == null ) return BooleanObj.False;
            if( o is BooleanObj ) return o;
            return CreateBoolean( o.ToBoolean() );
        }

        public RuntimeObj CreateNumber( double value )
        {
            return DoubleObj.Create( value );
        }

        public RuntimeObj CreateNumber( RuntimeObj o )
        {
            if( o == null ) return DoubleObj.Zero;
            if( o is DoubleObj ) return o;
            return CreateNumber( o.ToDouble() );
        }

        public RuntimeObj CreateString( string value )
        {
            if( value == null ) return RuntimeObj.Null;
            if( value.Length == 0 ) return StringObj.EmptyString;
            return new StringObj( value );
        }

        public RuntimeObj CreateString( RuntimeObj o )
        {
            if( o == null ) return RuntimeObj.Null;
            if( o is StringObj ) return o;
            return CreateString( o.ToString() );
        }

        public RuntimeObj CreateDateTime( DateTime value )
        {
            if( value == JSSupport.JSEpoch ) return _epoch;
            return new JSEvalDate( value );
        }

        public RuntimeError CreateSyntaxError( Expr e, string message, bool referenceError = false )
        {
            return new RuntimeError( e, message, referenceError );
        }

        public RuntimeError CreateAccessorSyntaxError( AccessorExpr e )
        {
            AccessorMemberExpr m = e as AccessorMemberExpr;
            if( m != null )
            {
                if( m.IsUnbound ) return CreateSyntaxError( e, "Undefined in scope: " + m.Name, referenceError: true );
                return CreateSyntaxError( e, "Unknown property: " + m.Name, referenceError: true );
            }
            if( e is AccessorIndexerExpr ) return CreateSyntaxError( e, "Indexer is not supported." );
            return CreateSyntaxError( e, "Not a function." );
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
            if( s != null ) return CreateString( s );
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
                    return f.SetResult( CreateString( args[0] ) );

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
