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
    public class JSEvalString : RuntimeObj, IComparable
    {
        static public readonly JSEvalString EmptyString = new JSEvalString( String.Empty );

        readonly string _value;

        public JSEvalString( string value )
        {
            if( value == null ) throw new ArgumentNullException( "value" );
            _value = value;
        }

        public override string Type
        {
            get { return RuntimeObj.TypeString; }
        }

        public override bool ToBoolean()
        {
            return JSSupport.ToBoolean( _value );
        }

        public override double ToDouble()
        {
            return JSSupport.ToNumber( _value );
        }

        public override string ToString()
        {
            return _value;
        }

        public override bool Equals( object obj )
        {
            if( obj == this ) return true;
            JSEvalString s = obj as JSEvalString;
            return s != null ? s._value == _value : false;
        }

        public override int GetHashCode()
        {
            return _value.GetHashCode();
        }

        public int CompareTo( object obj )
        {
            JSEvalString s = obj as JSEvalString;
            if( s != null ) return String.Compare( _value, s._value, StringComparison.InvariantCulture );
            if( obj is String ) return String.Compare( _value, (String)obj, StringComparison.InvariantCulture );
            throw new ArgumentException( "Must be a string.", "obj" );
        }

        public override PExpr Visit( IAccessorFrame frame )
        {
            var s = frame.GetState( c =>
                c.On( "charAt" ).OnCall( ( f, args ) =>
                {
                    int idx = args.Count > 0 ? JSSupport.ToInt32( args[0].ToDouble() ) : 0;
                    if( idx < 0 || idx >= _value.Length ) return f.SetResult( JSEvalString.EmptyString );
                    return f.SetResult( f.Global.CreateString( new String( _value[idx], 1 ) ) );
                } )
                .On( "toString" ).OnCall( ( f, args ) =>
                {
                    return f.SetResult( this );
                }
                ) );
            return s != null ? s.Visit() : frame.SetError();
        }

    }

}
