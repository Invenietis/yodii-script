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
using Xunit;
using FluentAssertions;

namespace Yodii.Script.Tests
{
    
    public class ObjectSupportTests
    {

        [Fact]
        public void accessing_properties_on_anonymous_class()
        {
            var c = new GlobalContext();
            c.Register( "Pos", new { X = 3, Y = 78 } );
            TestHelper.RunNormalAndStepByStep( "Pos.X + Pos.Y", o =>
            {
                o.Should().BeOfType<DoubleObj>();
                o.ToDouble().Should().Be( 3.0 + 78 );
            }, c );
        }


        class AnObject
        {
            public AnObject()
            {
                NameAsField = "(Field name)";
                Name = "Name of the object";
                AnotherObject = new AnotherObject() { OtherName = "Name of the other", IntegerField = 90 };
            }

            public string NameAsField;

            public string Name { get; set; }

            public AnotherObject AnotherObject { get; }
        }

        class AnotherObject
        {
            public int TotalMethodCallCount;
            public int IntegerField;

            public string OtherName { get; set; }

            public int AMethod()
            {
                return ++TotalMethodCallCount;
            }
            // This method is hidden by AMethod() without parameters.
            public int AMethod( string willNeverBeCalled = null )
            {
                throw new Exception( "Never called!" );
            }
            public int AMethod( int boost )
            {
                return TotalMethodCallCount += boost;
            }
        }

        [Fact]
        public void accessing_property_and_field()
        {
            var c = new GlobalContext();
            c.Register( "AnObject", new AnObject() );
            TestHelper.RunNormalAndStepByStep( "AnObject.Name + AnObject.NameAsField", o =>
            {
                o.Should().BeOfType<StringObj>();
                o.ToString().Should().Be( "Name of the object(Field name)" );
            }, c );
        }

        [Fact]
        public void setting_property_and_field()
        {
            var c = new GlobalContext();
            c.Register( "AnObject", new AnObject() );
            TestHelper.RunNormalAndStepByStep( @"
                AnObject.Name = ""X"";
                AnObject.NameAsField = AnObject.Name + ""Y"";
                AnObject.Name + AnObject.NameAsField", o =>
            {
                o.Should().BeOfType<StringObj>();
                o.ToString().Should().Be( "XXY" );
            }, c );
        }

        [Fact]
        public void accessing_property_of_sub_property()
        {
            var c = new GlobalContext();
            c.Register( "AnObject", new AnObject() );
            TestHelper.RunNormalAndStepByStep( @"AnObject.AnotherObject.OtherName", o =>
            {
                o.Should().BeAssignableTo<RefRuntimeObj>();
                o.ToString().Should().Be( "Name of the other" );
            }, c );
        }

        [Fact]
        public void postincrementing_integer_field()
        {
            var c = new GlobalContext();
            var anObject = new AnObject();
            c.Register( "anObject", anObject );
            TestHelper.RunNormalAndStepByStep( @"
                // We must reset the value since this is called twice.
                if( anObject.AnotherObject.IntegerField != 90 ) anObject.AnotherObject.IntegerField = 90;
                anObject.AnotherObject.IntegerField++", o =>
            {
                o.Should().BeOfType<DoubleObj>();
                o.ToString().Should().Be( "90" );
                anObject.AnotherObject.IntegerField.Should().Be( 91 );
            }, c );
        }

        [Fact]
        public void calling_methods()
        {
            var c = new GlobalContext();
            var anObject = new AnObject();
            c.Register( "anObject", anObject );
            TestHelper.RunNormalAndStepByStep( @"
                anObject.AnotherObject.TotalMethodCallCount = 0;
                let r0 = anObject.AnotherObject.AMethod();
                if( r0 != 1 || anObject.AnotherObject.TotalMethodCallCount != 1 ) throw ""BUG!"";
                let r1 = anObject.AnotherObject.AMethod( 2 );
                r1 * 1000 + anObject.AnotherObject.TotalMethodCallCount
                ", o =>
            {
                o.Should().BeOfType<DoubleObj>();
                o.ToString().Should().Be( "3003" );
                anObject.AnotherObject.TotalMethodCallCount.Should().Be( 3 );
            }, c );
        }

        [Fact]
        public void calling_dictionnary_indexer()
        {
            var c = new GlobalContext();
            var dic = new Dictionary<string,string>();
            dic.Add( "TheKey", "TheValue" );
            c.Register( "Dic", dic );
            TestHelper.RunNormalAndStepByStep( @"
                Dic.get_Item(""TheKey"");
                ", o =>
            {
                o.Should().BeOfType<StringObj>();
                o.ToString().Should().Be( "TheValue" );
            }, c );
        }

        public class ModelWithMethod
        {
            public string TheVariable { get; set; }

            public string Reverse( string variable )
            {
                return String.Join( "", variable.Reverse() );
            }
        }

        [Fact]
        public void Model_with_property_and_helper_method()
        {
            var model = new ModelWithMethod()
            {
                TheVariable = "azerty"
            };

            var c = new GlobalContext();
            c.Register( "Model", model );

            var e = new TemplateEngine( c );
            var result = e.Process( "Hello, <%= Model.Reverse( Model.TheVariable ) %>" );

            result.Text.Should().Be( $"Hello, {model.Reverse( model.TheVariable )}" );
        }

    }
}
