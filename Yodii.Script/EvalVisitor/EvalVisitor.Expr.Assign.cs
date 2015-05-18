#region LGPL License
/*----------------------------------------------------------------------------
* This file (Yodii.Script\EvalVisitor\EvalVisitor.Expr.Assign.cs) is part of Yodii-Script. 
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
        class AssignExprFrame : Frame<AssignExpr>
        {
            PExpr _left;
            PExpr _right;

            public AssignExprFrame( EvalVisitor evaluator, AssignExpr e )
                : base( evaluator, e )
            {
            }

            protected override PExpr DoVisit()
            {
                if( IsPendingOrSignal( ref _right, Expr.Right ) ) return PendingOrSignal( _right );
                if( IsPendingOrSignal( ref _left, Expr.Left ) ) return PendingOrSignal( _left );
                RefRuntimeObj r = _left.Result as RefRuntimeObj;
                if( r == null ) return SetResult( Global.CreateSyntaxError( Expr.Left, "Invalid assignment left-hand side." ) );
                r.Value = _right.Result;
                return SetResult( r.Value );
            }
        }

        public PExpr Visit( AssignExpr e )
        {
            return Run( new AssignExprFrame( this, e ) );
        }

    }
}
