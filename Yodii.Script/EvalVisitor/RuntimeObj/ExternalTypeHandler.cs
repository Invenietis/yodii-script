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
    public class ExternalTypeHandler
    {
        readonly Type _type;
        readonly List<IHandler> _handlers;

        internal ExternalTypeHandler( Type t )
        {
            Debug.Assert( t != null );
            _type = t;
            _handlers = new List<IHandler>();
        }

        public struct MethodCallInfo
        {
            public readonly MethodInfo Method;
            public readonly object[] Parameters;

            internal MethodCallInfo( MethodInfo m, object[] parameters )
            {
                Method = m;
                Parameters = parameters;
            }
        }

        public interface IHandler
        {
            /// <summary>
            /// Gets the external type.
            /// </summary>
            ExternalTypeHandler Holder { get; }

            /// <summary>
            /// Gets the member name.
            /// </summary>
            string Name { get; }

            /// <summary>
            /// Gets the property or field type.
            /// </summary>
            Type PropertyOrFieldType { get; }

            /// <summary>
            /// Non null getter function for a property or a field.
            /// </summary>
            Func<object, object[], object> PropertyGetter { get; }

            /// <summary>
            /// Optional setter for writable field or property.
            /// </summary>
            Action<object, object[], object> PropertySetter { get; }

            /// <summary>
            /// Finds the best method based on the parameters.
            /// </summary>
            /// <param name="ctx">Current global context.</param>
            /// <param name="parameters">Call parameters.</param>
            /// <returns>The resulting most appropriate overload and its parameters.</returns>
            MethodCallInfo FindMethod( GlobalContext ctx, IReadOnlyList<RuntimeObj> parameters );
        }

        class PropertyHandler : IHandler
        {
            public PropertyHandler( ExternalTypeHandler h, string name, PropertyInfo p )
            {
                Holder = h;
                Name = name;
                PropertyOrFieldType = p.PropertyType;
                PropertyGetter = (o,args) => p.GetValue( o, args );
                if( p.CanWrite )
                {
                    PropertySetter = ( o, args, value ) => p.SetValue( o, value, args );
                }
            }

            public PropertyHandler( ExternalTypeHandler h, string name, FieldInfo f )
            {
                Holder = h;
                Name = name;
                PropertyOrFieldType = f.FieldType;
                PropertyGetter = (o,args) => f.GetValue( o );
                if( !f.IsInitOnly )
                {
                    PropertySetter = ( o, args, value ) => f.SetValue( o, value );
                }
            }
            public ExternalTypeHandler Holder { get; }
            public string Name { get; }
            public Type PropertyOrFieldType { get; }
            public Func<object, object[], object> PropertyGetter { get; }
            public Action<object, object[], object> PropertySetter { get; }
            public MethodCallInfo FindMethod( GlobalContext ctx, IReadOnlyList<RuntimeObj> parameters) => new MethodCallInfo();
            public override string ToString() => Holder._type.FullName + '.' + Name;
        }

        class MethodGroupHandler : IHandler
        {
            readonly Method[] _methods;

            struct Method
            {
                public Method( MethodInfo m )
                {
                    M = m;
                    Parameters = m.GetParameters();
                    MinParameterCount = Parameters.Length - Parameters.Count( p => p.HasDefaultValue );
                }

                public readonly MethodInfo M;
                public readonly ParameterInfo[] Parameters;
                public readonly int MinParameterCount;
            }

            public MethodGroupHandler( ExternalTypeHandler h, string name, IEnumerable<MethodInfo> methods )
            {
                Holder = h;
                Name = name;
                _methods = methods
                            .Select( m => new Method( m ) )
                            .OrderBy( m => m.MinParameterCount ).ThenBy( m => m.Parameters.Length )
                            .GroupBy( m => m.MinParameterCount )
                            .Select( g => g.First() )
                            .ToArray();
            }

            public ExternalTypeHandler Holder { get; }
            public string Name { get; }
            public Type PropertyOrFieldType => null;
            Func<object, object[], object> IHandler.PropertyGetter => null;
            Action<object, object[], object> IHandler.PropertySetter => null;
            public MethodCallInfo FindMethod( GlobalContext ctx, IReadOnlyList<RuntimeObj> parameters )
            {
                var m = _methods.FirstOrDefault( candidate => candidate.MinParameterCount == parameters.Count );
                if( m.M == null ) return new MethodCallInfo();
                object[] actualParameters = NativeFunctionObj.MapCallParameters( ctx, parameters, m.Parameters );
                return new MethodCallInfo( m.M, actualParameters );
            }

        }

        public IHandler GetHandler( string name )
        {
            foreach( var m in _handlers )
            {
                if( m.Name == name ) return m;
            }
            IHandler newOne = null;
            MemberInfo[] members = _type.GetMember( name );
            if( members.Length > 0 )
            {
                if( members.Length == 1 )
                {
                    PropertyInfo pI = members[0] as PropertyInfo;
                    if( pI != null ) newOne = new PropertyHandler( this, name, pI );
                    else
                    {
                        FieldInfo fI = members[0] as FieldInfo;
                        if( fI != null ) newOne = new PropertyHandler( this, name, fI );
                        else
                        {
                            MethodInfo mI = members[0] as MethodInfo;
                            if( mI != null ) newOne = new MethodGroupHandler( this, name, new[] { mI } );
                        }
                    }
                }
                else newOne = new MethodGroupHandler( this, name, members.Cast<MethodInfo>() );
            }
            if( newOne != null )
            {
                _handlers.Add( newOne );
            }
            return newOne;
        }

        public override string ToString() => _type.ToString();

    }
}
