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
    /// Encapsulates chain of accessors.
    /// </summary>
    public interface IAccessorFrame
    {
        /// <summary>
        /// Gets the <see cref="AccessorExpr"/> of this frame.
        /// </summary>
        AccessorExpr Expr { get; }

        /// <summary>
        /// Gets the global context.
        /// </summary>
        GlobalContext Global { get; }

        /// <summary>
        /// Initializes an accessor state based on a configuration. Returns null if no matching configuration have been found.
        /// </summary>
        /// <param name="configuration">Configuration of resolution handlers.</param>
        /// <returns>Null if no matching configuration have been found.</returns>
        IAccessorFrameState GetState( Action<IAccessorFrameInitializer> configuration );
 
        /// <summary>
        /// Gets the next accessor if any.
        /// </summary>
        IAccessorFrame NextAccessor { get; }

        /// <summary>
        /// Resolves this frame and returns a resolved promise.
        /// </summary>
        /// <param name="result">The evaluated resulting object.</param>
        /// <returns>A resolved promise.</returns>
        PExpr SetResult( RuntimeObj result );

        /// <summary>
        /// Resolves this frame with an error and returns a resolved promise.
        /// </summary>
        /// <param name="message">
        /// An optional error message. When let to null, a default message describing the error is generated ("unknown field or property 'x'." for example).
        /// </param>
        PExpr SetError( string message = null );

        /// <summary>
        /// Gets whether this frame has been resolved: either <see cref="SetError"/> or <see cref="SetResult"/> has been called.
        /// </summary>
        bool IsResolved { get; }

    }

}
