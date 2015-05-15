#region LGPL License
/*----------------------------------------------------------------------------
* This file (Yodii.Script\EvalVisitor\EvalVisitor.Expr.PrePostIncDec.cs) is part of Yodii-Script. 
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
        class PrePostIncDecExprFrame : Frame<PrePostIncDecExpr>
        {
            PExpr _operand;

            public PrePostIncDecExprFrame( EvalVisitor evaluator, PrePostIncDecExpr e )
                : base( evaluator, e )
            {
            }

            protected override PExpr DoVisit()
            {
                if( IsPendingOrSignal( ref _operand, Expr.Operand ) ) return PendingOrSignal( _operand );
                RefRuntimeObj r = _operand.Result as RefRuntimeObj;
                if( r == null ) return SetResult( Global.CreateSyntaxError( Expr.Operand, "Invalid increment or decrement operand." ) );
                
                var newValue = Global.CreateNumber( _operand.Result.ToDouble() + (Expr.Plus ? 1.0 : -1.0) );
                if( Expr.Prefix ) return SetResult( (r.Value = newValue) );
                var result = SetResult( r.Value );
                r.Value = newValue;
                return result;
            }
        }

        public PExpr Visit( PrePostIncDecExpr e )
        {
            return Run( new PrePostIncDecExprFrame( this, e ) );
        }

    }
}
