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
            Func<object, object> PropertyGetter { get; }

            /// <summary>
            /// Optional setter for writable field or property.
            /// </summary>
            Action<object, object> PropertySetter { get; }

            /// <summary>
            /// Finds the best method based on the parameters.
            /// </summary>
            /// <param name="parameters">Call parameters.</param>
            /// <returns>Null or the most appropriate overload.</returns>
            MethodInfo FindMethod( IReadOnlyList<RuntimeObj> parameters );
        }

        class PropertyHandler : IHandler
        {
            public PropertyHandler( ExternalTypeHandler h, string name, PropertyInfo p )
            {
                Holder = h;
                Name = name;
                PropertyOrFieldType = p.PropertyType;
                PropertyGetter = o => p.GetValue( o );
                if( p.CanWrite )
                {
                    PropertySetter = ( o, value ) => p.SetValue( o, value );
                }
            }

            public PropertyHandler( ExternalTypeHandler h, string name, FieldInfo f )
            {
                Holder = h;
                Name = name;
                PropertyOrFieldType = f.FieldType;
                PropertyGetter = o => f.GetValue( o );
                if( !f.IsInitOnly )
                {
                    PropertySetter = ( o, value ) => f.SetValue( o, value );
                }
            }
            public ExternalTypeHandler Holder { get; }
            public string Name { get; }
            public Type PropertyOrFieldType { get; }
            public Func<object, object> PropertyGetter { get; }
            public Action<object, object> PropertySetter { get; }
            public MethodInfo FindMethod( IReadOnlyList<RuntimeObj> parameters ) => null;
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
                }

                public readonly MethodInfo M;
                public readonly ParameterInfo[] Parameters;
            }

            public MethodGroupHandler( ExternalTypeHandler h, string name, IEnumerable<MethodInfo> methods )
            {
                Holder = h;
                Name = name;
                _methods = methods.Select( m => new Method( m ) ).ToArray();
            }

            public ExternalTypeHandler Holder { get; }
            public string Name { get; }
            public Type PropertyOrFieldType => null;
            Func<object, object> IHandler.PropertyGetter => null;
            Action<object, object> IHandler.PropertySetter => null;
            public MethodInfo FindMethod( IReadOnlyList<RuntimeObj> parameters )
            {
                return _methods.FirstOrDefault( m => m.Parameters.Length == parameters.Count ).M;
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
