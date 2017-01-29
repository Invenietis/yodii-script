using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Yodii.Script
{
    /// <summary>
    /// Parametrizes how a <see cref="Analyzer"/> works.
    /// </summary>
    public class AnalyzerOptions
    {
        /// <summary>
        /// Gets or sets whether a semicolon (;) can be used along with the comma (,) to separate 
        /// actual parameters in function calls.
        /// </summary>
        public bool AllowSemiColonAsActualParameterSeparator { get; set; }

        /// <summary>
        /// Gets or sets whether a global scope is opened.
        /// Defaults to false: each call to <see cref="Analyzer.Analyse(Tokenizer, bool)"/> is independant.
        /// </summary>
        public bool ShareGlobalScope { get; set; }

        /// <summary>
        /// Gets or sets whether masking is allowed (like in Javascript). 
        /// When masking is disallowed (like in C#), registering new entries returns a <see cref="SyntaxErrorExpr"/>
        /// instead of the registered expression.
        /// Defaults to true (javascript mode).
        /// </summary>
        public bool AllowScopeMasking { get; set; } = true;

        /// <summary>
        /// Gets or sets whether redefinition of a name in the same scope is possible. 
        /// This is allowed in javascript even with "use strict" for 'var' (but not for 'let' or 'const').
        /// It defaults to false: this a dangerous and useless "feature".
        /// </summary>
        public bool AllowScopeLocalRedefinition { get; set; }

    }

}
