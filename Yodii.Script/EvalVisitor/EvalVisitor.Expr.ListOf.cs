#region LGPL License
/*----------------------------------------------------------------------------
* This file (Yodii.Script\EvalVisitor\EvalVisitor.Expr.ListOf.cs) is part of Yodii-Script. 
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
using System.Diagnostics;

using System.Collections.ObjectModel;

namespace Yodii.Script
{

    internal partial class EvalVisitor
    {
        class ListOfExprFrame : Frame<ListOfExpr>
        {
            readonly PExpr[] _statements;
            int _sCount;

            public ListOfExprFrame( EvalVisitor evaluator, ListOfExpr e )
                : base( evaluator, e )
            {
                _statements = new PExpr[e.List.Count];
            }

            protected override PExpr DoVisit()
            {
                while( _sCount < _statements.Length )
                {
                    if( IsPendingOrSignal( ref _statements[_sCount], Expr.List[_sCount] ) ) return PendingOrSignal( _statements[_sCount] );
                    ++_sCount;
                }
                return SetResult( _statements[_sCount-1].Result );
            }
        }

        public PExpr Visit( ListOfExpr e )
        {
            return Run( new ListOfExprFrame( this, e ) );
        }

    }
}
