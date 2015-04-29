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
        class EvaluationResult : IScriptEngineResult
        {
            ScriptEngine _engine;
            readonly EvalVisitor _ev;
            RuntimeObj _result;
            RuntimeError _error;
            ScriptEngineStatus _status;

            class FrameStack : ObservableCollection<IDeferredExpr>, IObservableReadOnlyList<IDeferredExpr> {}
            FrameStack _frameStack;


            public EvaluationResult( ScriptEngine e )
            {
                _engine = e;
                _ev = e._evaluator;
            }

            public RuntimeObj Result
            {
                get { return _result; }
            }

            public RuntimeError Error
            {
                get { return _error; }
            }

            public ScriptEngineStatus Status
            {
                get { return _status; }
            }

            public void Continue()
            {
                if( _engine == null ) throw new ObjectDisposedException( "EvaluationResult" );
                if( _status != ScriptEngineStatus.IsPending ) throw new InvalidOperationException();
                if( _ev.FirstFrame != null )
                {
                    UpdateStatus( _ev.FirstFrame.StepOver() );
                }
            }

            public void UpdateStatus( PExpr r )
            {
                _error = (_result = r.Result) as RuntimeError;
                _status = ScriptEngineStatus.None;
                if( r.IsErrorResult ) _status |= ScriptEngineStatus.IsError;
                if( r.IsPending ) _status |= ScriptEngineStatus.IsPending;
                else _status |= ScriptEngineStatus.IsFinished;
            }

            public IObservableReadOnlyList<IDeferredExpr> EnsureFrameStack()
            {
                if( _frameStack == null )
                {
                    _frameStack = new FrameStack();
                    foreach( var f in _ev.Frames )
                    {
                        _frameStack.Add( f );
                    }
                }
                return _frameStack;
            }

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
