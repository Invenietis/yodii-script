#region LGPL License
/*----------------------------------------------------------------------------
* This file (Tests\Yodii.Script.Tests\FunctionTests.cs) is part of Yodii-Script. 
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
    public class TryCatchTests
    {
        [Test]
        public void simple_try_throw_catch()
        {
            string s = @"   let r = 0;
                            try { throw 42; } catch( e ) { r = e; }
                            +r;
                        ";
            TestHelper.RunNormalAndStepByStep( s, o =>
            {
                Assert.IsInstanceOf<JSEvalNumber>( o );
                Assert.That( o.ToDouble(), Is.EqualTo( 42 ) );
            } );
        }

        [Test]
        public void throw_from_function()
        {
            string s = @"   let no = 0;
                            function t( n ) { ++no; throw n; no = 10000; }
                            let r;
                            try { t(42); } catch( e ) { r = e; }
                            try { t(100); } catch( e ) { r *= e; }
                            try { t(7); } catch( e ) { r -= e; }
                            r*no;
                        ";
            TestHelper.RunNormalAndStepByStep( s, o =>
            {
                Assert.IsInstanceOf<JSEvalNumber>( o );
                Assert.That( o.ToDouble(), Is.EqualTo( (42 * 100 - 7)*3 ) );
            } );
        }

        [Test]
        public void first_chance_error()
        {
            string s = @"   function t( n ) { throw n; }
                            let r;
                            try { t(42); } catch( e ) { r = e; }
                            try { t(100); } catch( e ) { r *= e; }
                            try { t(7); } catch( e ) { r -= e; }
                            r;
                        ";
            TestHelper.RunNormalAndStepByStepWithFirstChanceError( s, o =>
            {
                Assert.IsInstanceOf<RefRuntimeObj>( o );
                Assert.That( o.ToDouble(), Is.EqualTo( 42 * 100 - 7 ) );
            }, 3 );
        }

    }
}
