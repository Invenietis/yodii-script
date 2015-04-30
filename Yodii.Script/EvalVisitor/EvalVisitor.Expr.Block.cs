#region LGPL License
/*----------------------------------------------------------------------------
* This file (Yodii.Script\EvalVisitor\EvalVisitor.Expr.Block.cs) is part of CiviKey. 
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
using System.Diagnostics;

using System.Collections.ObjectModel;

namespace Yodii.Script
{

    public partial class EvalVisitor
    {
        class BlockExprFrame : ListOfExprFrame
        {
            public BlockExprFrame( EvalVisitor evaluator, BlockExpr e )
                : base( evaluator, e )
            {
            }

            protected override PExpr DoVisit()
            {
                foreach( var local in ((BlockExpr)Expr).Locals )
                {
                    _visitor._dynamicScope.Register( local );
                }
                return base.DoVisit();
            }

            protected override void OnDispose()
            {
                foreach( var local in ((BlockExpr)Expr).Locals )
                {
                    _visitor._dynamicScope.Unregister( local );
                }
            }
        }

        public PExpr Visit( BlockExpr e )
        {
            return new BlockExprFrame( this, e ).Visit();
        }

    }
}
