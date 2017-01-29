#region LGPL License
/*----------------------------------------------------------------------------
* This file (Yodii.Script\EvalVisitor\JSEvalNumber.cs) is part of Yodii-Script. 
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
using System.Globalization;
using System.Linq;
using System.Text;


namespace Yodii.Script
{
    public class DoubleObj : RuntimeObj
    {
        static public readonly DoubleObj NaN = new DoubleObj( double.NaN );
        static public readonly DoubleObj Zero = new DoubleObj( 0.0 );
        static public readonly DoubleObj MinusOne = new DoubleObj( -1.0 );
        static public readonly DoubleObj One = new DoubleObj( 1.0 );
        static public readonly DoubleObj Two = new DoubleObj( 2.0 );
        static public readonly DoubleObj Infinity = new DoubleObj( double.PositiveInfinity );
        static public readonly DoubleObj NegativeInfinity = new DoubleObj( double.NegativeInfinity );

        readonly double _value;

        DoubleObj( double value )
        {
            _value = value;
        }

        static public DoubleObj Create( double value )
        {
            if( value == 0 ) return Zero;
            if( value == -1 ) return MinusOne;
            if( value == 1 ) return One;
            if( value == 2 ) return Two;
            if( double.IsNaN( value ) ) return NaN;
            if( double.IsPositiveInfinity( value ) ) return Infinity;
            if( double.IsNegativeInfinity( value ) ) return NegativeInfinity;
            return new DoubleObj( value );
        }

        public override string Type => RuntimeObj.TypeNumber;

        public override object ToNative( GlobalContext c ) => _value;

        public override bool ToBoolean()
        {
            return JSSupport.ToBoolean( _value );
        }

        public override double ToDouble() => _value;

        public bool IsNaN => double.IsNaN( _value ); 

        public override string ToString()
        {
            return _value.ToString( CultureInfo.InvariantCulture );
        }

        public override PExpr Visit( IAccessorFrame frame )
        {
            var s = frame.GetImplementationState( c => 
                c.On("ToString").OnCall( (f,args) => 
                {
                    int radix = 10;
                    if( args.Count == 1 ) radix = JSSupport.ToInt32( args[0].ToDouble() );
                    if( radix < 2 || radix > 36 ) return f.SetError( "Radix must be between 2 and 36." );
                    return f.SetResult( StringObj.Create( JSSupport.ToString( _value, radix ) ) );
                }
                ) );
            return s != null ? s.Visit() : frame.SetError();
        }

    }

}
