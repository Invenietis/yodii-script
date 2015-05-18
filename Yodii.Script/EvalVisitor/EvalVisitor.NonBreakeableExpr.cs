#region LGPL License
/*----------------------------------------------------------------------------
* This file (Yodii.Script\EvalVisitor\EvalVisitor.NonBreakeableExpr.cs) is part of Yodii-Script. 
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
        public PExpr Visit( ConstantExpr e )
        {
            if( e.Value == null ) return new PExpr( RuntimeObj.Null );
            if( e.Value is string ) return new PExpr( _global.CreateString( (string)e.Value ) );
            if( e == ConstantExpr.UndefinedExpr ) return new PExpr( RuntimeObj.Undefined );
            if( e.Value is Double ) return new PExpr( _global.CreateNumber( (Double)e.Value ) );
            if( e.Value is Boolean ) return new PExpr( _global.CreateBoolean( (Boolean)e.Value ) );
            return new PExpr( new RuntimeError( e, "Unsupported JS type: " + e.Value.GetType().Name ) );
        }

        public PExpr Visit( SyntaxErrorExpr e )
        {
            return new PExpr( _global.CreateSyntaxError( e, e.ErrorMessage ) );
        }

        public PExpr Visit( NopExpr e )
        {
            return new PExpr( RuntimeObj.Undefined );
        }

    }
}
