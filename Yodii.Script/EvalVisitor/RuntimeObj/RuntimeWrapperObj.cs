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
    /// <summary>
    /// Minimalist runtime object that wraps a mere <see cref="IAccessorVisitor"/>.
    /// </summary>
    public class RuntimeWrapperObj : RuntimeObj
    {
        readonly IAccessorVisitor _value;

        /// <summary>
        /// Initializes a new <see cref="RuntimeWrapperObj"/> bound to a <see cref="IAccessorVisitor"/>.
        /// </summary>
        /// <param name="v">The wrapped acess visitor.</param>
        public RuntimeWrapperObj( IAccessorVisitor v )
        {
            if( v == null ) throw new ArgumentNullException( nameof( v ) );
            _value = v;
        }

        /// <summary>
        /// This is an "object".
        /// </summary>
        public override string Type => TypeObject;

        /// <summary>
        /// Returns the wrapped <see cref="IAccessorVisitor"/>.
        /// </summary>
        /// <param name="c">The global context (unused here).</param>
        /// <returns>The access visitor.</returns>
        public override object ToNative( GlobalContext c ) => _value;

        /// <summary>
        /// Always evaluates to true.
        /// </summary>
        /// <returns>Always true.</returns>
        public override bool ToBoolean() => true;

        public override double ToDouble() => 0.0;

        public override PExpr Visit( IAccessorFrame frame ) => _value.Visit( frame );

        public override string ToString() => _value.ToString();

    }

}
