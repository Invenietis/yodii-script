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
    /// <summary>
    /// Abstract Syntax Tree generator.
    /// </summary>
    public class Analyzer
    {
        Tokenizer _parser;
        StaticScope _scope;
        AnalyzerOptions _options;

        static readonly AnalyzerOptions _defaultOptions = new AnalyzerOptions();

        /// <summary>
        /// Initialzes a new <see cref="Analyzer"/>.
        /// </summary>
        /// <param name="options">Analyser options to use.</param>
        public Analyzer( AnalyzerOptions options = null )
        {
            _options = options ?? _defaultOptions;
            _scope = new StaticScope( _options.ShareGlobalScope, _options.AllowScopeMasking, _options.AllowScopeLocalRedefinition );
        }

        /// <summary>
        /// Analyses the tokens to produce an AST of Expr.
        /// When <paramref name="allowGlobalUse"/> is true and <see cref="AnalyzerOptions.ShareGlobalScope"/> is true, the top-level declarations
        /// go into the global scope.
        /// </summary>
        /// <param name="p">Tokenizer to analyse.</param>
        /// <param name="allowGlobalUse">False to scope declarations to this analysis.</param>
        /// <returns>The AST (that may be a <see cref="SyntaxErrorExpr"/> or contains such errors).</returns>
        public Expr Analyse( Tokenizer p, bool allowGlobalUse = true )
        {
            _parser = p;
            if( !(allowGlobalUse && _scope.GlobalScope) ) _scope.OpenScope();
            return HandleBlock( Expression( 0 ) );
        }

        /// <summary>
        /// Analyses a string.
        /// </summary>
        /// <param name="s">String to analyse.</param>
        /// <param name="tOptions">Tokenizer options.</param>
        /// <param name="aOptions">Analyser options.</param>
        /// <returns>The resulting AST.</returns>
        public static Expr AnalyseString( string s, TokenizerOptions tOptions = null, AnalyzerOptions aOptions = null )
        {
            Analyzer a = new Analyzer( aOptions );
            return a.Analyse( new Tokenizer( s, tOptions ) );
        }

        Expr Expression( int rightBindingPower )
        {
            Expr left = HandleNud();
            while( !left.IsStatement && rightBindingPower < _parser.CurrentToken.PrecedenceLevel() )
            {
                left = HandleLed( left );
            }
            return left;
        }

        Expr HandleNud()
        {
            if( _parser.IsErrorOrEndOfInput ) return new SyntaxErrorExpr( _parser.Location, "Error: " + _parser.ErrorCode.ToString() );
            Debug.Assert( !_parser.IsErrorOrEndOfInput );
            if( _parser.IsNumber ) return new ConstantExpr( _parser.Location, _parser.ReadDouble(), _parser.Match( TokenizerToken.SemiColon ) );
            if( _parser.IsString ) return new ConstantExpr( _parser.Location, _parser.ReadString(), _parser.Match( TokenizerToken.SemiColon ) );
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
                if( _parser.MatchIdentifier( "foreach" ) ) return HandleForeach();
                if( _parser.MatchIdentifier( "function" ) ) return HandleFunction();
                if( _parser.MatchIdentifier( "try" ) ) return HandleTryCatch();
                if( _parser.MatchIdentifier( "with" ) ) return HandleWith();
                return HandleIdentifier();
            }
            if( _parser.Match( TokenizerToken.OpenCurly ) ) return HandleBlock();
            if( _parser.Match( TokenizerToken.OpenPar ) )
            {
                SourceLocation location = _parser.PrevNonCommentLocation;
                Expr e = Expression( 0 );
                if( e is SyntaxErrorExpr ) return e;
                return _parser.Match( TokenizerToken.ClosePar ) ? e : new SyntaxErrorExpr( _parser.Location, "Expected ')' opened at {0}.", location );
            }
            if( _parser.Match( TokenizerToken.SemiColon ) ) return NopExpr.Statement;
            return new SyntaxErrorExpr( _parser.Location, "Syntax Error." );
        }

        Expr HandleLed( Expr left )
        {
            if( _parser.IsBinaryOperator || _parser.IsCompareOperator ) return HandleBinaryExpr( left );
            if( _parser.IsLogical ) return HandleLogicalExpr( left );
            if( _parser.Match( TokenizerToken.Dot ) ) return HandleMember( left );
            if( _parser.Match( TokenizerToken.QuestionMark ) ) return HandleTernaryConditional( left );
            if( _parser.Match( TokenizerToken.OpenPar ) ) return HandleCallOrIndex( left, TokenizerToken.ClosePar );
            if( _parser.Match( TokenizerToken.OpenSquare ) ) return HandleCallOrIndex( left, TokenizerToken.CloseSquare );
            if( _parser.IsAssignOperator )
            {
                AccessorExpr a = left as AccessorExpr;
                if( a == null ) return new SyntaxErrorExpr( _parser.Location, "Invalid assignment left-hand side." );
                return HandleAssign( a, a );
            }
            if( _parser.CurrentToken == TokenizerToken.PlusPlus || _parser.CurrentToken == TokenizerToken.MinusMinus ) return HandlePostIncDec( left );
            return new SyntaxErrorExpr( _parser.Location, "Syntax Error." );
        }

        Expr HandleTryCatch()
        {
            var locTry = _parser.PrevNonCommentLocation;
            if( !_parser.Match( TokenizerToken.OpenCurly ) ) return new SyntaxErrorExpr( _parser.Location, "Expected '{{}'." );
            Expr tryExpr = HandleBlock();
            if( tryExpr is SyntaxErrorExpr ) return tryExpr;
            var locCatch = _parser.Location;
            if( !_parser.MatchIdentifier( "catch" ) ) return new SyntaxErrorExpr( locCatch, "Expected 'catch'." );
            IReadOnlyList<AccessorLetExpr> parameters, closures;
            Expr catchExpr = HandleFuncParametersAndBody( out parameters, out closures, true );
            if( parameters.Count > 1 ) return new SyntaxErrorExpr( locCatch, "At most one parameter is expected." );
            return new TryCatchExpr( locTry, tryExpr, parameters.Count == 0 ? null : parameters[0], catchExpr );
        }

        Expr HandleWith()
        {
            // "with" identifier has already been matched.
            SourceLocation location = _parser.PrevNonCommentLocation;
            Expr obj;
            if( !IsOptionallyEnclosedExpr( out obj ) ) return obj;
            Expr code = HandleStatement();
            return new WithExpr( location, obj, code );
        }

        Expr HandlePostIncDec( Expr left )
        {
            var loc = _parser.Location;
            var t = _parser.CurrentToken;
            _parser.Forward();
            AccessorExpr a = left as AccessorExpr;
            if( a == null ) return new SyntaxErrorExpr( loc, "invalid increment operand." );
            return new PrePostIncDecExpr( loc, a, t == TokenizerToken.PlusPlus, false, _parser.Match( TokenizerToken.SemiColon ) );
        }

        Expr HandleLet()
        {
            List<AccessorLetExpr> decl = null;
            List<Expr> multi = null;
            for( ;;)
            {
                string name = _parser.ReadIdentifier();
                if( name == null ) return new SyntaxErrorExpr( _parser.Location, "Expected identifier (variable name)." );
                var v = new AccessorLetExpr( _parser.PrevNonCommentLocation, name );
                Expr e = _parser.IsAssignOperator
                            ? HandleAssign( v, _scope.Find( name ), name )
                            : v;
                Debug.Assert( !(e is SyntaxErrorExpr) );
                if( _parser.Match( TokenizerToken.Comma ) )
                {
                    if( multi == null )
                    {
                        multi = new List<Expr>();
                        decl = new List<AccessorLetExpr>();
                    }
                    multi.Add( e );
                    decl.Add( v );
                }
                else
                {
                    Expr reg = _scope.Declare( v );
                    if( reg is SyntaxErrorExpr ) return reg;
                    if( multi == null ) return e;
                    foreach( var var in decl )
                    {
                        reg = _scope.Declare( var );
                        if( reg is SyntaxErrorExpr ) return reg;
                    }
                    multi.Add( e );
                    return new ListOfExpr( multi );
                }
            }
        }

        Expr HandleFunction()
        {
            var funcLocation = _parser.PrevNonCommentLocation;
            string name = _parser.ReadIdentifier();
            AccessorLetExpr funcName = null;
            if( name != null )
            {
                funcName = new AccessorLetExpr( _parser.PrevNonCommentLocation, name );
                Expr eRegName = _scope.Declare( funcName );
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
            if( !_parser.Match( TokenizerToken.OpenPar ) )
            {
                if( !allowNone ) return new SyntaxErrorExpr( _parser.Location, "Expected '('." );
            }
            else
            {
                string pName;
                while( (pName = _parser.ReadIdentifier()) != null )
                {
                    AccessorLetExpr param = new AccessorLetExpr( _parser.PrevNonCommentLocation, pName );
                    Expr eRegParam = _scope.Declare( param );
                    if( eRegParam is SyntaxErrorExpr ) return eRegParam;
                    if( !_parser.Match( TokenizerToken.Comma ) ) break;
                }
                if( !_parser.Match( TokenizerToken.ClosePar ) ) return new SyntaxErrorExpr( _parser.Location, "Expected ')'." );
            }
            if( !_parser.Match( TokenizerToken.OpenCurly ) ) return new SyntaxErrorExpr( _parser.Location, "Expected '{{}'." );
            return null;
        }

        Expr HandleAssign( AccessorExpr left, AccessorExpr leftSource, string unboundName = null )
        {
            Debug.Assert( left != null );
            var location = _parser.Location;
            if( _parser.Match( TokenizerToken.Assign ) )
            {
                return new AssignExpr( location, left, Expression( 2 ) );
            }
            if( leftSource == null ) leftSource = new AccessorMemberExpr( _parser.Location, null, unboundName, false );
            TokenizerToken binaryTokenType = _parser.CurrentToken.FromAssignOperatorToBinary();
            _parser.Forward();
            return new AssignExpr( location, left, new BinaryExpr( location, leftSource, binaryTokenType, Expression( 2 ) ) );
        }

        Expr HandleIf()
        {
            // "if" identifier has already been matched.
            SourceLocation location = _parser.PrevNonCommentLocation;
            Expr c;
            if( !IsOptionallyEnclosedExpr( out c ) ) return c;
            Expr whenTrue = HandleStatement();
            Expr whenFalse = null;
            if( _parser.MatchIdentifier( "else" ) ) whenFalse = HandleStatement();
            return new IfExpr( location, false, c, whenTrue, whenFalse );
        }

        bool IsOptionallyEnclosedExpr( out Expr c )
        {
            bool openPar = _parser.Match( TokenizerToken.OpenPar );
            c = Expression( 0 );
            if( !openPar || _parser.Match( TokenizerToken.ClosePar ) ) return true;
            c = new SyntaxErrorExpr( _parser.Location, "Expected ')'." );
            return false;
        }

        Expr HandleStatement()
        {
            if( _parser.Match( TokenizerToken.OpenCurly ) ) return HandleBlock();
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
            while( !_parser.Match( TokenizerToken.CloseCurly ) && !_parser.IsEndOfInput )
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

        Expr HandleForeach()
        {
            SourceLocation location = _parser.PrevNonCommentLocation;
            bool openPar = _parser.Match( TokenizerToken.OpenPar );
            string name = _parser.ReadIdentifier();
            if( name == "let" ) name = _parser.ReadIdentifier();
            if( name == null ) return new SyntaxErrorExpr( _parser.Location, "Expected identifier (variable name)." );
            AccessorLetExpr var = new AccessorLetExpr( _parser.PrevNonCommentLocation, name );
            Expr e = _scope.Declare( var );
            if( e is SyntaxErrorExpr ) return e;
            if( !_parser.MatchIdentifier( "in" ) ) return new SyntaxErrorExpr( _parser.Location, "Expected in keyword." );
            Expr generator = Expression( 0 );
            if( openPar && !_parser.Match( TokenizerToken.ClosePar ) ) return new SyntaxErrorExpr( _parser.Location, "Expected closing parenthesis." );
            Expr code = HandleStatement();
            return new ForeachExpr( location, var, generator, code );
        }

        Expr HandleWhile()
        {
            SourceLocation location = _parser.PrevNonCommentLocation;
            Expr c;
            if( !IsOptionallyEnclosedExpr( out c ) ) return c;
            Expr code = HandleStatement();
            return new WhileExpr( location, c, code );
        }

        Expr HandleDoWhile()
        {
            SourceLocation location = _parser.PrevNonCommentLocation;
            if( !_parser.Match( TokenizerToken.OpenCurly ) ) return new SyntaxErrorExpr( _parser.Location, "Expected '{{'." );
            Expr code = HandleBlock();
            if( !_parser.MatchIdentifier( "while" ) ) return new SyntaxErrorExpr( _parser.Location, "Expected 'while'." );
            Expr c;
            if( !IsOptionallyEnclosedExpr( out c ) ) return c;
            return new WhileExpr( location, true, c, code );
        }

        Expr HandleMember( Expr left )
        {
            string id = _parser.ReadIdentifier();
            if( id == null ) return new SyntaxErrorExpr( _parser.Location, "Identifier expected." );
            return new AccessorMemberExpr( _parser.PrevNonCommentLocation, left, id, _parser.Match( TokenizerToken.SemiColon ) );
        }

        Expr HandleCallOrIndex( Expr left, TokenizerToken closer )
        {
            SourceLocation loc = _parser.PrevNonCommentLocation;
            IList<Expr> parameters = null;
            IReadOnlyList<AccessorLetExpr> declaredFunctions = null;
            if( !_parser.Match( closer ) )
            {
                _scope.OpenScope();
                for( ;;)
                {
                    Debug.Assert( TokenizerToken.Comma.PrecedenceLevel() == 2 );
                    Expr e = Expression( 2 );
                    if( e is SyntaxErrorExpr ) return e;
                    if( parameters == null ) parameters = new List<Expr>();
                    parameters.Add( e );

                    if( _parser.Match( closer ) ) break;
                    if( !_parser.Match( TokenizerToken.Comma )
                        && !e.IsStatement
                        && !_options.AllowSemiColonAsActualParameterSeparator )
                    {
                        return new SyntaxErrorExpr( _parser.Location, $"Expected {closer} opened at {0}.", loc );
                    }
                }
                declaredFunctions = _scope.CloseScope();
            }
            var arguments = parameters != null ? parameters.ToArray() : Expr.EmptyArray;
            return new AccessorCallExpr( loc, left, arguments, declaredFunctions, _parser.Match( TokenizerToken.SemiColon ), closer == TokenizerToken.CloseSquare );
        }
        Expr HandleIdentifier()
        {
            string id = _parser.ReadIdentifier();
            if( id == "null" ) return new ConstantExpr( _parser.PrevNonCommentLocation, null, _parser.Match( TokenizerToken.SemiColon ) );
            if( id == "true" ) return new ConstantExpr( _parser.PrevNonCommentLocation, true, _parser.Match( TokenizerToken.SemiColon ) );
            if( id == "false" ) return new ConstantExpr( _parser.PrevNonCommentLocation, false, _parser.Match( TokenizerToken.SemiColon ) );
            if( id == "undefined" ) return ConstantExpr.UndefinedExpr;
            var bound = _scope.FindAndRegisterClosure( id );
            return bound != null ? (Expr)bound : new AccessorMemberExpr( _parser.PrevNonCommentLocation, null, id, _parser.Match( TokenizerToken.SemiColon ) );
        }

        Expr HandleUnaryExpr()
        {
            var loc = _parser.Location;
            var t = _parser.CurrentToken;
            _parser.Forward();
            // Unary operators are TokenizerToken.OpLevel14, except Minus and Plus that are classified as binary operators and are associated to OpLevel12.
            var right = Expression( TokenizerToken.OpLevel14.PrecedenceLevel() );
            if( t == TokenizerToken.PlusPlus || t == TokenizerToken.MinusMinus )
            {
                AccessorExpr a = right as AccessorExpr;
                if( a == null ) return new SyntaxErrorExpr( loc, "invalid increment operand." );
                return new PrePostIncDecExpr( loc, a, t == TokenizerToken.PlusPlus, true, right.IsStatement );
            }
            return new UnaryExpr( loc, t, right );
        }

        Expr HandleBinaryExpr( Expr left )
        {
            _parser.Forward();
            return new BinaryExpr( _parser.PrevNonCommentLocation, left, _parser.PrevNonCommentToken, Expression( _parser.PrevNonCommentToken.PrecedenceLevel() ) );
        }

        Expr HandleLogicalExpr( Expr left )
        {
            _parser.Forward();
            // Right associative operators to support short-circuit (hence the -1 on the level).
            return new BinaryExpr( _parser.PrevNonCommentLocation, left, _parser.PrevNonCommentToken, Expression( _parser.PrevNonCommentToken.PrecedenceLevel() - 1 ) );
        }

        Expr HandleTernaryConditional( Expr left )
        {
            SourceLocation qLoc = _parser.PrevNonCommentLocation;
            Debug.Assert( TokenizerToken.QuestionMark.PrecedenceLevel() == 6 );
            Expr whenTrue = Expression( 6 );
            if( whenTrue is SyntaxErrorExpr ) return whenTrue;
            if( !_parser.Match( TokenizerToken.Colon ) ) return new SyntaxErrorExpr( _parser.Location, "Expected colon (:) after ? at {0}.", qLoc );
            return new IfExpr( qLoc, true, left, whenTrue, Expression( 6 ) );
        }

    }

}
