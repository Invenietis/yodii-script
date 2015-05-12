#region LGPL License
/*----------------------------------------------------------------------------
* This file (Tests\Yodii.Script.Tests\StaticScopeTests.cs) is part of Yodii-Script. 
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
using NUnit.Framework;
using Yodii.Script;

namespace Yodii.Script.Tests
{
    [TestFixture]
    public class StaticScopeTests
    {
        [Test]
        public void closing_a_non_opened_scope_is_a_programming_error()
        {
            StaticScope s = new StaticScope();
            s.OpenScope();
            s.CloseScope();
            Assert.Throws<InvalidOperationException>( () => s.CloseScope() );

            StaticScope sWithGlobal = new StaticScope( true );
            sWithGlobal.OpenScope();
            sWithGlobal.CloseScope();
            Assert.Throws<InvalidOperationException>( () => s.CloseScope() );
        }

        [Test]
        public void declaring_while_no_scope_is_opened_is_a_syntax_error()
        {
            StaticScope s = new StaticScope();

            var v = new AccessorLetExpr( SourceLocation.Empty, "V" );
            Assert.IsInstanceOf<SyntaxErrorExpr>( s.Declare( "toto", v ) );
            
            s.OpenScope();
            Assert.AreSame( s.Declare( "toto", v ), v );
            Assert.AreSame( s.Find( "toto" ), v );
            CheckClose( s.CloseScope(), "V" );
            
            Assert.IsInstanceOf<SyntaxErrorExpr>( s.Declare( "toto", v ) );
        }

        [Test]
        public void redefinition_in_the_same_scope_is_not_allowed_by_default()
        {
            StaticScope s = new StaticScope();

            var v = new AccessorLetExpr( SourceLocation.Empty, "V" );
            var v1 = new AccessorLetExpr( SourceLocation.Empty, "V (redefined)" );
            var v2 = new AccessorLetExpr( SourceLocation.Empty, "V (redefined again)" );
            var vFailed = new AccessorLetExpr( SourceLocation.Empty, "V (failed)" );

            s.OpenScope();

            Assert.AreSame( s.Declare( "V", v ), v );
            Assert.IsInstanceOf<SyntaxErrorExpr>( s.Declare( "V", vFailed ) );

            s.AllowLocalRedefinition = true;
            Assert.AreSame( s.Declare( "V", v1 ), v1 );
            Assert.AreSame( s.Declare( "V", v2 ), v2 );

            s.AllowLocalRedefinition = false;
            Assert.IsInstanceOf<SyntaxErrorExpr>( s.Declare( "V", vFailed ) );
            
            CheckClose( s.CloseScope(), "V", "V (redefined)", "V (redefined again)" );
        }

        [Test]
        public void declaring_in_subordinated_scopes_masks_declarations_from_upper_scope()
        {
            StaticScope s = new StaticScope();

            s.OpenScope();
            s.Declare( "V1", new AccessorLetExpr( SourceLocation.Empty, "V1 from scope n°1" ) );
            s.Declare( "V2", new AccessorLetExpr( SourceLocation.Empty, "V2 from scope n°1" ) );
            s.Declare( "V3", new AccessorLetExpr( SourceLocation.Empty, "V3 from scope n°1" ) );

            CheckDeclVarName( s, "V1", "V1 from scope n°1" );
            CheckDeclVarName( s, "V2", "V2 from scope n°1" );
            CheckDeclVarName( s, "V3", "V3 from scope n°1" );

            s.OpenScope();
            {
                s.Declare( "V1", new AccessorLetExpr( SourceLocation.Empty, "V1 from scope n°2" ) );
                CheckDeclVarName( s, "V1", "V1 from scope n°2" );
                CheckDeclVarName( s, "V2", "V2 from scope n°1" );
                CheckDeclVarName( s, "V3", "V3 from scope n°1" );

                s.OpenScope();
                {
                    s.Declare( "V1", new AccessorLetExpr( SourceLocation.Empty, "V1 from scope n°3" ) );
                    s.Declare( "V2", new AccessorLetExpr( SourceLocation.Empty, "V2 from scope n°3" ) );
                    CheckDeclVarName( s, "V1", "V1 from scope n°3" );
                    CheckDeclVarName( s, "V2", "V2 from scope n°3" );
                    CheckDeclVarName( s, "V3", "V3 from scope n°1" );

                    s.OpenScope();
                    {
                        s.Declare( "V3", new AccessorLetExpr( SourceLocation.Empty, "V3 from scope n°4" ) );
                        CheckDeclVarName( s, "V1", "V1 from scope n°3" );
                        CheckDeclVarName( s, "V2", "V2 from scope n°3" );
                        CheckDeclVarName( s, "V3", "V3 from scope n°4" );
                        CheckClose( s.CloseScope(), "V3 from scope n°4" );
                    }
                    CheckDeclVarName( s, "V1", "V1 from scope n°3" );
                    CheckDeclVarName( s, "V2", "V2 from scope n°3" );
                    CheckDeclVarName( s, "V3", "V3 from scope n°1" );
                    s.Declare( "V4", new AccessorLetExpr( SourceLocation.Empty, "V4" ) );
                    s.Declare( "V5", new AccessorLetExpr( SourceLocation.Empty, "V5" ) );
                    CheckClose( s.CloseScope(), "V1 from scope n°3", "V2 from scope n°3", "V4", "V5" );
                }
                Assert.IsNull( s.Find( "V4" ) );
                Assert.IsNull( s.Find( "V5" ) );
                CheckClose( s.CloseScope(), "V1 from scope n°2" );
            }

            CheckClose( s.CloseScope(), "V1 from scope n°1", "V2 from scope n°1", "V3 from scope n°1" );
        }

        static void CheckClose( IReadOnlyList<Expr> close, params string[] names )
        {
            CollectionAssert.AreEqual( names, close.Cast<AccessorLetExpr>().Select( e => e.Name ).ToArray() );
        }

        static void CheckDeclVarName( StaticScope s, string varName, string name )
        {
            Assert.That( ((AccessorLetExpr)s.Find( varName )).Name == name );
        }

    }
}
