#region LGPL License
/*----------------------------------------------------------------------------
* This file (Yodii.Script\EvalVisitor\IAccessorFrame.cs) is part of Yodii-Script. 
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
    /// Exposes its Expression as a <see cref="AccessorMemberExpr"/> and the <see cref="PrevMemberAccessor"/> to ease
    /// access to <see cref="AccessorMemberExpr.Name"/>, <see cref="AccessorMemberExpr.MemberFullName"/> and
    /// member accessor chain.
    /// This is currently the only specialized <see cref="IAccessorFrame"/> (there is nothing specific 
    /// in index or call accessor frames). 
    /// </summary>
    public interface IAccessorMemberFrame : IAccessorFrame
    {
        /// <summary>
        /// Gets the <see cref="AccessorExpr"/> of this frame.
        /// </summary>
        new AccessorMemberExpr Expr { get; }

        /// <summary>
        /// Gets the previous <see cref="IAccessorMemberFrame"/> if the actual next frame
        /// is also a member access. See remarks.
        /// </summary>
        /// <remarks>
        /// The fact that the next frame is actually the previous accessor (and vice versa) is 
        /// the key to really understand how (and why) accessor chains work in Yodii.Script...
        /// </remarks>
        IAccessorMemberFrame PrevMemberAccessor { get; }
    }

}
