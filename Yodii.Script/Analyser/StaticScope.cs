#region LGPL License
/*----------------------------------------------------------------------------
* This file (Yodii.Script\Analyser\StaticScope.cs) is part of Yodii-Script. 
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
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Yodii.Script
{
    /// <summary>
    /// Manages variables scopes during analysis.
    /// Declaration and retrieving are O(1).
    /// </summary>
    public class StaticScope
    {
        class Scope
        {
            public Scope NextScope;
            public readonly Scope StrongScope;
            NameEntry _firstNamed;
            int _count;
            HashSet<AccessorLetExpr> _closures;

            public Scope( Scope next, Scope currentStrongScope )
            {
                NextScope = next;
                StrongScope = currentStrongScope ?? this;
            }

            public bool IsStrong
            {
                get { return StrongScope == this; }
            }

            internal void Add( NameEntry newOne, NameEntry first )
            {
                Debug.Assert( first == newOne || first.Next == newOne );
                newOne.Scope = this;
                newOne.NextInScope = _firstNamed;
                _firstNamed = first;
                ++_count;
            }

            internal IReadOnlyList<AccessorLetExpr> RetrieveValues( StaticScope container, bool close, int skipCount = 0 )
            {
                if( skipCount < 0 ) throw new ArgumentException( "skipCount" );
                int i = _count - skipCount;
                if( i <= 0 && !close ) return AccessorLetExpr.EmptyArray;
                var all = i > 0 ? new AccessorLetExpr[i] : AccessorLetExpr.EmptyArray;
                NameEntry first = _firstNamed;
                while( first != null )
                {
                    NameEntry e = first.Next ?? first;
                    Debug.Assert( e.E != null );
                    if( i > 0 ) all[--i] = e.E;
                    if( close ) container.Unregister( first );
                    first = e.NextInScope;
                }
                return all;
            }

            internal void AddClosure( AccessorLetExpr a )
            {
                if( _closures == null ) _closures = new HashSet<AccessorLetExpr>();
                _closures.Add( a );
            }

            internal IReadOnlyList<AccessorLetExpr> GetClosures()
            {
                return _closures != null ? _closures.ToArray() : AccessorLetExpr.EmptyArray;
            }
        }

        class NameEntry
        {
            /// <summary>
            /// Next entry for the same name. 
            /// </summary>
            public NameEntry Next;
            
            /// <summary>
            /// Next entry in the same scope.
            /// </summary>
            public NameEntry NextInScope;
            
            /// <summary>
            /// The declared expression. Null if first declaration has been scoped out.
            /// </summary>
            public AccessorLetExpr E;

            /// <summary>
            /// This is required to support AllowLocalRedefinition = false in O(1).
            /// </summary>
            public Scope Scope;

            public NameEntry( NameEntry next, AccessorLetExpr e )
            {
                Next = next;
                E = e;
            }
        }

        Scope _firstScope;
        readonly Dictionary<string,NameEntry> _vars;
        readonly bool _globalScope;
        Scope _currentStrongScope;
        bool _allowMasking;
        bool _disallowRegistration;
        bool _allowLocalRedefinition;

        /// <summary>
        /// Initializes a new <see cref="StaticScope"/>.
        /// </summary>
        /// <param name="globalScope">
        /// True to share declarations in a global default scope. By default, <see cref="OpenScope"/> must be called before calling <see cref="Declare"/>.
        /// </param>
        /// <param name="allowMasking">
        /// False to forbid masking (to work like in C#). By default declaration in a subordinated scope masks any declaration from upper levels (Javascript).
        /// </param>
        /// <param name="allowLocalRedefinition">
        /// True to allow redefinition of a name in the same scope (masking but in the currenly opened scope).
        /// This is allowed in javascript even with "use strict" for 'var' (but not for 'let' or 'const').
        /// It defaults to false: this a dangerous and useless "feature".
        /// </param>
        public StaticScope( bool globalScope = false, bool allowMasking = true, bool allowLocalRedefinition = false )
        {
            _vars = new Dictionary<string, NameEntry>();
            _allowMasking = allowMasking;
            _allowLocalRedefinition = allowLocalRedefinition;
            _globalScope = globalScope;
            if( _globalScope ) _firstScope = new Scope( null, null );
        }

        /// <summary>
        /// Gets or sets whether masking is allowed (like in Javascript). 
        /// When masking is disallowed (like in C#), registering new entries returns a <see cref="SyntaxErrorExpr"/>
        /// instead of the registered expression.
        /// </summary>
        public bool AllowMasking
        {
            get { return _allowMasking; }
            set { _allowMasking = true; }
        }

        /// <summary>
        /// True to allow redifinition of a name in the same scope. 
        /// This is allowed in javascript even with "use strict" but here it defaults to false since I consider this a dangerous and useless feature.
        /// </summary>
        public bool AllowLocalRedefinition
        {
            get { return _allowLocalRedefinition; }
            set { _allowLocalRedefinition = value; }
        }

        /// <summary>
        /// Disallow any new registration.
        /// Defaults to false (typically sets to true to evaluate pure functions).
        /// </summary>
        public bool DisallowRegistration
        {
            get { return _disallowRegistration; }
            set { _disallowRegistration = true; }
        }

        /// <summary>
        /// Gets whether a global scope is opened.
        /// </summary>
        public bool GlobalScope
        {
            get { return _globalScope; }
        }

        /// <summary>
        /// Declares an expression in the current scope. Returns either the given <see cref="AccessorLetExpr"/>
        /// or a <see cref="SyntaxErrorExpr"/>.
        /// </summary>
        /// <param name="e">The <see cref="AccessorLetExpr"/> to register.</param>
        /// <returns>The registered accessor or a syntax error if it can not be registered.</returns>
        public Expr Declare( AccessorLetExpr e )
        {
            if( _firstScope == null ) return new SyntaxErrorExpr( e.Location, "Invalid declaration (a scope must be opened first)." );
            if( _disallowRegistration ) return new SyntaxErrorExpr( e.Location, "Invalid declaration." );
            var curScope = _firstScope.NextScope ?? _firstScope;
            NameEntry first, newOne;
            if( _vars.TryGetValue( e.Name, out first ) )
            {
                if( first.E == null )
                {
                    first.E = e;
                    newOne = first;
                }
                else
                {
                    var cur = first.Next ?? first;
                    if( _allowMasking || cur.Scope.StrongScope != _currentStrongScope )
                    {
                        if( _allowLocalRedefinition || cur.Scope != curScope )
                        {
                            first.Next = newOne = new NameEntry( first.Next, e );
                        }
                        else
                        {
                            return new SyntaxErrorExpr( e.Location, "Declaration of '{1}' conflicts with declaration at {0}.", first.E.Location, e.Name );
                        }
                    }
                    else
                    {
                        return new SyntaxErrorExpr( e.Location, "Masking is not allowed: declaration of '{1}' conflicts with declaration at {0}.", first.E.Location, e.Name );
                    }
                }
            }
            else _vars.Add( e.Name, (first = newOne = new NameEntry( null, e )) );
            curScope.Add( newOne, first );
            return e;
        }

        void Unregister( NameEntry first )
        {
            if( first.Next != null )
            {
                first.Next = first.Next.Next;
            }
            else if( first.E != null )
            {
                first.E = null;
            }
        }

        /// <summary>
        /// Opens a new scope (a weak one): any <see cref="Declare"/> will be done in this new scope.
        /// </summary>
        public void OpenScope()
        {
            if( _firstScope == null ) _currentStrongScope = _firstScope = new Scope( null, null );
            else _firstScope.NextScope = new Scope( _firstScope.NextScope, _currentStrongScope );
        }

        /// <summary>
        /// Opens a new strong scope: any <see cref="Declare"/> will be done in this new scope and all access to variables above it
        /// will register the closure.
        /// </summary>
        public void OpenStrongScope()
        {
            if( _firstScope == null ) _currentStrongScope = _firstScope = new Scope( null, null );
            else
            {
                _firstScope.NextScope = new Scope( _firstScope.NextScope, null );
                _currentStrongScope = _firstScope.NextScope;
            }
        }

        /// <summary>
        /// Closes the current scope (be it strong or not) and returns all the declared variables in the order of their declarations, optionnaly skipping the first ones.
        /// </summary>
        /// <returns>The declared expressions (an empty list if nothing has been declared or skipCount is too big).</returns>
        public IReadOnlyList<AccessorLetExpr> CloseScope( int skipCount = 0 )
        {
            if( _firstScope == null ) throw new InvalidOperationException( "No Scope opened." );
            if( _firstScope != null && _firstScope.NextScope == null && _globalScope ) throw new InvalidOperationException( "The GlobalScope can not be closed." );
            Scope closing;
            if( _firstScope.NextScope == null )
            {
                Debug.Assert( _currentStrongScope == _firstScope, "Root is always a Strong scope by design." );
                closing = _firstScope;
                _firstScope = null;
            }
            else
            {
                closing = _firstScope.NextScope;
                _firstScope.NextScope = closing.NextScope;
                if( _firstScope.NextScope != null ) _currentStrongScope = _firstScope.NextScope.StrongScope;
                else _currentStrongScope = _firstScope;
            }
            return closing.RetrieveValues( this, true, skipCount );
        }

        /// <summary>
        /// Closes the current strong scope and returns the closures (in the key) and all the declared expressions in the order of their declarations (in the value).
        /// If the current one is not a strong one, an exception is thrown.
        /// </summary>
        /// <param name="skipLocalCount">Number of locals to skip (typically already obtained and handled thanks to a previous call to <see cref="GetCurrent"/>).</param>
        /// <returns>The declared expressions (an empty list if nothing has been declared).</returns>
        public KeyValuePair<IReadOnlyList<AccessorLetExpr>, IReadOnlyList<AccessorLetExpr>> CloseStrongScope( int skipLocalCount = 0 )
        {
            if( _firstScope == null ) throw new InvalidOperationException( "No Scope opened." );
            var curScope = _firstScope.NextScope ?? _firstScope;
            if( !curScope.IsStrong ) throw new InvalidOperationException( "The scope is not a strong one." );
            var closures = curScope.GetClosures();
            return new KeyValuePair<IReadOnlyList<AccessorLetExpr>, IReadOnlyList<AccessorLetExpr>>( closures, CloseScope( skipLocalCount ) );
        }

        /// <summary>
        /// Gets the global scope content.
        /// </summary>
        /// <returns>The global scope. Empty if GlobalScope is false (or if nothing has been declared at the global scope).</returns>
        public IReadOnlyList<AccessorLetExpr> Globals
        {
            get { return _firstScope == null ? AccessorLetExpr.EmptyArray : _firstScope.RetrieveValues( this, false ); }
        }

        /// <summary>
        /// Obtains a named <see cref="Expr"/> if it exists. Null otherwise.
        /// This does not track closure.
        /// </summary>
        /// <param name="name">Name in the scope.</param>
        /// <returns>Null if not found.</returns>
        public AccessorLetExpr Find( string name )
        {
            NameEntry t;
            if( _vars.TryGetValue( name, out t ) ) return (t.Next ?? t).E;
            return null;
        }

        /// <summary>
        /// Like <see cref="Find"/> but if the variable belongs to another strong scope than the current one, it is registered
        /// in the Closures of its strong scope.
        /// </summary>
        /// <param name="name">Name in the scope.</param>
        /// <returns>Null if not found.</returns>
        public AccessorLetExpr FindAndRegisterClosure( string name )
        {
            NameEntry t;
            if( _vars.TryGetValue( name, out t ) )
            {
                if( t.Next != null ) t = t.Next;
                if( t.Scope.StrongScope != _currentStrongScope )
                {
                    _currentStrongScope.AddClosure( t.E );
                }
                return t.E;
            }
            return null;
        }

        /// <summary>
        /// Gets the variables registered in the current scope so far, optionnaly skipping the first ones.
        /// </summary>
        public IReadOnlyList<AccessorLetExpr> GetCurrent( int skipCount = 0 )
        {
            return _firstScope == null ? AccessorLetExpr.EmptyArray : (_firstScope.NextScope ?? _firstScope).RetrieveValues( this, false, skipCount );
        }
    }

}
