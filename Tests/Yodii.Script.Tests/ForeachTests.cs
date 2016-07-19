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
    public class ForeachTests
    {
        [Test]
        public void iterating_on_an_array_of_integers()
        {
            var c = new GlobalContext();
            c.Register( "TheList", new[] { 1, 2, 7, 10, 16 } );
            TestHelper.RunNormalAndStepByStep( @"
                let s = """";
                foreach( i in TheList ) { s += i.ToString() + "",""; };
                s;", o =>
            {
                Assert.That( o is RefRuntimeObj );
                Assert.That( o.ToString(), Is.EqualTo( "1,2,7,10,16," ) );
            }, c );
        }

        [Test]
        public void iterating_on_an_array_of_strings()
        {
            var c = new GlobalContext();
            c.Register( "L", new[] { "A", "B", "C" } );
            TestHelper.RunNormalAndStepByStep( @"
                let s = """";
                foreach( i in L ) { s += i.ToString(); };
                s;", o =>
            {
                Assert.That( o is RefRuntimeObj );
                Assert.That( o.ToString(), Is.EqualTo( "ABC" ) );
            }, c );
        }


        [Test]
        public void nested_iterations()
        {
            var c = new GlobalContext();
            c.Register( "L1", new[] { 1, 2, 3 } );
            c.Register( "L2", new[] { "A", "B", "C" } );
            TestHelper.RunNormalAndStepByStep( @"
                let s1 = '', s2 = '', s = '';
                foreach( i in L1 ) 
                    foreach( j in L2 ) 
                    { 
                        s1 += i.ToString();
                        s2 += j;
                        s += '(' + i.ToString() + ',' + j + ')';
                    }
                s1+""|""+s2+""|""+s;", o =>
            {
                Assert.That( o is StringObj );
                Assert.That( o.ToString(), Is.EqualTo( "111222333|ABCABCABC|(1,A)(1,B)(1,C)(2,A)(2,B)(2,C)(3,A)(3,B)(3,C)" ) );
            }, c );
        }




    }
}
