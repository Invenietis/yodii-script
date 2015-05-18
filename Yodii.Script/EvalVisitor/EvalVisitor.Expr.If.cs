#region LGPL License
/*----------------------------------------------------------------------------
* This file (Yodii.Script\EvalVisitor\EvalVisitor.Expr.If.cs) is part of Yodii-Script. 
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
        class IfExprFrame : Frame<IfExpr>
        {
            PExpr _condition;
            PExpr _whenTrue;
            PExpr _whenFalse;

            public IfExprFrame( EvalVisitor evaluator, IfExpr e )
                : base( evaluator, e )
            {
            }

            protected override PExpr DoVisit()
            {
                if( IsPendingOrSignal( ref _condition, Expr.Condition ) ) return PendingOrSignal( _condition );

                if( _condition.Result.ToBoolean() )
                {
                    if( IsPendingOrSignal( ref _whenTrue, Expr.WhenTrue ) ) return PendingOrSignal( _whenTrue );
                    return SetResult( _whenTrue.Result );
                }

                if( Expr.WhenFalse != null )
                {
                    if( IsPendingOrSignal( ref _whenFalse, Expr.WhenFalse ) ) return PendingOrSignal( _whenFalse );
                    return SetResult( _whenFalse.Result );
                }
                return SetResult( RuntimeObj.Undefined );
            }
        }

        public PExpr Visit( IfExpr e )
        {
            return Run( new IfExprFrame( this, e ) );
        }
    }
}
