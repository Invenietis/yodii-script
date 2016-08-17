#region LGPL License
/*----------------------------------------------------------------------------
* This file (Yodii.Script\EvalVisitor\DynamicScope.cs) is part of Yodii-Script. 
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
    /// Manages variables declaration and closures.
    /// </summary>
    public class DynamicScope
    {
        class Entry
        {
            public Entry Next;
            public RefRuntimeObj O;
            public Entry( Entry n, RefRuntimeObj o ) 
            {
                Debug.Assert( o != null );
                Next = n;
                O = o;
            }
        }
        readonly Dictionary<AccessorLetExpr,Entry> _vars;

        /// <summary>
        /// Initializes a new <see cref="DynamicScope"/>.
        /// </summary>
        public DynamicScope()
        {
            _vars = new Dictionary<AccessorLetExpr, Entry>();
        }

        /// <summary>
        /// Registers a local variable or a function parameter.
        /// Registering multiple times the same locals or parameters means that recursion is at work. 
        /// </summary>
        /// <param name="local">The local or parameter to register.</param>
        /// <returns>The unitialized <see cref="RefRuntimeObj"/> (undefined).</returns>
        public RefRuntimeObj Register( AccessorLetExpr local ) => Register( local, new RefRuntimeObj() );

        /// <summary>
        /// Registers an indexed variable.
        /// </summary>
        /// <param name="local">The local or parameter to register.</param>
        /// <param name="index">Index of the variable.</param>
        /// <returns>The unitialized <see cref="RefRuntimeIndexedObj"/> (undefined).</returns>
        public RefRuntimeIndexedObj Register( AccessorLetExpr local, int index ) => Register( local, new RefRuntimeIndexedObj( index ) );

        /// <summary>
        /// Registers a <see cref="Closure"/>: its actual <see cref="Closure.Ref"/> hides any current registration
        /// for its <see cref="Closure.Variable"/>.
        /// </summary>
        /// <param name="c">Closure to register.</param>
        /// <returns>The closure <see cref="RefRuntimeObj"/>.</returns>
        public virtual RefRuntimeObj Register( Closure c ) => Register( c.Variable, c.Ref );

        T Register<T>( AccessorLetExpr local, T refObj  ) where T : RefRuntimeObj
        {
            Entry e;
            if( _vars.TryGetValue( local, out e ) )
            {
                if( e.O == null ) e.O = refObj;
                else e = e.Next = new Entry( e.Next, refObj );
            }
            else _vars.Add( local, e = new Entry( null, refObj ) );
            Debug.Assert( e.O == refObj );
            return refObj;
        }

        /// <summary>
        /// Unregisters a previously registered local variable, function parameter, or <see cref="Closure"/>.
        /// </summary>
        /// <param name="decl">The declaration to unregister.</param>
        public virtual void Unregister( AccessorLetExpr decl )
        {
            Entry e;
            if( _vars.TryGetValue( decl, out e ) )
            {
                if( e.Next != null )
                {
                    e.Next = e.Next.Next;
                    return;
                }
                if( e.O != null )
                {
                    e.O = null;
                    return;
                }
            }
            throw new InvalidOperationException( $"Unregistering non registered '{decl.Name}'." );
        }
        
        /// <summary>
        /// Gets the current value for a given declaration that must have been registered at least once.
        /// </summary>
        /// <param name="r">The declaration.</param>
        /// <returns>The current <see cref="RefRuntimeObj"/> to consider.</returns>
        public RefRuntimeObj FindRegistered( AccessorLetExpr r )
        {
            Entry e;
            if( _vars.TryGetValue( r, out e ) ) return (e.Next ?? e).O;
            throw new ArgumentException( $"Unregistered variable '{r.Name}'." );
        }
    }
}
