#region LGPL License
/*----------------------------------------------------------------------------
* This file (Yodii.Script\ScriptEngine\ScriptEngine.Result.cs) is part of Yodii-Script. 
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
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Yodii.Script
{
    public partial class ScriptEngine
    {
        /// <summary>
        /// Result of the <see cref="G:Execute"/> methods. Exposes a <see cref="Status"/>, the <see cref="CurrentResult"/>, an <see cref="Error"/> and  
        /// offers a simple <see cref="Continue"/> method whenever the Status is <see cref="ScriptEngineStatus.IsPending"/>.
        /// </summary>
        public class Result : IDisposable
        {
            readonly ScriptEngine _engine;
            EvalVisitor _visitor;
            RuntimeObj _result;
            RuntimeError _error;
            ScriptEngineStatus _status;

            internal Result( ScriptEngine e )
            {
                _engine = e;
                _visitor = e._visitor;
            }

            /// <summary>
            /// Gets the result of the execution. When an execution is pending, this is the 
            /// result of the top frame that has just been resolved.
            /// </summary>
            public RuntimeObj CurrentResult
            {
                get { return _result; }
            }

            /// <summary>
            /// Gets the error that stopped the execution if any.
            /// </summary>
            public RuntimeError Error
            {
                get { return _error; }
            }

            /// <summary>
            /// Gets the current engine status. When <see cref="ScriptEngineStatus.IsPending"/>, <see cref="Continue"/> can be called.
            /// </summary>
            public ScriptEngineStatus Status
            {
                get { return _status; }
            }

            /// <summary>
            /// Gets whether <see cref="Continue"/> can be called (<see cref="Status"/> has <see cref="ScriptEngineStatus.CanContinue"/> bit set).
            /// </summary>
            public bool CanContinue
            {
                get { return (_status & ScriptEngineStatus.CanContinue) != 0; }
            }

            /// <summary>
            /// Continue the execution of the script. 
            /// Must be called only when <see cref="CanContinue"/> is true otherwise an exception is thrown.
            /// </summary>
            public void Continue()
            {
                if( _engine == null ) throw new ObjectDisposedException( "EvaluationResult" );
                if( (_status & ScriptEngineStatus.CanContinue) == 0 ) throw new InvalidOperationException();
                if( _visitor.FirstFrame != null )
                {
                    UpdateStatus( _visitor.FirstFrame.StepOver() );
                }
            }

            /// <summary>
            /// Updates the status based on the current <see cref="PExpr"/>.
            /// </summary>
            /// <param name="r">The current <see cref="PExpr"/>.</param>
            internal protected virtual void UpdateStatus( PExpr r )
            {
                _error = (_result = r.Result) as RuntimeError;
                _status = ScriptEngineStatus.None;
                if( r.AsErrorResult != null ) _status |= ScriptEngineStatus.IsError;
                if( r.IsPending )
                {
                    Debug.Assert( r.DeferredStatus != PExpr.DeferredKind.None );
                    switch( r.DeferredStatus )
                    {
                        case PExpr.DeferredKind.Timeout: _status |= ScriptEngineStatus.Timeout; break;
                        case PExpr.DeferredKind.Breakpoint: _status |= ScriptEngineStatus.Breakpoint; break;
                        case PExpr.DeferredKind.AsyncCall: _status |= ScriptEngineStatus.AsyncCall; break;
                        case PExpr.DeferredKind.FirstChanceError: _status |= ScriptEngineStatus.FirstChanceError; break;
                        default: Debug.Fail( "UpdateStatus" ); break;
                    }
                }
                else _status |= ScriptEngineStatus.IsFinished;
            }

            /// <summary>
            /// Resets the current execution. This frees the <see cref="ScriptEngine"/>: new calls to its <see cref="G:ScriptEngine.Execute"/> can be made. 
            /// </summary>
            public void Dispose()
            {
                if( _visitor != null )
                {
                    _engine.StopExecution();
                    _error = null;
                    _result = null;
                    _status = ScriptEngineStatus.None;
                    _visitor = null;
                }
            }
        }
    }
}
