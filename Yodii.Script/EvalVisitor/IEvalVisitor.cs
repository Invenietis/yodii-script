#region LGPL License
/*----------------------------------------------------------------------------
* This file (Yodii.Script\EvalVisitor\IEvalVisitor.cs) is part of Yodii-Script. 
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

    /// <summary>
    /// A <see cref="IExprVisitor{T}"/> where T is a <see cref="PExpr"/> that is bound to a <see cref="GlobalContext"/>
    /// and exposes a <see cref="CurrentResult"/> evaluation result object and/or a <see cref="CurrentError"/>.
    /// </summary>
    public interface IEvalVisitor : IExprVisitor<PExpr>
    {
        /// <summary>
        /// Gets the <see cref="GlobalContext"/> that will be used to obtain primitive 
        /// objects (<see cref="RuntimeObj)"/>) and resolve unbound accessors.
        /// </summary>
        GlobalContext Global { get; }

    }

}
