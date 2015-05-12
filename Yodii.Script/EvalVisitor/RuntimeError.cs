#region LGPL License
/*----------------------------------------------------------------------------
* This file (Yodii.Script\EvalVisitor\RuntimeError.cs) is part of Yodii-Script. 
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
    public class RuntimeError : RuntimeSignal
    {
        RuntimeError _next;
        RuntimeError _prev;

        public RuntimeError( Expr culprit, string message, RuntimeError previous = null )
            : base( culprit )
        {
            Message = message;
            if( previous != null )
            {
                if( previous._next != null ) throw new InvalidOperationException( "Previous error is already linked to a next error." );
                previous._next = this;
                _prev = previous;
            }
        }

        public string Message { get; private set; }

        public RuntimeError Previous { get { return _prev; } }

        public RuntimeError Origin
        {
            get
            {
                RuntimeError e = this;
                while( e._prev != null ) e = e._prev;
                return e;
            }
        }

        public override PExpr Visit( IAccessorFrame frame )
        {
            if( frame.Expr.IsMember( "message" ) ) return frame.SetResult( frame.Global.CreateString( Message ) );
            return frame.SetError();
        }

        public override string ToString()
        {
            return String.Format( "Error: {0} at {1}.", Message, Expr.Location.ToString() );
        }
    }

}
