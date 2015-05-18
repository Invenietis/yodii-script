#region LGPL License
/*----------------------------------------------------------------------------
* This file (Yodii.Script\ScriptEngine\ScriptEngineStatus.cs) is part of Yodii-Script. 
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
    /// Describes engine status.
    /// </summary>
    public enum ScriptEngineStatus
    {
        /// <summary>
        /// No current status.
        /// </summary>
        None = 0,

        /// <summary>
        /// An error has been encountered.
        /// </summary>
        IsError = 1,

        /// <summary>
        /// Execution is over.
        /// </summary>
        IsFinished = 2,
        
        /// <summary>
        /// Common bit of <see cref="Breakpoint"/>, <see cref="AsyncCall"/>, <see cref="Timeout"/>.
        /// </summary>
        IsPending = 4,
        
        /// <summary>
        /// Common bit of <see cref="Breakpoint"/>, <see cref="AsyncCall"/>, <see cref="Timeout"/>.
        /// </summary>
        CanContinue = 8,
        
        /// <summary>
        /// A breakpoint has been reached.
        /// </summary>
        Breakpoint = IsPending | CanContinue | 16,

        /// <summary>
        /// An asynchronous call is being processed.
        /// </summary>
        AsyncCall = IsPending | 32,

        /// <summary>
        /// A timeout occurred.
        /// </summary>
        Timeout = IsPending | CanContinue | 64,
        
        /// <summary>
        /// An error occurred. Execution is stopped at the point of the error.
        /// </summary>
        FirstChanceError = IsPending | IsError | CanContinue | 128
    }

}
