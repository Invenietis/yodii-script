#region LGPL License
/*----------------------------------------------------------------------------
* This file (Yodii.Script\EvalVisitor\PExpr.cs) is part of Yodii-Script. 
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
    /// Promise of an <see cref="Expr"/>: either a resolved <see cref="RuntimeObj"/> or a <see cref="IDeferredExpr"/>.
    /// </summary>
    public struct PExpr
    {
        public readonly IDeferredExpr Deferred;
        public readonly RuntimeObj Result;

        public PExpr( IDeferredExpr pending )
            : this( pending, null )
        {
        }

        public PExpr( RuntimeObj resultOrSignal )
            : this( null, resultOrSignal )
        {
        }

        PExpr( IDeferredExpr pending, RuntimeObj resultOrSignal )
        {
            Deferred = pending;
            Result = resultOrSignal;
        }

        /// <summary>
        /// Gets whether this is an unitialized PExpr: its <see cref="Result"/> and <see cref="Deferred"/> are both null.
        /// </summary>
        public bool IsUnknown { get { return Result == null && Deferred == null; } }

        /// <summary>
        /// Gets whether this PExpr is resolved and <see cref="Result"/> is a <see cref="RuntimeSignal"/>.
        /// </summary>
        public bool IsSignal { get { return Result is RuntimeSignal; } }

        /// <summary>
        /// Gets whether the resolved <see cref="Result"/> is a <see cref="RuntimeError"/> (a RuntimeError is a <see cref="RuntimeSignal"/>).
        /// </summary>
        public bool IsErrorResult { get { return Result is RuntimeError; } }

        /// <summary>
        /// Gets whether the <see cref="Deferred"/> is not null (and the <see cref="Result"/> is null).
        /// </summary>
        public bool IsPending { get { return Deferred != null; } }
        
        /// <summary>
        /// Gets whether a <see cref="Result"/> exists (it can be a <see cref="RuntimeSignal"/>).
        /// </summary>
        public bool IsResolved { get { return Result != null; } }

        /// <summary>
        /// Gets whether this PExpr is pending (<see cref="Deferred"/> is not null) or <see cref="Result"/> is a <see cref="RuntimeSignal"/>.
        /// </summary>
        public bool IsPendingOrSignal { get { return Deferred != null || IsSignal; } }

        public override string ToString()
        {
            string sP = Deferred != null ? String.Format( "({0}), Expr: {1}", Deferred.GetType().Name, Deferred.Expr ) : null;
            string sR = Result != null ? String.Format( "({0}), Value = {1}", Result.GetType().Name, Result ) : null;
            return sP ?? sR ?? "(Unknown)";
        }
    }

}
