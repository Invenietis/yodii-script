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
        readonly static char[] _invalidNameChars = new char[] { ' ', '\t', '\r', '\n', '{', '}', '(', ')', '[', ']', ',', ':', ';' };

        JSEvalDate _epoch;
        readonly Dictionary<Type, ExternalTypeHandler> _types;
        readonly Dictionary<string, RuntimeObj> _objects;
        readonly HashSet<string> _namespaces;

        public GlobalContext()
        {
            _epoch = new JSEvalDate( JSSupport.JSEpoch );
            _types = new Dictionary<Type, ExternalTypeHandler>();
            _objects = new Dictionary<string, RuntimeObj>();
            _namespaces = new HashSet<string>();
        }

        /// <summary>
        /// Registers a external global object.
        /// </summary>
        /// <param name="name">
        /// Name of the object including an optional namespace prefix ("System.FileSystem.FileReader"). 
        /// Must be unique and none of its namespaces must be the same as as already registered object 
        /// otherwise an exception is thrown.
        /// Must not ne null, empty, starts or ends with a dot nor contains double dots or space.
        /// </param>
        /// <param name="o">The object to register. Must not be null.</param>
        public void Register( string name, object o )
        {
            if( o == null ) throw new ArgumentNullException( nameof( o ) );
            if( string.IsNullOrEmpty( name ) ) throw new ArgumentNullException( nameof( name ) );
            if( name.IndexOfAny( _invalidNameChars ) >= 0 ) throw new ArgumentException( "Invalid namespace (no white space, comma, parens allowed).", nameof( name ) );

            var nsName = SplitNamespace( name );
            List<string> validNamespaces = null;
            if( !string.IsNullOrEmpty( nsName.Key ) )
            {
                validNamespaces = new List<string>();
                // Checks that none of the above parents are objects.
                string ns = nsName.Key;
                int idx;
                for( ; ; )
                {
                    if( _objects.ContainsKey( ns ) ) throw new ArgumentException( $"Object '{nsName.Value}' cannot be registered in namespace '{nsName.Key}' since '{ns}' is already a registered object.", nameof( name ) );
                    validNamespaces.Add( ns );
                    idx = ns.LastIndexOf( '.' );
                    if( idx < 0 ) break;
                    if( idx == 0 || idx == ns.Length-1 ) throw new ArgumentException( "Invalid dots in namespace.", nameof( name ) );
                    ns = ns.Substring( 0, idx );
                }
            }
            if( _namespaces.Contains( name ) ) throw new ArgumentException( $"Object '{name}' is already registered as a namespace.", nameof( name ) );
            var oR = o as RuntimeObj;
            if( oR == null )
            {
                IAccessorVisitor v = o as IAccessorVisitor;
                oR = v != null ? new RuntimeWrapperObj( v ) : Create( o );
            } 
            _objects.Add( name, oR );
            // Time to register the valid namespaces if any.
            if( validNamespaces != null )
            {
                foreach( var n in validNamespaces ) _namespaces.Add( n ); 
            }
        }

        static KeyValuePair<string,string> SplitNamespace( string name )
        {
            int idx = name.LastIndexOf( '.' );
            if( idx < 0 ) return new KeyValuePair<string, string>( null, name );
            if( idx == 0 || idx == name.Length-1 ) throw new ArgumentException( "Invalid dots in namespace.", nameof( name ) );
            return new KeyValuePair<string, string>( name.Substring( 0, idx ), name.Substring( idx + 1 ) );
        }

        /// <summary>
        /// Unregisters a previously <see cref="Register"/>ed object.
        /// </summary>
        /// <param name="name">Name of the global to remove from this context.</param>
        /// <returns>False if the object was not registered.</returns>
        public bool Unregister( string name )
        {
            return _objects.Remove( name );
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
            Delegate func = o as Delegate;
            if( func != null ) return new NativeFunctionObj( func );
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
            var deepestMemberFrame = frame.NextAccessors( true )
                                            .Select( f => f as IAccessorMemberFrame )
                                            .TakeWhile( f => f != null )
                                            .LastOrDefault();
            while( deepestMemberFrame != null )
            {
                RuntimeObj obj;
                if( _objects.TryGetValue( deepestMemberFrame.Expr.MemberFullName, out obj ) )
                {
                    return deepestMemberFrame.SetResult( obj );
                }
                deepestMemberFrame = deepestMemberFrame.PrevMemberAccessor;
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
