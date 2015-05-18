#region LGPL License
/*----------------------------------------------------------------------------
* This file (Yodii.Script\EvalVisitor\RuntimeObjComparer.cs) is part of Yodii-Script. 
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
using System.Diagnostics;

namespace Yodii.Script
{
    public struct RuntimeObjComparer
    {
        public readonly RuntimeObj X;
        public readonly RuntimeObj Y;
        public readonly bool Swapped;

        public RuntimeObjComparer( RuntimeObj x, RuntimeObj y )
        {
            if( x == RuntimeObj.Null ) x = RuntimeObj.Undefined;
            else x = x.ToValue();
            if( y == RuntimeObj.Null ) y = RuntimeObj.Undefined;
            else y = y.ToValue();
            if( (Swapped = String.CompareOrdinal( x.Type, y.Type ) > 0) )
            {
                X = y;
                Y = x;
            }
            else
            {
                X = x;
                Y = y;
            }
        }

        public bool AreEqual( GlobalContext c )
        {
            if( ReferenceEquals( X, Y ) )
            {
                return X != JSEvalNumber.NaN;
            }
            if( ReferenceEquals( X.Type, Y.Type ) )
            {
                Debug.Assert( X != RuntimeObj.Undefined && X != RuntimeObj.Null, "This has been handled by the normalization and the above reference test." );
                if( ReferenceEquals( X.Type, RuntimeObj.TypeNumber ) )
                {
                    Debug.Assert( !(((JSEvalNumber)X).IsNaN && ((JSEvalNumber)Y).IsNaN) );
                    return X.ToDouble() == Y.ToDouble();
                }
                else if( ReferenceEquals( X.Type, RuntimeObj.TypeString ) )
                {
                    return X.ToString() == Y.ToString();
                }
                else if( ReferenceEquals( X.Type, RuntimeObj.TypeBoolean ) )
                {
                    return X.ToBoolean() == Y.ToBoolean();
                }
                else
                {
                    IComparable cmp;
                    if( X.GetType() == Y.GetType() && (cmp = X as IComparable) != null )
                    {
                        Debug.Assert( (cmp.CompareTo( Y ) == 0) == X.Equals( Y ), "When IComparable is implemented, it must match Equals behavior." );
                        return cmp.Equals( Y );
                    }
                }
                return false;
            }
            if( ReferenceEquals( X.Type, RuntimeObj.TypeNumber ) && ReferenceEquals( Y.Type, RuntimeObj.TypeString ) )
            {
                return X.ToDouble() == Y.ToDouble();
            }
            if( ReferenceEquals( X.Type, RuntimeObj.TypeBoolean ) || ReferenceEquals( Y.Type, RuntimeObj.TypeBoolean ) )
            {
                return X.ToBoolean() == Y.ToBoolean();
            }
            return false;
        }

        public bool Compare( GlobalContext c, out int result )
        {
            result = 0;
            if( Y == RuntimeObj.Undefined ) return X == RuntimeObj.Undefined;

            Debug.Assert( typeof( IComparable ).IsAssignableFrom( typeof( JSEvalString ) ), "JSEvalString is Comparable." );
            Debug.Assert( typeof( IComparable ).IsAssignableFrom( typeof( JSEvalDate ) ), "JSEvalDate is Comparable." );
            
            IComparable cmp;
            if( X.GetType() == Y.GetType() && (cmp = X as IComparable) != null )
            {
                result = cmp.CompareTo( Y );
            }
            else if( X.Type == RuntimeObj.TypeNumber || Y.Type == RuntimeObj.TypeNumber )
            {
                Double xD = X.ToDouble();
                Double yD = Y.ToDouble();
                if( Double.IsNaN( xD ) || Double.IsNaN( yD ) ) return false;
                if( xD < yD ) result = -1;
                else if( xD > yD ) result = 1;
            }
            else return false;
            if( Swapped ) result = -result;
            return true;
        }

        public bool AreEqualStrict( GlobalContext c )
        {
            if( ReferenceEquals( X, Y ) )
            {
                return X != JSEvalNumber.NaN;
            }

            if( !ReferenceEquals( X.Type, Y.Type ) ) return false;
            Debug.Assert( X != RuntimeObj.Undefined && X != RuntimeObj.Null );

            if( ReferenceEquals( X.Type, RuntimeObj.TypeNumber ) )
            {
                if( X == JSEvalNumber.NaN || Y == JSEvalNumber.NaN )
                {
                    return false;
                }
                return X.ToDouble() == Y.ToDouble();
            }
            if( ReferenceEquals( X.Type, RuntimeObj.TypeString ) )
            {
                return X.ToString() == Y.ToString();
            }
            if( ReferenceEquals( X.Type, RuntimeObj.TypeBoolean ) )
            {
                return X.ToBoolean() == Y.ToBoolean();
            }
            return false;
        }
    }

}
