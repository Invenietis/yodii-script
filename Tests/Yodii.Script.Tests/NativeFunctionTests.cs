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
using FluentAssertions;

namespace Yodii.Script.Tests
{
    [TestFixture]
    public class NativeFunctionTests
    {
        [Test]
        public void calling_a_void_delegate()
        {
            var c = new GlobalContext();
            string called = null;
            Action<string> a = delegate ( string s ) { called = s; };
            c.Register( "CallMe", a );
            TestHelper.RunNormalAndStepByStep( @"CallMe( 'I''m famous.' );", o =>
            {
                o.Should().BeSameAs( RuntimeObj.Undefined );
                called.Should().Be( "I'm famous." );
            }, c );
        }

        [Test]
        public void calling_a_static_function_requires_an_explicit_cast_to_resolve_method_among_method_group()
        {
            var c = new GlobalContext();
            c.Register( "CallMe", (Func<string,string>)StaticFunc );
            TestHelper.RunNormalAndStepByStep( @"CallMe( 'I''m famous.' );", o =>
            {
                o.Should().BeOfType<StringObj>();
                o.ToString().Should().Be( "Yes! I'm famous." );
            }, c );
        }

        static string StaticFunc( string s ) => "Yes! " + s;

        class O
        {
            public string Text { get; set; }
            public string InstanceMethod( string c ) => Text + c;
        }

        [Test]
        public void calling_an_instance_method_requires_an_explicit_cast_to_resolve_method_among_method_group()
        {
            var obj = new O() { Text = "Oh My... " };
            var c = new GlobalContext();
            c.Register( "CallMe", (Func<string, string>)obj.InstanceMethod );
            TestHelper.RunNormalAndStepByStep( @"CallMe( 'I''m famous.' );", o =>
            {
                o.Should().BeOfType<StringObj>();
                o.ToString().Should().Be( "Oh My... I'm famous." );
            }, c );
        }


    }
}
