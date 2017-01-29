using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using NUnit.Framework;

namespace Yodii.Script.Tests
{
    [TestFixture]
    public class WithObjectSupportTests
    {
        class NamedObject
        {
            public string Name { get; set; } = "Name";
        }

        [Test]
        public void with_syntax_support_on_member()
        {
            var c = new GlobalContext();
            var obj = new NamedObject();
            c.Register( "N", obj );
            TestHelper.RunNormalAndStepByStep( @"with( N ) { Name = 'changed'; }", o =>
            {
                o.Should().BeOfType<StringObj>();
                o.ToString().Should().Be( "changed" );
                obj.Name.Should().Be( "changed" );
            }, c );

        }

        class NamedObjectWithNamedObject
        {
            public NamedObject Obj { get; } = new NamedObject();
        }

        [Test]
        public void with_syntax_support_on_subordinate_members()
        {
            var c = new GlobalContext();
            var root = new NamedObjectWithNamedObject();
            c.Register( "N", root );
            TestHelper.RunNormalAndStepByStep( @"with( N ) { Obj.Name = 'changed'; }", o =>
            {
                root.Obj.Name.Should().Be( "changed" );
            }, c );
            TestHelper.RunNormalAndStepByStep( @"with( N.Obj ) { Name = 'changed again'; }", o =>
            {
                root.Obj.Name.Should().Be( "changed again" );
            }, c );
        }

        [Test]
        public void with_closing()
        {
            var c = new GlobalContext();
            c.Register( "Name", "Global" );
            c.Register( "O1", new NamedObject() { Name = "O1" } );
            c.Register( "O2", new NamedObject() { Name = "O2" } );
            TestHelper.RunNormalAndStepByStep( @"
                let n = '';
                with( O1 ) 
                { 
                    n += Name; 
                }
                n + Name;
                ", o =>
            {
                o.ToString().Should().Be( "O1Global" );
            }, c );
        }

        [Test]
        public void with_property_hiding()
        {
            object AtoF = new { Name = "a1", A = 
                                new { Name = "b1", B = 
                                    new { Name = "c1", C = 
                                        new { Name = "d1", D = 
                                            new { Name = "e1", E = 
                                                new { Name = "f1", F = 
                                                    new { Name = "g1" } } } } } } };
            object DtoF = new { Name = "d2", D = 
                                new { Name = "e2", E =
                                    new { Name = "f2", F = 
                                        new { Name = "g2" } } } };
            object AtoB = new { Name = "a3", A = 
                                new { Name = "b3", B = 
                                    new { Name = "c3" } } };

            var c = new GlobalContext();
            c.Register( "AtoF", AtoF );
            c.Register( "DtoF", DtoF );
            c.Register( "AtoB", AtoB );
            c.Register( "Name", "NO!" );
            TestHelper.RunNormalAndStepByStep( @"
                let collector = '';
                with( AtoF ) 
                { 
                    collector += Name;
                    collector += A.Name;
                    collector += A.B.Name;
                    collector += A.B.C.Name;
                    collector += A.B.C.D.Name;
                    collector += A.B.C.D.E.Name;
                    collector += A.B.C.D.E.F.Name;
                }
                collector", o =>
            {
                o.ToString().Should().Be( "a1b1c1d1e1f1g1", "Jus to see 'with' on a single object." );
            }, c );
            TestHelper.RunNormalAndStepByStep( @"
                let collector = '';
                with( AtoF ) 
                with( AtoB ) 
                { 
                    collector += A.B.C.Name;
                }
                collector", o =>
            {
                o.Should().BeOfType<RuntimeError>( "Even if A.B.C exists on AtoF, AtoB.A has been selected and AtoB has no C." )
                            .Which.ToString().Should().Contain( "Missing member" );
            }, c );

            TestHelper.RunNormalAndStepByStep( @"
                let collector = '';
                with( AtoB ) 
                with( DtoF ) 
                with( AtoF ) 
                { 
                    collector += Name;
                    collector += A.Name;
                    collector += A.B.Name;
                    collector += A.B.C.Name;
                    collector += A.B.C.D.Name;
                    collector += A.B.C.D.E.Name;
                    collector += A.B.C.D.E.F.Name;
                }
                collector", o =>
            {
                o.ToString().Should().Be( "a1b1c1d1e1f1g1", "AtoF covers AtoB and DtoF." );
            }, c );

            TestHelper.RunNormalAndStepByStep( @"
                let collector = '';
                with( AtoF ) 
                {
                    with( DtoF ) 
                    { 
                        collector += Name;
                        collector += A.Name;
                        collector += A.B.Name;
                        collector += A.B.C.Name;
                        collector += D.Name;
                        collector += D.E.Name;
                        collector += D.E.F.Name;
                    }
                    collector += Name;
                }
                collector += Name;
                collector", o =>
            {
                o.ToString().Should().Be( "d2b1c1d1e2f2g2a1NO!", "DtoF wins for Name and its access that start with 'D'." );
            }, c );

            TestHelper.RunNormalAndStepByStep( @"
                let collector = '';
                with( DtoF ) 
                {
                    with( AtoF ) 
                    { 
                        collector += Name;
                        collector += A.Name;
                        collector += A.B.Name;
                        collector += A.B.C.Name;
                        collector += D.Name;
                        collector += D.E.Name;
                        collector += D.E.F.Name;
                    }
                    collector += Name;
                }   
                collector += Name;
                collector", o =>
            {
                o.ToString().Should().Be( "a1b1c1d1e2f2g2d2NO!", "AtoF wins for Name and its access that start with 'A'." );
            }, c );

        }

    }
}
