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
            Assert.IsInstanceOf<SyntaxErrorExpr>( s.Declare( v ) );
            
            s.OpenScope();
            Assert.AreSame( s.Declare( v ), v );
            Assert.AreSame( s.Find( "V" ), v );
            CheckClose( s.CloseScope(), v );
            
            Assert.IsInstanceOf<SyntaxErrorExpr>( s.Declare( v ) );
        }

        [Test]
        public void redefinition_in_the_same_scope_is_not_allowed_by_default()
        {
            StaticScope s = new StaticScope();

            var v = new AccessorLetExpr( SourceLocation.Empty, "V" );
            var v1 = new AccessorLetExpr( SourceLocation.Empty, "V" );
            var v2 = new AccessorLetExpr( SourceLocation.Empty, "V" );
            var vFailed = new AccessorLetExpr( SourceLocation.Empty, "V" );

            s.OpenScope();

            Assert.AreSame( s.Declare( v ), v );
            Assert.IsInstanceOf<SyntaxErrorExpr>( s.Declare( vFailed ) );

            s.AllowLocalRedefinition = true;
            Assert.AreSame( s.Declare( v1 ), v1 );
            Assert.AreSame( s.Declare( v2 ), v2 );

            s.AllowLocalRedefinition = false;
            Assert.IsInstanceOf<SyntaxErrorExpr>( s.Declare( vFailed ) );
            
            CheckClose( s.CloseScope(), v, v1, v2 );
        }

        [Test]
        public void declaring_in_subordinated_scopes_masks_declarations_from_upper_scope()
        {
            StaticScope s = new StaticScope();

            s.OpenScope();
            var v1From1 = (AccessorLetExpr)s.Declare( new AccessorLetExpr( SourceLocation.Empty, "V1" ) );
            var v2From1 = (AccessorLetExpr)s.Declare( new AccessorLetExpr( SourceLocation.Empty, "V2" ) );
            var v3From1 = (AccessorLetExpr)s.Declare( new AccessorLetExpr( SourceLocation.Empty, "V3" ) );

            Assert.That( s.Find( "V1" ) == v1From1 );
            Assert.That( s.Find( "V2" ) == v2From1 );
            Assert.That( s.Find( "V3" ) == v3From1 );

            s.OpenScope();
            {
                var v1From2 = (AccessorLetExpr)s.Declare( new AccessorLetExpr( SourceLocation.Empty, "V1" ) );
                Assert.That( s.Find( "V1" ) == v1From2 );
                Assert.That( s.Find( "V2" ) == v2From1 );
                Assert.That( s.Find( "V3" ) == v3From1 );

                s.OpenScope();
                {
                    var v1From3 = (AccessorLetExpr)s.Declare( new AccessorLetExpr( SourceLocation.Empty, "V1" ) );
                    var v2From3 = (AccessorLetExpr)s.Declare( new AccessorLetExpr( SourceLocation.Empty, "V2" ) );
                    Assert.That( s.Find( "V1" ) == v1From3 );
                    Assert.That( s.Find( "V2" ) == v2From3 );
                    Assert.That( s.Find( "V3" ) == v3From1 );

                    s.OpenScope();
                    {
                        var v3From4 = (AccessorLetExpr)s.Declare( new AccessorLetExpr( SourceLocation.Empty, "V3" ) );
                        Assert.That( s.Find( "V1" ) == v1From3 );
                        Assert.That( s.Find( "V2" ) == v2From3 );
                        Assert.That( s.Find( "V3" ) == v3From4 );
                        CheckClose( s.CloseScope(), v3From4 );
                    }
                    Assert.That( s.Find( "V1" ) == v1From3 );
                    Assert.That( s.Find( "V2" ) == v2From3 );
                    Assert.That( s.Find( "V3" ) == v3From1 );
                    var v4From3 = (AccessorLetExpr)s.Declare( new AccessorLetExpr( SourceLocation.Empty, "V4" ) );
                    var v5From3 = (AccessorLetExpr)s.Declare( new AccessorLetExpr( SourceLocation.Empty, "V5" ) );
                    CheckClose( s.CloseScope(), v1From3, v2From3, v4From3, v5From3 );
                }
                Assert.IsNull( s.Find( "V4" ) );
                Assert.IsNull( s.Find( "V5" ) );
                CheckClose( s.CloseScope(), v1From2 );
            }

            CheckClose( s.CloseScope(), v1From1, v2From1, v3From1 );
        }

        static void CheckClose( IReadOnlyList<Expr> close, params AccessorLetExpr[] decl )
        {
            CollectionAssert.AreEqual( decl, close.Cast<AccessorLetExpr>() );
        }

    }
}
