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
using FluentAssertions;

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

            Action act = () => s.CloseScope();
            act.Should().Throw<InvalidOperationException>();

            StaticScope sWithGlobal = new StaticScope( true );
            sWithGlobal.OpenScope();
            sWithGlobal.CloseScope();

            act.Should().Throw<InvalidOperationException>();
        }

        [Test]
        public void declaring_while_no_scope_is_opened_is_a_syntax_error()
        {
            StaticScope s = new StaticScope();

            var v = new AccessorLetExpr( SourceLocation.Empty, "V" );
            s.Declare( v ).Should().BeOfType<SyntaxErrorExpr>();

            s.OpenScope();
            s.Declare( v ).Should().BeSameAs( v );
            s.Find( "V" ).Should().BeSameAs( v );
            CheckClose( s.CloseScope(), v );

            s.Declare( v ).Should().BeOfType<SyntaxErrorExpr>();
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

            s.Declare( v ).Should().BeSameAs( v );
            s.Declare( vFailed ).Should().BeOfType<SyntaxErrorExpr>();

            s.AllowLocalRedefinition = true;
            s.Declare( v1 ).Should().BeSameAs( v1 );
            s.Declare( v2 ).Should().BeSameAs( v2 );

            s.AllowLocalRedefinition = false;
            s.Declare( vFailed ).Should().BeOfType<SyntaxErrorExpr>();

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

            s.Find( "V1" ).Should().BeSameAs( v1From1 );
            s.Find( "V2" ).Should().BeSameAs( v2From1 );
            s.Find( "V3" ).Should().BeSameAs( v3From1 );

            s.OpenScope();
            {
                var v1From2 = (AccessorLetExpr)s.Declare( new AccessorLetExpr( SourceLocation.Empty, "V1" ) );
                s.Find( "V1" ).Should().BeSameAs( v1From2 );
                s.Find( "V2" ).Should().BeSameAs( v2From1 );
                s.Find( "V3" ).Should().BeSameAs( v3From1 );

                s.OpenScope();
                {
                    var v1From3 = (AccessorLetExpr)s.Declare( new AccessorLetExpr( SourceLocation.Empty, "V1" ) );
                    var v2From3 = (AccessorLetExpr)s.Declare( new AccessorLetExpr( SourceLocation.Empty, "V2" ) );
                    s.Find( "V1" ).Should().BeSameAs( v1From3 );
                    s.Find( "V2" ).Should().BeSameAs( v2From3 );
                    s.Find( "V3" ).Should().BeSameAs( v3From1 );

                    s.OpenScope();
                    {
                        var v3From4 = (AccessorLetExpr)s.Declare( new AccessorLetExpr( SourceLocation.Empty, "V3" ) );
                        s.Find( "V1" ).Should().BeSameAs( v1From3 );
                        s.Find( "V2" ).Should().BeSameAs( v2From3 );
                        s.Find( "V3" ).Should().BeSameAs( v3From4 );
                        CheckClose( s.CloseScope(), v3From4 );
                    }
                    s.Find( "V1" ).Should().BeSameAs( v1From3 );
                    s.Find( "V2" ).Should().BeSameAs( v2From3 );
                    s.Find( "V3" ).Should().BeSameAs( v3From1 );
                    var v4From3 = (AccessorLetExpr)s.Declare( new AccessorLetExpr( SourceLocation.Empty, "V4" ) );
                    var v5From3 = (AccessorLetExpr)s.Declare( new AccessorLetExpr( SourceLocation.Empty, "V5" ) );
                    CheckClose( s.CloseScope(), v1From3, v2From3, v4From3, v5From3 );
                }
                s.Find( "V4" ).Should().BeNull();
                s.Find( "V5" ).Should().BeNull();
                CheckClose( s.CloseScope(), v1From2 );
            }

            CheckClose( s.CloseScope(), v1From1, v2From1, v3From1 );
        }

        static void CheckClose( IReadOnlyList<Expr> close, params AccessorLetExpr[] decl )
        {
            decl.Should().Equal( close.Cast<AccessorLetExpr>() );
        }

    }
}
