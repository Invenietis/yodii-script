#region LGPL License
/*----------------------------------------------------------------------------
* This file (Yodii.Script\Analyser\ExprAnalyser.cs) is part of Yodii-Script. 
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
using System.Diagnostics;


namespace Yodii.Script
{
    public class ExprAnalyser
    {
        static readonly int _questionMarkPrecedenceLevel = JSTokenizer.PrecedenceLevel( JSTokenizerToken.QuestionMark );

        JSTokenizer _parser;
        StaticScope _scope;

        /// <summary>
        /// Configuration for an <see cref="ExprAnalyser"/>.
        /// </summary>
        public class Config
        {
            /// <summary>
            /// Initalizes a default configuration.
            /// </summary>
            public Config()
            {
                AllowMasking = true;
            }

            /// <summary>
            /// Gets or sets whether a global scope is opened.
            /// </summary>
            public bool GlobalScope { get; set; }

            /// <summary>
            /// Gets or sets whether masking is allowed (like in Javascript). 
            /// When masking is disallowed (like in C#), registering new entries returns a <see cref="SyntaxErrorExpr"/>
            /// instead of the registered expression.
            /// Defaults to true (javascript mode).
            /// </summary>
            public bool AllowMasking { get; set; }

            /// <summary>
            /// Gets or sets whether redefinition of a name in the same scope is possible. 
            /// This is allowed in javascript even with "use strict" but here it defaults to false since I consider this a dangerous and useless feature.
            /// </summary>
            public bool AllowLocalRedefinition { get; set; }

        }

        static readonly Config _emptyConfig = new Config();

        public ExprAnalyser( Config configuration = null )
        {
            if( configuration == null ) configuration = _emptyConfig;
            _scope = new StaticScope( configuration.GlobalScope, configuration.AllowMasking, configuration.AllowLocalRedefinition );
        }

        /// <summary>
        /// Analyses the tokens to produce an AST of Expr.
        /// When <paramref name="allowGlobalUse"/> is true and <see cref="Config.GlobalScope"/> is true, the top-level declarations
        /// go into the global scope.
        /// </summary>
        /// <param name="p">Tokeinzer to analyse.</param>
        /// <param name="allowGlobalUse">False to scope declarations to this analysis.</param>
        /// <returns>The AST (that may be a <see cref="SyntaxErrorExpr"/> or contains such errors).</returns>
        public Expr Analyse( JSTokenizer p, bool allowGlobalUse = true )
        {
            _parser = p;
            if( !(allowGlobalUse && _scope.GlobalScope) ) _scope.OpenScope();
            return HandleBlock( Expression( 0 ) );
        }

        public static Expr AnalyseString( string s, Config configuration = null)
        {
            ExprAnalyser a = new ExprAnalyser( configuration );
            return a.Analyse( new JSTokenizer( s ) );
        }

        Expr Expression( int rightBindingPower )
        {
            Expr left = HandleNud();
            while( !left.IsStatement && rightBindingPower < _parser.CurrentPrecedenceLevel )
            {
                left = HandleLed( left );
            }
            return left;
        }

        Expr HandleNud()
        {
            if( _parser.IsErrorOrEndOfInput ) return new SyntaxErrorExpr( _parser.Location, "Error: " + _parser.ErrorCode.ToString() );
            Debug.Assert( !_parser.IsErrorOrEndOfInput );
            if( _parser.IsNumber ) return new ConstantExpr( _parser.Location, _parser.ReadDouble(), _parser.Match( JSTokenizerToken.SemiColon ) );
            if( _parser.IsString ) return new ConstantExpr( _parser.Location, _parser.ReadString(), _parser.Match( JSTokenizerToken.SemiColon ) );
            if( _parser.IsUnaryOperatorOrPlusOrMinus ) return HandleUnaryExpr();
            if( _parser.IsIdentifier )
            {
                if( _parser.MatchIdentifier( "if" ) ) return HandleIf();
                if( _parser.MatchIdentifier( "let" ) ) return HandleLet();
                if( _parser.MatchIdentifier( "while" ) ) return HandleWhile();
                if( _parser.MatchIdentifier( "break" ) ) return new FlowBreakingExpr( _parser.PrevNonCommentLocation, false );
                if( _parser.MatchIdentifier( "continue" ) ) return new FlowBreakingExpr( _parser.PrevNonCommentLocation, true );
                if( _parser.MatchIdentifier( "return" ) ) return new FlowBreakingExpr( _parser.PrevNonCommentLocation, Expression( 0 ), false );
                if( _parser.MatchIdentifier( "throw" ) ) return new FlowBreakingExpr( _parser.PrevNonCommentLocation, Expression( 0 ), true );
                if( _parser.MatchIdentifier( "do" ) ) return HandleDoWhile();
                if( _parser.MatchIdentifier( "function" ) ) return HandleFunction();
                if( _parser.MatchIdentifier( "try" ) ) return HandleTryCatch();
                return HandleIdentifier();
            }
            if( _parser.Match( JSTokenizerToken.OpenCurly ) ) return HandleBlock();
            if( _parser.Match( JSTokenizerToken.OpenPar ) )
            {
                SourceLocation location = _parser.PrevNonCommentLocation;
                Expr e = Expression( 0 );
                if( e is SyntaxErrorExpr ) return e;
                return _parser.Match( JSTokenizerToken.ClosePar ) ? e : new SyntaxErrorExpr( _parser.Location, "Expected ')' opened at {0}.", location );
            }
            if( _parser.Match( JSTokenizerToken.SemiColon ) ) return NopExpr.Statement;
            return new SyntaxErrorExpr( _parser.Location, "Syntax Error." );
        }

        Expr HandleLed( Expr left )
        {
            if( _parser.IsBinaryOperator || _parser.IsCompareOperator ) return HandleBinaryExpr( left );
            if( _parser.IsLogical ) return HandleLogicalExpr( left );
            if( _parser.Match( JSTokenizerToken.Dot ) ) return HandleMember( left );
            if( _parser.Match( JSTokenizerToken.QuestionMark ) ) return HandleTernaryConditional( left );
            if( _parser.Match( JSTokenizerToken.OpenPar ) ) return HandleCall( left );
            if( _parser.Match( JSTokenizerToken.OpenSquare ) ) return HandleIndexer( left );
            if( _parser.IsAssignOperator ) return HandleAssign( left );
            if( _parser.CurrentToken == JSTokenizerToken.PlusPlus || _parser.CurrentToken == JSTokenizerToken.MinusMinus ) return HandlePostIncDec( left );
            return new SyntaxErrorExpr( _parser.Location, "Syntax Error." );
        }

        Expr HandleTryCatch()
        {
            var locTry = _parser.PrevNonCommentLocation;
            if( !_parser.Match( JSTokenizerToken.OpenCurly ) ) return new SyntaxErrorExpr( _parser.Location, "Expected '{{}'." );
            Expr tryExpr = HandleBlock();
            if( tryExpr is SyntaxErrorExpr ) return tryExpr;
            var locCatch = _parser.Location;
            if( !_parser.MatchIdentifier( "catch" ) ) return new SyntaxErrorExpr( locCatch, "Expected 'catch'." );
            IReadOnlyList<AccessorLetExpr> parameters, closures;
            Expr catchExpr = HandleFuncParametersAndBody( out parameters, out closures, true );
            if( parameters.Count > 1 ) return new SyntaxErrorExpr( locCatch, "At most one parameter is expected." );
            return new TryCatchExpr( locTry, tryExpr, parameters.Count == 0 ? null : parameters[0], catchExpr );
        }

        Expr HandlePostIncDec( Expr left )
        {
            var loc = _parser.Location;
            var t = _parser.CurrentToken;
            _parser.Forward();
            AccessorExpr a = left as AccessorExpr;
            if( a == null ) return new SyntaxErrorExpr( loc, "invalid increment operand." );
            return new PrePostIncDecExpr( loc, a, t == JSTokenizerToken.PlusPlus, false, _parser.Match( JSTokenizerToken.SemiColon ) );
        }

        Expr HandleLet()
        {
            var multi = new List<Expr>();
            do
            {
                string name = _parser.ReadIdentifier();
                if( name == null ) return new SyntaxErrorExpr( _parser.Location, "Expected identifier (variable name)." );
                Expr e = _scope.Declare( name, new AccessorLetExpr( _parser.PrevNonCommentLocation, name ) );
                if( _parser.Match( JSTokenizerToken.Assign ) ) e = HandleAssign( e, true );
                multi.Add( e );
                if( e is SyntaxErrorExpr ) break;
            }
            while( _parser.Match( JSTokenizerToken.Comma ) );
            if( multi.Count == 1 ) return multi[0];
            return new ListOfExpr( multi );
        }

        Expr HandleFunction()
        {
            var funcLocation = _parser.PrevNonCommentLocation;
            string name = _parser.ReadIdentifier();
            AccessorLetExpr funcName = null;
            if( name != null )
            {
                funcName = new AccessorLetExpr( _parser.PrevNonCommentLocation, name );
                Expr eRegName = _scope.Declare( name, funcName );
                if( eRegName is SyntaxErrorExpr ) return eRegName;
            }
            IReadOnlyList<AccessorLetExpr> parameters, closures;
            Expr body = HandleFuncParametersAndBody( out parameters, out closures, false );
            var f = new FunctionExpr( funcLocation, parameters, body, closures, funcName );
            if( funcName == null ) return f;
            return new AssignExpr( funcLocation, funcName, f );
        }

        Expr HandleFuncParametersAndBody( out IReadOnlyList<AccessorLetExpr> parameters, out IReadOnlyList<AccessorLetExpr> closures, bool allowNoParams )
        {
            parameters = closures = null;
            // We open a strong scope: accesses to variables declared above are tracked.
            _scope.OpenStrongScope();
            Expr b = TryRegisterFuncParametersAndOpenBody( allowNoParams );
            if( b != null )
            {
                _scope.CloseScope();
                return b;
            }
            // Get the parameters that have been registered.
            parameters = _scope.GetCurrent();
            List<Expr> statements = new List<Expr>();
            FillStatements( statements );
            // Closes the strong scope and get the closures and the locals,
            // skipping the parameters that have already been handled.
            var closuresAndLocals = _scope.CloseStrongScope( parameters.Count );
            b = BlockFromStatements( statements, closuresAndLocals.Value );
            closures = closuresAndLocals.Key;
            return b;
        }

        Expr TryRegisterFuncParametersAndOpenBody( bool allowNone )
        {
            if( !_parser.Match( JSTokenizerToken.OpenPar ) )
            {
                if( !allowNone ) return new SyntaxErrorExpr( _parser.Location, "Expected '('." );
            }
            else
            {
                string pName;
                while( (pName = _parser.ReadIdentifier()) != null )
                {
                    AccessorLetExpr param = new AccessorLetExpr( _parser.PrevNonCommentLocation, pName );
                    Expr eRegParam = _scope.Declare( pName, param );
                    if( eRegParam is SyntaxErrorExpr ) return eRegParam;
                    if( !_parser.Match( JSTokenizerToken.Comma ) ) break;
                }
                if( !_parser.Match( JSTokenizerToken.ClosePar ) ) return new SyntaxErrorExpr( _parser.Location, "Expected ')'." );
            }
            if( !_parser.Match( JSTokenizerToken.OpenCurly ) ) return new SyntaxErrorExpr( _parser.Location, "Expected '{{}'." );
            return null;
        }

        Expr HandleAssign( Expr left, bool pureAssign = false )
        {
            var location = _parser.Location;
            AccessorExpr a = left as AccessorExpr;
            if( a == null ) return new SyntaxErrorExpr( location, "Invalid assignment left-hand side." );
            if( pureAssign || _parser.Match( JSTokenizerToken.Assign ) )
            {
                return new AssignExpr( location, a, Expression( JSTokenizer.PrecedenceLevel( JSTokenizerToken.Comma ) ) );
            }
            JSTokenizerToken binaryTokenType = JSTokenizer.FromAssignOperatorToBinary( _parser.CurrentToken );
            _parser.Forward();
            return new AssignExpr( location, a, new BinaryExpr( location, left, binaryTokenType, Expression( 0 ) ) );
        }


        Expr HandleIf()
        {
            // "if" identifier has already been matched.
            SourceLocation location = _parser.PrevNonCommentLocation;
            Expr c;
            if( !IsCondition( out c ) ) return c;
            Expr whenTrue = HandleStatement();
            Expr whenFalse = null;
            if( _parser.MatchIdentifier( "else" ) ) whenFalse = HandleStatement();
            return new IfExpr( location, false, c, whenTrue, whenFalse );
        }

        bool IsCondition( out Expr c )
        {
            if( !_parser.Match( JSTokenizerToken.OpenPar ) ) c = new SyntaxErrorExpr( _parser.Location, "Expected '('." );
            else
            {
                c = Expression( 0 );
                if( _parser.Match( JSTokenizerToken.ClosePar ) ) return true;
                c = new SyntaxErrorExpr( _parser.Location, "Expected ')'." );
            }
            return false;
        }

        Expr HandleStatement()
        {
            if( _parser.Match( JSTokenizerToken.OpenCurly ) ) return HandleBlock();
            return Expression( 0 );
        }

        Expr HandleBlock( Expr first = null )
        {
            if( first == null ) _scope.OpenScope();
            List<Expr> statements = new List<Expr>();
            if( first != null && !first.IsNop ) statements.Add( first );
            FillStatements( statements );
            // Always close the scope (even opened by the caller).
            return BlockFromStatements( statements, _scope.CloseScope() );
        }

        void FillStatements( List<Expr> statements )
        {
            while( !_parser.Match( JSTokenizerToken.CloseCurly ) && !_parser.IsEndOfInput )
            {
                Expr e = Expression( 0 );
                if( !e.IsNop ) statements.Add( e );
                if( e is SyntaxErrorExpr ) break;
            }
        }

        static Expr BlockFromStatements( List<Expr> statements, IReadOnlyList<AccessorLetExpr> locals )
        {
            if( statements.Count == 0 ) return NopExpr.Statement;
            if( statements.Count == 1 && locals.Count == 0 ) return statements[0];
            return new BlockExpr( statements.ToArray(), locals );
        }
        
        Expr HandleWhile()
        {
            SourceLocation location = _parser.PrevNonCommentLocation;
            Expr c;
            if( !IsCondition( out c ) ) return c;
            Expr code = HandleStatement();
            return new WhileExpr( location, c, code );
        }

        Expr HandleDoWhile()
        {
            SourceLocation location = _parser.PrevNonCommentLocation;
            if( !_parser.Match( JSTokenizerToken.OpenCurly ) ) return new SyntaxErrorExpr( _parser.Location, "Expected '{{'." );
            Expr code = HandleBlock();
            if( !_parser.MatchIdentifier( "while" ) ) return new SyntaxErrorExpr( _parser.Location, "Expected 'while'." );
            Expr c;
            if( !IsCondition( out c ) ) return c;
            return new WhileExpr( location, true, c, code );
        }



        Expr HandleMember( Expr left )
        {
            string id = _parser.ReadIdentifier();
            if( id == null ) return new SyntaxErrorExpr( _parser.Location, "Identifier expected." );
            return new AccessorMemberExpr( _parser.PrevNonCommentLocation, left, id, _parser.Match( JSTokenizerToken.SemiColon ) );
        }

        Expr HandleIndexer( Expr left )
        {
            SourceLocation loc = _parser.PrevNonCommentLocation;
            Expr i = Expression( 0 );
            if( i is SyntaxErrorExpr ) return i;
            if( !_parser.Match( JSTokenizerToken.CloseSquare ) )
            {
                return new SyntaxErrorExpr( _parser.Location, "Expected ] opened at {0}.", loc );
            }
            return new AccessorIndexerExpr( loc, left, i, _parser.Match( JSTokenizerToken.SemiColon ) );
        }

        Expr HandleCall( Expr left )
        {
            SourceLocation loc = _parser.PrevNonCommentLocation;
            IList<Expr> parameters = null;
            if( !_parser.Match( JSTokenizerToken.ClosePar ) )
            {
                for( ; ; )
                {
                    Debug.Assert( JSTokenizer.PrecedenceLevel( JSTokenizerToken.Comma ) == 2 );
                    Expr e = Expression( 2 );
                    if( e is SyntaxErrorExpr ) return e;

                    if( parameters == null ) parameters = new List<Expr>();
                    parameters.Add( e );

                    if( _parser.Match( JSTokenizerToken.ClosePar ) ) break;
                    if( !_parser.Match( JSTokenizerToken.Comma ) )
                    {
                        return new SyntaxErrorExpr( _parser.Location, "Expected ) opened at {0}.", loc );
                    }
                }
            }
            var arguments = parameters != null ? parameters.ToArray() : Expr.EmptyArray;
            return new AccessorCallExpr( loc, left, arguments, _parser.Match( JSTokenizerToken.SemiColon ) );
        }

        Expr HandleIdentifier()
        {
            string id = _parser.ReadIdentifier();
            if( id == "null" ) return new ConstantExpr( _parser.PrevNonCommentLocation, null, _parser.Match( JSTokenizerToken.SemiColon ) );
            if( id == "true" ) return new ConstantExpr( _parser.PrevNonCommentLocation, true, _parser.Match( JSTokenizerToken.SemiColon ) );
            if( id == "false" ) return new ConstantExpr( _parser.PrevNonCommentLocation, false, _parser.Match( JSTokenizerToken.SemiColon ) );
            if( id == "undefined" ) return ConstantExpr.UndefinedExpr;
            var bound = _scope.FindAndRegisterClosure( id );
            return bound != null ? (Expr)bound : new AccessorMemberExpr( _parser.PrevNonCommentLocation, null, id, _parser.Match( JSTokenizerToken.SemiColon ) );
        }

        Expr HandleUnaryExpr()
        {
            var loc = _parser.Location;
            var t = _parser.CurrentToken;
            _parser.Forward();
            // Unary operators are JSTokenizerToken.OpLevel14, except Minus and Plus that are classified as binary operators and are associated to OpLevel12.
            var right = Expression( JSTokenizer.PrecedenceLevel( JSTokenizerToken.OpLevel14 ) );
            if( t == JSTokenizerToken.PlusPlus || t == JSTokenizerToken.MinusMinus )
            {
                AccessorExpr a = right as AccessorExpr;
                if( a == null ) return new SyntaxErrorExpr( loc, "invalid increment operand." );
                return new PrePostIncDecExpr( loc, a, t == JSTokenizerToken.PlusPlus, true, right.IsStatement );
            }
            return new UnaryExpr( loc, t, right );
        }

        Expr HandleBinaryExpr( Expr left )
        {
            _parser.Forward();
            return new BinaryExpr( _parser.PrevNonCommentLocation, left, _parser.PrevNonCommentToken, Expression( JSTokenizer.PrecedenceLevel( _parser.PrevNonCommentToken ) ) );
        }

        Expr HandleLogicalExpr( Expr left )
        {
            _parser.Forward();
            // Right associative operators to support short-circuit (hence the -1 on the level).
            return new BinaryExpr( _parser.PrevNonCommentLocation, left, _parser.PrevNonCommentToken, Expression( JSTokenizer.PrecedenceLevel( _parser.PrevNonCommentToken ) - 1 ) );
        }

        Expr HandleTernaryConditional( Expr left )
        {
            SourceLocation qLoc = _parser.PrevNonCommentLocation;
            Expr whenTrue = Expression( _questionMarkPrecedenceLevel );
            if( whenTrue is SyntaxErrorExpr ) return whenTrue;
            if( !_parser.Match( JSTokenizerToken.Colon ) ) return new SyntaxErrorExpr( _parser.Location, "Expected colon (:) after ? at {0}.", qLoc );
            return new IfExpr( qLoc, true, left, whenTrue, Expression( _questionMarkPrecedenceLevel ) );
        }

    }

}
