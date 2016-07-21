#region LGPL License
/*----------------------------------------------------------------------------
* This file (Tests\Yodii.Script.Tests\WithGlobalContext.cs) is part of Yodii-Script. 
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
using System.Threading.Tasks;
using CK.Core;
using NUnit.Framework;

namespace Yodii.Script.Tests
{
    [TestFixture]
    public class TemplateTests
    {
        [Test]
        public void foreach_with_one_write_statement()
        {
            var c = new GlobalContext();
            c.Register( "TheList", new[] { 1, 2, 7, 10, 16 } );
            var e = new TemplateEngine( c );
            var r = e.Process( "<%foreach i in TheList {%>*<%=i%><%}%>" );
            Assert.That( r.ErrorMessage, Is.Null );
            Assert.That( r.Script, Is.Not.Null );
            Assert.That( r.Text, Is.EqualTo( "*1*2*7*10*16" ) );
        }

        [Test]
        public void empty_tags_are_ignored()
        {
            var e = new TemplateEngine( new GlobalContext() );
            {
                var r = e.Process( "<%%>*<%=%>$<%%>" );
                Assert.That( r.ErrorMessage, Is.Null );
                Assert.That( r.Script, Is.Not.Null );
                Assert.That( r.Text, Is.EqualTo( "*$" ) );
            }
            {
                var r = e.Process( "<% %>*<%= %>$<% %>" );
                Assert.That( r.ErrorMessage, Is.Null );
                Assert.That( r.Script, Is.Not.Null );
                Assert.That( r.Text, Is.EqualTo( "*$" ) );
            }
        }

        [Test]
        public void when_there_is_no_tag_there_is_no_script()
        {
            var e = new TemplateEngine( new GlobalContext() );
            var r = e.Process( "There is no tag here." );
            Assert.That( r.ErrorMessage, Is.Null );
            Assert.That( r.Script, Is.Null );
            Assert.That( r.Text, Is.EqualTo( "There is no tag here." ) );
        }
    }
}
