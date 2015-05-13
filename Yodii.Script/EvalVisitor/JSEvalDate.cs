#region LGPL License
/*----------------------------------------------------------------------------
* This file (Yodii.Script\EvalVisitor\JSEvalDate.cs) is part of Yodii-Script. 
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
    public class JSEvalDate : RuntimeObj, IComparable
    {
        public static readonly string Format = "ddd, dd MMM yyyy HH':'mm':'ss 'GMT'zzz";
        public static readonly string FormatTUTC = "ddd, dd MMM yyyy HH':'mm':'ss 'UTC'";
        public static readonly string DateFormat = "ddd, dd MMM yyyy";
        public static readonly string TimeFormat = "HH':'mm':'ss 'GMT'zzz";
        
        DateTime _value;

        public JSEvalDate( DateTime value )
        {
            _value = value;
        }

        public override string Type
        {
            get { return RuntimeObj.TypeObject; }
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
            return JSSupport.ToString( _value );
        }

        //public override RuntimeObj ToPrimitive( GlobalContext c )
        //{
        //    return c.CreateString( JSSupport.ToString( _value ) );
        //}

        public override bool Equals( object obj )
        {
            if( obj == this ) return true;
            JSEvalDate d = obj as JSEvalDate;
            return d != null ? d._value == _value : false;
        }

        public override int GetHashCode()
        {
            return _value.GetHashCode();
        }

        public int CompareTo( object obj )
        {
            JSEvalDate d = obj as JSEvalDate;
            if( d != null ) return _value.CompareTo( d._value );
            if( obj is DateTime ) return _value.CompareTo( (DateTime)obj );
            throw new ArgumentException( "Must be a Date.", "obj" );
        }

        public override PExpr Visit( IAccessorFrame frame )
        {
            var s = frame.GetState( c =>
                c.On( "toString" ).OnCall( ( f, args ) =>
                {
                    return f.SetResult( f.Global.CreateString( JSSupport.ToString( _value ) ) );
                }
                ) );
            return s != null ? s.Visit() : frame.SetError();
        }

    }



}
