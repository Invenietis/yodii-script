#region LGPL License
/*----------------------------------------------------------------------------
* This file (Yodii.Script\EvalVisitor\IDeferredExpr.cs) is part of Yodii-Script. 
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
    public interface IDeferredExpr
    {
        /// <summary>
        /// Gets the expression.
        /// </summary>
        Expr Expr { get; }

        /// <summary>
        /// Gets the resolved result. Null until this deffered is resolved.
        /// </summary>
        RuntimeObj Result { get; }

        /// <summary>
        /// Gets whether a result (or an error) has been resolved.
        /// </summary>
        bool IsResolved { get; }

        /// <summary>
        /// Executes the required code until this expression is resolved.
        /// </summary>
        /// <returns>A promise that may not be resolved if a breakpoint is met.</returns>
        PExpr StepOver();

        /// <summary>
        /// Executes only one step.
        /// </summary>
        /// <returns>A promise that may be resolved if all the required code has been executed.</returns>
        PExpr StepIn();
    }
}
