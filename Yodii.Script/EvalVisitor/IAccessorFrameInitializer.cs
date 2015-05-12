#region LGPL License
/*----------------------------------------------------------------------------
* This file (Yodii.Script\EvalVisitor\IAccessorFrameInitializer.cs) is part of Yodii-Script. 
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
    
    public interface IAccessorFrameInitializer
    {

        /// <summary>
        /// Register a member name selector.
        /// </summary>
        /// <param name="memberName">Name of the member.</param>
        /// <returns>This initializer (fluent syntax).</returns>
        IAccessorFrameInitializer On( string memberName );

        /// <summary>
        /// Registers an access index.
        /// </summary>
        /// <param name="code">Handler that must actually resolve the index access.</param>
        /// <returns>This initializer (fluent syntax).</returns>
        IAccessorFrameInitializer OnIndex( Func<IAccessorFrame, RuntimeObj, PExpr> code );

        /// <summary>
        /// Registers a call to a function.
        /// </summary>
        /// <param name="code">Handler that must actually do the call.</param>
        IAccessorFrameInitializer OnCall( Func<IAccessorFrame, IReadOnlyList<RuntimeObj>, PExpr> code );

    }

}
