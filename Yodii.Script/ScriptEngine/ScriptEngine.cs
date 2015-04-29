#region LGPL License
/*----------------------------------------------------------------------------
* This file (Yodii.Script\ScriptEngine\ScriptEngine.cs) is part of CiviKey. 
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
using System.Threading.Tasks;

namespace Yodii.Script
{
    public partial class ScriptEngine
    {
        readonly BreakpointManager _breakpoints;
        readonly EvalVisitor _evaluator;
        readonly GlobalContext _globalContext;
        EvaluationResult _currentResult;

        public ScriptEngine( GlobalContext ctx = null )
        {
            _breakpoints = new BreakpointManager();
            _globalContext = ctx ?? new GlobalContext();
            _evaluator = new EvalVisitor( _globalContext, true, _breakpoints.MustBreak );
        }

        public BreakpointManager Breakpoints
        {
            get { return _breakpoints; }
        }

        public GlobalContext Context
        {
            get { return _globalContext; }
        }

        public IScriptEngineResult Execute( string s )
        {
            return Execute( ExprAnalyser.AnalyseString( s ) );
        }

        public IScriptEngineResult Execute( Expr e )
        {
            if( _currentResult != null ) throw new InvalidOperationException();
            _currentResult = new EvaluationResult( this );
            _currentResult.UpdateStatus( _evaluator.VisitExpr( e ) );
            return _currentResult;
        }

        /// <summary>
        /// Simple helper to evaluate a string (typically a pure expression without side effects).
        /// </summary>
        /// <param name="s">The string to evaluate.</param>
        /// <param name="ctx">The <see cref="GlobalContext"/>. When null, a new default GlobalContext is used.</param>
        /// <returns>The result of the evaluation.</returns>
        public static RuntimeObj Evaluate( string s, GlobalContext ctx = null )
        {
            Expr e = ExprAnalyser.AnalyseString( s );
            EvalVisitor v = new EvalVisitor( ctx ?? new GlobalContext() );
            return v.VisitExpr( e ).Result;
        }
    }
}
