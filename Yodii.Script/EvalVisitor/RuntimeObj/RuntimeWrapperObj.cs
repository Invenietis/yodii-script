#region LGPL License
/*----------------------------------------------------------------------------
* This file (Yodii.Script\EvalVisitor\RefRuntimeObj.cs) is part of Yodii-Script. 
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
    public class RuntimeWrapperObj : RuntimeObj
    {
        readonly IAccessorVisitor _value;

        public RuntimeWrapperObj( IAccessorVisitor v )
        {
            if( v == null ) throw new ArgumentNullException( nameof( v ) );
            _value = v;
        }

        public override string Type => TypeObject;

        public override object ToNative( GlobalContext c ) => _value;

        public override bool ToBoolean() => true;

        public override double ToDouble() => 0.0;

        public override PExpr Visit( IAccessorFrame frame ) => _value.Visit( frame );

        public override string ToString() => _value.ToString();

    }

}
