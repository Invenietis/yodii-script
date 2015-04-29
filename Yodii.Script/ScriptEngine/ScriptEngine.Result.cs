#region LGPL License
/*----------------------------------------------------------------------------
* This file (Yodii.Script\ScriptEngine\ScriptEngine.EvaluationResult.cs) is part of CiviKey. 
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
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CK.Core;

namespace Yodii.Script
{
    public partial class ScriptEngine
    {

        /// <summary>
        /// Result of the <see cref="G:Execute"/> methods. Exposes a <see cref="Status"/> and an observable list of the stack. 
        /// Offers a simple <see cref="Continue"/> method whenever the Status is <see cref="ScriptEngineStatus.IsPending"/>.
        /// </summary>
        public class Result : IDisposable
        {
            ScriptEngine _engine;
            readonly EvalVisitor _ev;
            RuntimeObj _result;
            RuntimeError _error;
            ScriptEngineStatus _status;

            class FrameStack : ObservableCollection<IDeferredExpr>, IObservableReadOnlyList<IDeferredExpr> {}
            FrameStack _rawFrameStack;

            internal Result( ScriptEngine e )
            {
                _engine = e;
                _ev = e._evaluator;
            }

            /// <summary>
            /// Gets the result of the execution. When stepping, this is the result of the top frame that has just been resolved.
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
            /// Continue the execution of the script. Must be called only when <see cref="Status"/> is <see cref="ScriptEngineStatus.IsPending"/> otherwise
            /// an exception is thrown.
            /// </summary>
            public void Continue()
            {
                if( _engine == null ) throw new ObjectDisposedException( "EvaluationResult" );
                if( _status != ScriptEngineStatus.IsPending ) throw new InvalidOperationException();
                if( _ev.FirstFrame != null )
                {
                    UpdateStatus( _ev.FirstFrame.StepOver() );
                }
            }

            internal void UpdateStatus( PExpr r )
            {
                _error = (_result = r.Result) as RuntimeError;
                _status = ScriptEngineStatus.None;
                if( r.IsErrorResult ) _status |= ScriptEngineStatus.IsError;
                if( r.IsPending ) _status |= ScriptEngineStatus.IsPending;
                else _status |= ScriptEngineStatus.IsFinished;
                if( _rawFrameStack != null )
                {
                    int i = 0;
                    foreach( var f in _ev.Frames )
                    {
                        if( i >= _rawFrameStack.Count ) _rawFrameStack.Add( f );
                        else if( _rawFrameStack[i] != f )
                        {
                            do
                            {
                                _rawFrameStack.RemoveAt( i++ );
                            }
                            while( _rawFrameStack.Count > i );
                        }
                    }
                    while( _rawFrameStack.Count > i )
                    {
                        _rawFrameStack.RemoveAt( i );
                    }
                }
            }

            /// <summary>
            /// Gets the raw frame stack as an observable list: the stack of all current <see cref="IDeferredExpr"/> beeing evaluated.
            /// </summary>
            /// <returns>An observable list that will be dynamically updated.</returns>
            public IObservableReadOnlyList<IDeferredExpr> EnsureRawFrameStack()
            {
                if( _rawFrameStack == null )
                {
                    _rawFrameStack = new FrameStack();
                    foreach( var f in _ev.Frames )
                    {
                        _rawFrameStack.Add( f );
                    }
                }
                return _rawFrameStack;
            }

            /// <summary>
            /// Resets the current execution. This frees the <see cref="ScriptEngine"/>: new calls to its <see cref="G:ScriptEngine.Execute"/> can be made. 
            /// </summary>
            public void Dispose()
            {
                if( _engine != null )
                {
                    _ev.ResetCurrentEvaluation();
                    _engine._currentResult = null;
                    _engine = null;
                }
            }
        }
    }
}
