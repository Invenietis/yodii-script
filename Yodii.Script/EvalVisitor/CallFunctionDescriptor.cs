#region LGPL License
/*----------------------------------------------------------------------------
* This file (Yodii.Script\EvalVisitor\CallFunctionDescriptor.cs) is part of CiviKey. 
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
using CK.Core;

namespace Yodii.Script
{
    public struct CallFunctionDescriptor
    {
        public readonly IAccessorFrame Frame;
        public readonly IReadOnlyList<RuntimeObj> Arguments;

        public bool IsValid
        {
            get { return Frame != null; }
        }

        internal CallFunctionDescriptor( IAccessorFrame frame, IReadOnlyList<RuntimeObj> arguments )
        {
            Frame = frame;
            Arguments = arguments;
        }
    }

}
