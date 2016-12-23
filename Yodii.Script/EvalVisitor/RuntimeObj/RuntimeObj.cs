#region LGPL License
/*----------------------------------------------------------------------------
* This file (Yodii.Script\EvalVisitor\RuntimeObj.cs) is part of Yodii-Script. 
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

namespace Yodii.Script
{
    public abstract class RuntimeObj : IAccessorVisitor
    {
        public readonly static string TypeBoolean = "boolean";
        public readonly static string TypeNumber = "number";
        public readonly static string TypeObject = "object";
        public readonly static string TypeFunction = "function";
        public readonly static string TypeString = "string";
        public readonly static string TypeUndefined = "undefined";

        /// <summary>
        /// Simplest possible object.
        /// This is public in order to be typically used as static 
        /// internal markers in advanced scenario.
        /// The static <see cref="Undefined"/> singleton is one of them.
        /// </summary>
        public class UndefinedObj : RuntimeObj
        {            
            public override string Type => TypeUndefined; 

            public override object ToNative( GlobalContext c ) => null;

            public override string ToString() => "undefined";

            public override bool ToBoolean() => false;

            public override double ToDouble() => double.NaN;
        }
        
        class NullObj : RuntimeObj
        {
            public override string Type => TypeObject;

            public override object ToNative( GlobalContext c ) => null;

            public override bool ToBoolean() => false;

            public override double ToDouble() => 0.0;

            public override string ToString() => string.Empty;

            public override int GetHashCode() => 0;

            public override bool Equals( object obj )
            {
                return obj == null || obj == Null || obj == DBNull.Value;
            }
        }

        public static readonly RuntimeObj Undefined = new UndefinedObj();
        public static readonly RuntimeObj Null = new NullObj();

        public abstract string Type { get; }

        public abstract bool ToBoolean();

        public abstract double ToDouble();

        public abstract object ToNative( GlobalContext c ); 

        /// <summary>
        /// Resolves a potential reference: only <see cref="RefRuntimeObject"/> overrides this method, by default all 
        /// runtime objects are their own value.
        /// </summary>
        /// <returns>This object or the referenced object if this is a reference.</returns>
        public virtual RuntimeObj ToValue() => this;

        public virtual PExpr Visit( IAccessorFrame frame ) => frame.SetError();

    }

}
