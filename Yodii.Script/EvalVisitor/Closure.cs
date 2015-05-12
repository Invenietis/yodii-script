#region LGPL License
/*----------------------------------------------------------------------------
* This file (Yodii.Script\EvalVisitor\Closure.cs) is part of Yodii-Script. 
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
using System.Threading.Tasks;

namespace Yodii.Script
{

    /// <summary>
    /// A closure captures a variable definition and an actual <see cref="RefRuntimeObj"/> for it.
    /// </summary>
    public struct Closure
    {
        /// <summary>
        /// The declaration.
        /// </summary>
        public readonly AccessorLetExpr Variable;

        /// <summary>
        /// The current reference.
        /// </summary>
        public readonly RefRuntimeObj Ref;

        /// <summary>
        /// Initializes a new <see cref="Closure"/>.
        /// </summary>
        /// <param name="v">The variable declaration.</param>
        /// <param name="r">The bound reference.</param>
        public Closure( AccessorLetExpr v, RefRuntimeObj r )
        {
            Variable = v;
            Ref = r;
        }
    }

}
