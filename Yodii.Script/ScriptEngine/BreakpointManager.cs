#region LGPL License
/*----------------------------------------------------------------------------
* This file (Yodii.Script\ScriptEngine\BreakpointManager.cs) is part of CiviKey. 
*  
* CiviKey is free software: you can redistribute it and/or modify 
* it under the terms of the GNU Lesser General Public License as published 
* by the Free Software Foundation, either version 3 of the License, or 
* (at your option) any later version. 
*  
* CiviKey is distributed in the hope that it will be useful, 
* but WITHOUT ANY WARRANTY; without even the implied warranty of
* MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the 
* GNU Lesser General Public License for more details. 
* You should have received a copy of the GNU Lesser General Public License 
* along with CiviKey.  If not, see <http://www.gnu.org/licenses/>. 
*  
* Copyright © 2007-2015, 
*     Invenietis <http://www.invenietis.com>,
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
    public class BreakpointManager
    {
        readonly HashSet<Expr> _breakpoints;
        bool _breakAlways;

        public BreakpointManager()
        {
            _breakpoints = new HashSet<Expr>();
        }

        protected ISet<Expr> Breakpoints
        {
            get { return _breakpoints; }
        }

        public void ClearBreakpoints()
        {
            _breakpoints.Clear();
        }

        public bool BreakAlways
        {
            get { return _breakAlways; }
            set { _breakAlways = value; }
        }

        public bool AddBreakpoint( Expr e )
        {
            if( !e.IsBreakable ) return false;
            return _breakpoints.Add( e );
        }
        
        public bool RemoveBreakpoint( Expr e )
        {
            if( !e.IsBreakable ) return false;
            return _breakpoints.Remove( e );
        }

        public virtual bool MustBreak( Expr e )
        {
            return e.IsBreakable && (_breakAlways || _breakpoints.Contains( e ));
        }

    }
}
