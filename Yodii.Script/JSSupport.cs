#region LGPL License
/*----------------------------------------------------------------------------
* This file (Yodii.Script\JSSupport.cs) is part of Yodii-Script. 
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
    static public class JSSupport
    {
        public static readonly object Undefined = new object();

        public static readonly DateTime JSEpoch = new DateTime( 1970, 1, 1, 0, 0, 0, DateTimeKind.Utc );
        public static readonly long TicksPerMillisecond = 10000;
        public static readonly string TrueString = "true";
        public static readonly string FalseString = "false";

        public static bool ToBoolean( double v )
        {
            return v != 0 && !double.IsNaN( v );
        }

        public static bool ToBoolean( string s )
        {
            return s != null && s.Length > 0;
        }

        public static bool ToBoolean( object o )
        {
            if( o == null || o == Undefined ) return false;
            IConvertible c = o as IConvertible;
            if( c == null )
            {
                // We may handle Nullable<> here but since
                // it is a mess, I give up.
                return true;
            }
            switch( c.GetTypeCode() )
            {
                case TypeCode.Boolean: return (bool)o;
                case TypeCode.Char: return true;
                case TypeCode.String: return ToBoolean( (string)o );
                case TypeCode.DateTime: return ToBoolean( ToNumber( (DateTime)o ) );
                case TypeCode.Byte: return (byte)o != 0;
                case TypeCode.Int16: return (short)o != 0;
                case TypeCode.Int32: return (int)o != 0;
                case TypeCode.Int64: return (long)o != 0;
                case TypeCode.SByte: return (sbyte)o != 0;
                case TypeCode.UInt16: return (ushort)o != 0;
                case TypeCode.UInt32: return (uint)o != 0;
                case TypeCode.UInt64: return (ulong)o != 0;
                case TypeCode.Decimal: return (Decimal)o != 0;
                case TypeCode.Double:
                case TypeCode.Single: return ToBoolean( (double)o );
                case TypeCode.Object: return Convert.ToBoolean( o );
                default: return true;
            }
        }

        public static double ToNumber( DateTime date )
        {
            return (date.ToUniversalTime().Ticks - JSEpoch.Ticks) / TicksPerMillisecond;
        }

        public static double ToNumber( bool b )
        {
            return b ? 1.0 : 0.0;
        }

        /// <summary>
        /// Converts a double into an int, projecting any out of range values to 0 (<see cref="double.NaN"/> is always mapped to 0).
        /// Can optionally map values exceeding [<see cref="int.MinValue"/>,<see cref="int.MaxValue"/>] to the closest min/max.
        /// </summary>
        /// <param name="d">Double to convert.</param>
        /// <param name="toMinMax">True to map values exceeding [<see cref="Int32.MinValue"/>,<see cref="Int32.MaxValue"/>] to the closest min/max.</param>
        /// <returns>A signed integer.</returns>
        public static int ToInt32( double d, bool toMinMax = false )
        {
            if( double.IsNaN( d ) ) return 0;
            if( d > int.MaxValue ) return toMinMax ? int.MaxValue : 0;
            if( d < int.MinValue ) return toMinMax ? int.MinValue : 0;
            return Convert.ToInt32( d );
        }

        /// <summary>
        /// Converts a double into a long, projecting any out of range values to 0 (<see cref="Double.NaN"/> is always mapped to 0).
        /// Can optionally map values exceeding [<see cref="Int64.MinValue"/>,<see cref="Int64.MaxValue"/>] to the closest min/max.
        /// </summary>
        /// <param name="d">Double to convert.</param>
        /// <param name="toMinMax">True to map values exceeding [<see cref="Int64.MinValue"/>,<see cref="Int64.MaxValue"/>] to the closest min/max.</param>
        /// <returns>A signed long.</returns>
        public static long ToInt64( double d, bool toMinMax = false )
        {
            if( double.IsNaN( d ) ) return 0;
            if( d > long.MaxValue ) return toMinMax ? long.MaxValue : 0;
            if( d < long.MinValue ) return toMinMax ? long.MinValue : 0;
            return Convert.ToInt64( d );
        }

        /// <summary>
        /// Handles "Infinity" and "-Infinity". The string may contains leading and/or trailing white spaces and a leading negative sign.
        /// When <see cref="string.IsNullOrWhiteSpace"/> the result is 0 by default (it is <see cref="double.NaN"/> for <see cref="ParseFloat"/>). 
        /// Non parse-able numbers (or "NaN" itself) are returns as <see cref="double.NaN"/>.
        /// </summary>
        /// <param name="s">String to convert.</param>
        /// <param name="whenNullOrWhitespace">Defaults to 0.</param>
        /// <returns>The double, following javascript conversion rules.</returns>
        public static double ToNumber( string s, double whenNullOrWhitespace = 0 )
        {
            if( string.IsNullOrWhiteSpace( s ) ) return whenNullOrWhitespace;
            double r;
            if( double.TryParse( s, NumberStyles.Float, CultureInfo.InvariantCulture, out r ) )
            {
                return r;
            }
            return double.NaN;
        }

        /// <summary>
        /// Handles "Infinity" and "-Infinity". The string may contains leading and/or trailing white spaces and a leading negative sign.
        /// When <see cref="string.IsNullOrWhiteSpace"/> the result is <see cref="double.NaN"/>. 
        /// Non parse-able numbers (or "NaN" itself) are returns as <see cref="double.NaN"/>.
        /// </summary>
        /// <param name="s">String to convert.</param>
        /// <returns>The double, following javascript parseFloat rules.</returns>
        public static double ParseFloat( string s )
        {
            return ToNumber( s, double.NaN );
        }

        public static double ToNumber( object o )
        {
            if( o == null ) return 0;
            if( o == Undefined ) return Double.NaN;
            IConvertible c = o as IConvertible;
            if( c == null )
            {
                // We may handle Nullable<> here but since
                // it is a mess, I give up.
                return Double.NaN;
            }
            switch( c.GetTypeCode() )
            {
                case TypeCode.Boolean: return ToNumber( (bool)o );
                case TypeCode.Char:
                case TypeCode.String: return ToNumber( (string)o );
                case TypeCode.DateTime: return ToNumber( (DateTime)o );
                default: return Convert.ToDouble( o );
            }
        }

        public static DateTime CreateDate( double n )
        {
            return new DateTime( (long)(n * TicksPerMillisecond + JSEpoch.Ticks), DateTimeKind.Utc );
        }

        public static string ToString( bool b )
        {
            return b ? TrueString : FalseString;
        }

        public static string ToString( double v )
        {
            return v.ToString( CultureInfo.InvariantCulture );
        }

        static readonly char[] _digits36 = {
        '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 
        'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j', 
        'k', 'l', 'm', 'n', 'o', 'p', 'q', 'r', 's', 't', 
        'u', 'v', 'w', 'x', 'y', 'z' };
        
        public static string ToString( double v, int radix )
        {
            if( radix < 2 || radix > 36 ) throw new ArgumentOutOfRangeException( "radix" );
            if( radix == 10 ) return v.ToString( CultureInfo.InvariantCulture );
            if( Double.IsNaN( v ) ) return "NaN";
            if( Double.IsInfinity( v ) ) return "Infinity";
            if( Double.IsNegativeInfinity( v ) ) return "-Infinity";

            Int64 l = (Int64)v;
            Int64 p = Math.Abs( l );
            if( p == 0 ) return "0";

            char[] digits = new char[64];
            int i = 64;
            while( --i > 0 )
            {
                digits[i] = _digits36[p % radix];
                if( (p /= radix) == 0 ) break;
            }
            if( l < 0 ) digits[--i] = '-';
            return new String( digits, i, 64-i );
        }

    }
}
