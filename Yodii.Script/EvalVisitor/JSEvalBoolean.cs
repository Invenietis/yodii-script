#region LGPL License
/*----------------------------------------------------------------------------
* This file (Yodii.Script\EvalVisitor\JSEvalBoolean.cs) is part of Yodii-Script. 
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
    public class JSEvalBoolean : RuntimeObj
    {
        public static readonly JSEvalBoolean True = new JSEvalBoolean( true );
        public static readonly JSEvalBoolean False = new JSEvalBoolean( false );

        bool _value;

        JSEvalBoolean( bool v )
        {
            _value = v;
        }

        public override string Type
        {
            get { return RuntimeObj.TypeBoolean; }
        }

        public override bool ToBoolean()
        {
            return _value;
        }

        public override double ToDouble()
        {
            return _value ? 1.0 : 0.0;
        }

        public override string ToString()
        {
            return _value ? JSSupport.TrueString : JSSupport.FalseString;
        }

    }
}
