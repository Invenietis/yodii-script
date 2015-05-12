#region LGPL License
/*----------------------------------------------------------------------------
* This file (Yodii.Script\ScriptEngine\BreakpointManager.cs) is part of Yodii-Script. 
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
    /// Breakpoints manager enables to set breakpoints on <see cref="Expr"/> or to <see cref="BreakAlways"/>.
    /// </summary>
    public class BreakpointManager
    {
        readonly HashSet<Expr> _breakpoints;
        bool _breakAlways;

        /// <summary>
        /// Initializes a new <see cref="BreakpointManager"/>.
        /// </summary>
        public BreakpointManager()
        {
            _breakpoints = new HashSet<Expr>();
        }

        /// <summary>
        /// Gets the set of breakpoints.
        /// </summary>
        protected ISet<Expr> Breakpoints
        {
            get { return _breakpoints; }
        }

        /// <summary>
        /// Clears all breakpoints.
        /// </summary>
        public void ClearBreakpoints()
        {
            _breakpoints.Clear();
        }

        /// <summary>
        /// Gets or sets whether one should break the execution on all possible <see cref="Expr"/>.
        /// </summary>
        public bool BreakAlways
        {
            get { return _breakAlways; }
            set { _breakAlways = value; }
        }

        /// <summary>
        /// Adds a new breakpoint.
        /// </summary>
        /// <param name="e">The <see cref="Expr"/> to break on.</param>
        /// <returns>True if the breakpoint has been added. False it it was already defined or the <see cref="Expr.IsBreakable"/> is false.</returns>
        public bool AddBreakpoint( Expr e )
        {
            if( !e.IsBreakable ) return false;
            return _breakpoints.Add( e );
        }
        
        /// <summary>
        /// Removes a breakpoint.
        /// </summary>
        /// <param name="e">The <see cref="Expr"/> to no more break on.</param>
        /// <returns>True if the breakpoint has actually been removed. False it was not defined.</returns>
        public bool RemoveBreakpoint( Expr e )
        {
            if( !e.IsBreakable ) return false;
            return _breakpoints.Remove( e );
        }

        /// <summary>
        /// Checks whether the engine should break on the given <see cref="Expr"/>.
        /// </summary>
        /// <param name="e">The <see cref="Expr"/> to challenge.</param>
        /// <returns>True if the engine must break.</returns>
        public virtual bool MustBreak( Expr e )
        {
            return e.IsBreakable && (_breakAlways || _breakpoints.Contains( e ));
        }

    }
}
