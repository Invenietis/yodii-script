#region LGPL License
/*----------------------------------------------------------------------------
* This file (Yodii.Script\EvalVisitor\JSEvalFunction.cs) is part of Yodii-Script. 
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
using System.Reflection;
using System.Text;


namespace Yodii.Script
{
    public class NativeFunctionObj : RuntimeObj
    {
        readonly Delegate _function;
        readonly ParameterInfo[] _parameters;

        internal NativeFunctionObj( Delegate function )
        {
            if( function == null ) throw new ArgumentNullException( nameof( function ) );
            _function = function;
            _parameters = _function.GetMethodInfo().GetParameters();
        }

        public IReadOnlyList<ParameterInfo> Parameters => _parameters;

        public override string Type => RuntimeObj.TypeFunction;

        public override object ToNative( GlobalContext c ) => _function;

        public override bool ToBoolean() => true;

        public override double ToDouble() => double.NaN;

        public override string ToString() => _function.ToString();

        public override PExpr Visit( IAccessorFrame frame )
        {
            AccessorCallExpr cE = frame.Expr as AccessorCallExpr;
            if( cE != null )
            {
                var s = frame.GetCallState( cE.Arguments, DoCall );
                if( s != null ) return s.Visit();
            }
            return frame.SetError();
        }

        PExpr DoCall( IAccessorFrame frame, IReadOnlyList<RuntimeObj> parameters )
        {
            try
            {
                object[] p = MapCallParameters( frame.Global, parameters, _parameters );
                object result = _function.DynamicInvoke( p );
                return _function.GetMethodInfo().ReturnType == typeof( void )
                        ? frame.SetResult( RuntimeObj.Undefined )
                        : frame.SetResult( frame.Global.Create( result ) );
            }
            catch( Exception ex )
            {
                return frame.SetError( ex.Message );
            }
        }

        internal static object[] MapCallParameters( GlobalContext ctx, IReadOnlyList<RuntimeObj> parameters, IReadOnlyList<ParameterInfo> actualTypes )
        {
            var actualParameters = new object[parameters.Count];
            for( int i = 0; i < actualParameters.Length; ++i )
            {
                actualParameters[i] = Convert.ChangeType( parameters[i].ToNative( ctx ), actualTypes[i].ParameterType );
            }
            return actualParameters;
        }

    }
}
