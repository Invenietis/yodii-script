#region LGPL License
/*----------------------------------------------------------------------------
* This file (Yodii.Script\Tokenizer\SourceLocation.cs) is part of CiviKey. 
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
    public struct SourceLocation
    {
        public const string NoSource = "(no source)";

        public static readonly SourceLocation Empty = new SourceLocation() { Source = NoSource };

        public string Source;
        public int Line;
        public int Column;

        public override int GetHashCode()
        {
            return Util.Hash.Combine( Util.Hash.StartValue, Source, Line, Column ).GetHashCode();
        }

        public override bool Equals( object obj )
        {
            if( obj is SourceLocation )
            {
                SourceLocation other = (SourceLocation)obj;
                return Line == other.Line && Column == other.Column && Source == other.Source;
            }
            return false;
        }

        public override string ToString()
        {
            return String.Format( "{0} - line {1}, column {2}", Source, Line, Column );
        }
    }
}
