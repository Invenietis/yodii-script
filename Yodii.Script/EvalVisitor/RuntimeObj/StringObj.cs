#region LGPL License
/*----------------------------------------------------------------------------
* This file (Yodii.Script\EvalVisitor\JSEvalString.cs) is part of Yodii-Script. 
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


namespace Yodii.Script
{
    public class StringObj : RuntimeObj, IComparable
    {
        static public readonly StringObj EmptyString = new StringObj( string.Empty );

        readonly string _value;

        StringObj( string value )
        {
            if( value == null ) throw new ArgumentNullException( "value" );
            _value = value;
        }
        
        public static StringObj Create( string value )
        {
            if( value == null ) throw new ArgumentNullException( nameof( value ) );
            if( value.Length == 0 ) return EmptyString;
            return new StringObj( value );
        }

        public override string Type => TypeString;

        public override object ToNative( GlobalContext c ) => _value;

        public override bool ToBoolean() => _value.Length > 0;

        public override double ToDouble()
        {
            return JSSupport.ToNumber( _value );
        }

        public override string ToString() => _value;

        public override bool Equals( object obj )
        {
            if( obj == this ) return true;
            StringObj s = obj as StringObj;
            return s != null ? s._value == _value : false;
        }

        public override int GetHashCode() => _value.GetHashCode();

        public int CompareTo( object obj )
        {
            StringObj s = obj as StringObj;
            if( s != null ) return string.Compare( _value, s._value, StringComparison.Ordinal );
            if( obj is String ) return string.Compare( _value, (string)obj, StringComparison.Ordinal );
            throw new ArgumentException( "Must be a string.", "obj" );
        }

        public override PExpr Visit( IAccessorFrame frame )
        {
            var s = frame.GetImplementationState( c =>
                c.OnIndex( ( f, arg ) =>
                {
                    int idx = JSSupport.ToInt32( arg.ToDouble() );
                    if( idx < 0 || idx >= _value.Length ) return f.SetResult( StringObj.EmptyString );
                    return f.SetResult( StringObj.Create( new string( _value[idx], 1 ) ) );
                } )
                .On( "ToString" ).OnCall( ( f, args ) =>
                {
                    return f.SetResult( this );
                }
                ) );
            return s != null ? s.Visit() : frame.SetError();
        }

    }

}
