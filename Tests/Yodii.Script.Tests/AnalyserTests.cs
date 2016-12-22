using NUnit.Framework;
using FluentAssertions;

namespace Yodii.Script.Tests
{
    [TestFixture]
    public class ExprAnalyserTests
    {
        [Test]
        public void an_empty_string_is_a_syntax_error()
        {
            ExprAnalyser a = new ExprAnalyser();
            JSTokenizer p = new JSTokenizer();
            {
                p.Reset( "" );
                p.IsEndOfInput.Should().Be( true );
                Expr e = a.Analyse( p );
                e.Should().BeOfType<SyntaxErrorExpr>();
            }
            {
                p.Reset( " \r\n \n   \r  \n \t  " );
                p.IsEndOfInput.Should().Be( true );
                Expr e = a.Analyse( p );
                e.Should().BeOfType<SyntaxErrorExpr>();
            }
        }

        [Test]
        public void SimpleExpression()
        {
            ExprAnalyser a = new ExprAnalyser();
            JSTokenizer p = new JSTokenizer();

            {
                p.Reset( "value" );
                p.IsErrorOrEndOfInput.Should().Be( false );
                Expr e = a.Analyse( p );
                e.Should().BeOfType<AccessorMemberExpr>();
                AccessorMemberExpr ac = e as AccessorMemberExpr;
                ac.IsUnbound.Should().Be( true );
            }
            {
                p.Reset( "!" );
                Expr e = a.Analyse( p );
                e.Should().BeOfType<UnaryExpr>();
                UnaryExpr u = e as UnaryExpr;
                u.TokenType.Should().Be( JSTokenizerToken.Not );
                u.Expression.Should().BeOfType<SyntaxErrorExpr>();
                SyntaxErrorCollector.Collect( e, null ).Count.Should().Be( 1 );
            }
            {
                p.Reset( "!value" );
                Expr e = a.Analyse( p );
                e.Should().BeOfType<UnaryExpr>();
                UnaryExpr u = e as UnaryExpr;
                u.TokenType.Should().Be( JSTokenizerToken.Not );
                u.Expression.Should().BeOfType<AccessorMemberExpr>();
                SyntaxErrorCollector.Collect( e, null ).Should().BeEmpty();
            }
            {
                p.Reset( " 0.12e43 && ~b " );
                Expr e = a.Analyse( p );
                e.Should().BeOfType<BinaryExpr>();
                BinaryExpr and = e as BinaryExpr;
                and.BinaryOperatorToken.Should().Be( JSTokenizerToken.And );
                IsConstant( and.Left, 0.12e43 );
                and.Right.Should().BeOfType<UnaryExpr>();
                UnaryExpr u = and.Right as UnaryExpr;
                u.TokenType.Should().Be( JSTokenizerToken.BitwiseNot );
                u.Expression.Should().BeOfType<AccessorMemberExpr>();
                AccessorMemberExpr m = u.Expression as AccessorMemberExpr;
                m.Left.Should().BeNull();
                SyntaxErrorCollector.Collect( e, null ).Should().BeEmpty();
            }
            {
                p.Reset( @"!a||~""x""" );
                Expr e = a.Analyse( p );
                e.Should().BeOfType<BinaryExpr>();
                BinaryExpr or = e as BinaryExpr;
                or.BinaryOperatorToken.Should().Be( JSTokenizerToken.Or );
                or.Left.Should().BeOfType<UnaryExpr>();
                or.Right.Should().BeOfType<UnaryExpr>();
                UnaryExpr u = or.Right as UnaryExpr;
                u.TokenType.Should().Be( JSTokenizerToken.BitwiseNot );
                IsConstant( u.Expression, "x" );

                SyntaxErrorCollector.Collect( e, null ).Should().BeEmpty();
            }
            {
                p.Reset( "(3)" );
                Expr e = a.Analyse( p );
                IsConstant( e, 3.0 );
            }
            {
                p.Reset( "(3+typeof 'x')" );
                Expr e = a.Analyse( p );
                e.Should().BeOfType<BinaryExpr>();
                BinaryExpr b = e as BinaryExpr;
                IsConstant( b.Left, 3.0 );
                b.Right.Should().BeOfType<UnaryExpr>();
                UnaryExpr u = b.Right as UnaryExpr;
                u.TokenType.Should().Be( JSTokenizerToken.TypeOf );
                IsConstant( u.Expression, "x" );

                SyntaxErrorCollector.Collect( e, null ).Should().BeEmpty();
            }
            {
                p.Reset( "1 ? 2 : 3" );
                Expr e = a.Analyse( p );
                e.Should().BeOfType<IfExpr>();
                IfExpr i = e as IfExpr;
                i.IsTernaryOperator.Should().Be( true );
                IsConstant( i.Condition, 1.0 );
                IsConstant( i.WhenTrue, 2.0 );
                IsConstant( i.WhenFalse, 3.0 );
            }
        }

        [Test]
        public void Array_support()
        {
            ExprAnalyser a = new ExprAnalyser();
            JSTokenizer p = new JSTokenizer();
            {
                p.Reset( "a[9]" );
                p.IsErrorOrEndOfInput.Should().Be( false );
                Expr e = a.Analyse( p );
                e.Should().BeOfType<AccessorCallExpr>();
                AccessorCallExpr ac = e as AccessorCallExpr;
                IsConstant( ac.Arguments[0], 9.0 );
            }
            {
                p.Reset( "array['Hello World!','H']" );
                p.IsErrorOrEndOfInput.Should().Be( false );
                Expr e = a.Analyse( p );
                e.Should().BeOfType<AccessorCallExpr>();
                AccessorCallExpr ac = e as AccessorCallExpr;
                IsConstant( ac.Arguments[0], "Hello World!" );
                IsConstant( ac.Arguments[1], "H" );
            }
        }

        void IsConstant( Expr e, object o )
        {
            e.Should().BeOfType<ConstantExpr>();
            ConstantExpr c = e as ConstantExpr;
            c.Value.Should().Be( o );
        }
    }
}
