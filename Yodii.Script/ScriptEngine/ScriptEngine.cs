#region LGPL License
/*----------------------------------------------------------------------------
* This file (Yodii.Script\ScriptEngine\ScriptEngine.cs) is part of Yodii-Script. 
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

namespace Yodii.Script
{
    /// <summary>
    /// Main object of the evaluation processus.
    /// </summary>
    public partial class ScriptEngine
    {
        readonly BreakpointManager _breakpoints;
        readonly EvalVisitor _visitor;
        readonly GlobalContext _globalContext;
        Result _currentResult;

        /// <summary>
        /// Initializes a new <see cref="ScriptEngine"/>, optionally bound to an existing <see cref="GlobalContext"/>.
        /// </summary>
        /// <param name="ctx">Optional global context to use.</param>
        /// <param name="breakPointManager">Optional <see cref="BreakpointManager"/> instance to use.</param>
        /// <param name="scopeManager">Optional <see cref="DynamicScope"/> to use.</param>
        public ScriptEngine( GlobalContext ctx = null, BreakpointManager breakPointManager = null, DynamicScope scopeManager = null )
        {
            _breakpoints = breakPointManager ?? new BreakpointManager();
            _globalContext = ctx ?? new GlobalContext();
            _visitor = new EvalVisitor( _globalContext, _breakpoints.MustBreak, scopeManager );
        }

        /// <summary>
        /// Gets the breakpoint manager that is used by this engine.
        /// </summary>
        public BreakpointManager Breakpoints
        {
            get { return _breakpoints; }
        }

        /// <summary>
        /// Gets the <see cref="DynamicScope"/>.
        /// </summary>
        protected DynamicScope ScopeManager
        {
            get { return _visitor.ScopeManager; }
        }

        /// <summary>
        /// Gets the <see cref="GlobalContext"/>.
        /// </summary>
        public GlobalContext Context
        {
            get { return _globalContext; }
        }

        /// <summary>
        /// Gets or sets whether the engine should break whenever a runtime error occurred.
        /// </summary>
        public virtual bool EnableFirstChanceError
        {
            get { return _visitor.EnableFirstChanceError; }
            set { _visitor.EnableFirstChanceError = value; }
        }

        /// <summary>
        /// Gets whether this engine is currently executing a script.
        /// </summary>
        public bool IsExecuting
        {
            get { return _currentResult != null; }
        }

        /// <summary>
        /// Executes a string by first calling <see cref="ExprAnalyser.AnalyseString"/>.
        /// </summary>
        /// <param name="s">The string to execute.</param>
        /// <returns>A result that may be pending...</returns>
        public Result Execute( string s )
        {
            return Execute( ExprAnalyser.AnalyseString( s ) );
        }

        /// <summary>
        /// Executes an already analysed script.
        /// </summary>
        /// <param name="s">The string to execute.</param>
        /// <returns>A result that may be pending...</returns>
        public Result Execute( Expr e )
        {
            if( _currentResult != null ) throw new InvalidOperationException();
            _currentResult = StartExecution();
            _currentResult.UpdateStatus( _visitor.VisitExpr( e ) );
            return _currentResult;
        }

        /// <summary>
        /// Starts a new execution: this creates a <see cref="Result"/> object (or a specialization of it) that will
        /// enable interactions during execution.
        /// </summary>
        /// <returns>A <see cref="Result"/> object for this engine.</returns>
        protected virtual Result StartExecution()
        {
            return new Result( this );
        }

        internal void StopExecution()
        {
            OnStopExecution();
            if( _currentResult == null ) throw new InvalidOperationException();
            _visitor.ResetCurrentEvaluation();
            _currentResult = null;
        }

        /// <summary>
        /// Called before the execution stops.
        /// </summary>
        protected virtual void OnStopExecution()
        {
        }

        /// <summary>
        /// Simple static helper to evaluate a string (typically a pure expression without side effects).
        /// </summary>
        /// <param name="s">The string to evaluate.</param>
        /// <param name="ctx">The <see cref="GlobalContext"/>. When null, a new default GlobalContext is used.</param>
        /// <returns>The result of the evaluation.</returns>
        public static RuntimeObj Evaluate( string s, GlobalContext ctx = null )
        {
            return Evaluate( ExprAnalyser.AnalyseString( s ) );
        }

        /// <summary>
        /// Simple static helper to evaluate an expression (typically a pure expression without side effects).
        /// </summary>
        /// <param name="e">The expression to evaluate.</param>
        /// <param name="ctx">The <see cref="GlobalContext"/>. When null, a new default GlobalContext is used.</param>
        /// <returns>The result of the evaluation.</returns>
        public static RuntimeObj Evaluate( Expr e, GlobalContext ctx = null )
        {
            EvalVisitor v = new EvalVisitor( ctx ?? new GlobalContext() );
            return v.VisitExpr( e ).Result;
        }
    }
}
