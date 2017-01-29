using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Yodii.Script
{
    /// <summary>
    /// Parametrizes how a <see cref="Tokenizer"/> works.
    /// </summary>
    public class TokenizerOptions
    {
        /// <summary>
        /// Gets or sets Pascal assigment: := is <see cref="TokenizerToken.Assign"/> and
        /// = alone becomes <see cref="TokenizerToken.Equal"/>.
        /// Note that in this mode, == is still Equal and <see cref="TokenizerToken.StrictEqual"/> is 
        /// still handled.
        /// </summary>
        public bool UsePascalAssign { get; set; }

        /// <summary>
        /// Gets or sets whether <see cref="TokenizerToken.LineComment"/> and <see cref="TokenizerToken.StarComment"/>
        /// must be skipped.
        /// Defaults to true.
        /// </summary>
        public bool SkipComments { get; set; } = true;
    }
}
