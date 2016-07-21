using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Yodii.Script
{
    public class TemplateEngine
    {
        readonly GlobalContext _ctx;
        readonly Writer _writer;
        readonly static Regex _rTag = new Regex( "<%=?.*?%>", RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture );

        class Writer : IAccessorVisitor
        {
            readonly StringBuilder _builder;
            readonly List<KeyValuePair<int, int>> _texts;
            string _template;

            public Writer()
            {
                _builder = new StringBuilder();
                _texts = new List<KeyValuePair<int, int>>();
            }

            public void Init( string template )
            {
                _builder.Clear();
                _texts.Clear();
                _template = template;
            }

            public int AddText( int start, int stop )
            {
                int idx = _texts.Count;
                _texts.Add( new KeyValuePair<int, int>(start,stop) );
                return idx;
            }

            public override string ToString() => _builder.ToString();

            public PExpr Visit( IAccessorFrame frame )
            {
                var s = frame.GetImplementationState( c =>
                    c.OnIndex( ( f, arg ) =>
                    {
                        var k = _texts[(int)arg.ToDouble()];
                        _builder.Append( _template, k.Key, k.Value );
                        return f.SetResult( RuntimeObj.Undefined );
                    }
                    )
                    .On( "Write" ).OnCall( ( f, args ) =>
                    {
                        foreach( var a in args ) _builder.Append( a.ToString() );
                        return f.SetResult( RuntimeObj.Undefined );

                    } )
                );
                return s != null ? s.Visit() : frame.SetError();
            }
        }

        public TemplateEngine( GlobalContext ctx )
        {
            _writer = new Writer();
            _ctx = ctx;
            _ctx.Register( "$writer", _writer );
        }

        /// <summary>
        /// Gets the global context.
        /// </summary>
        public GlobalContext Global => _ctx;

        /// <summary>
        /// Captures the <see cref="Process(string)"/> result.
        /// </summary>
        public struct Result
        {
            /// <summary>
            /// The resulting text (null on error).
            /// </summary>
            public readonly string Text;

            /// <summary>
            /// The error message if any. Otherwise null.
            /// </summary>
            public readonly string ErrorMessage;

            /// <summary>
            /// The script code.
            /// Null if no &lt;% ... %&gt; have been found.
            /// </summary>
            public readonly string Script;

            internal Result( string t, string error, string script )
            {
                Text = t;
                ErrorMessage = error;
                Script = script;
            }
        }

        /// <summary>
        /// Processes a template string.
        /// </summary>
        /// <param name="template">The template to process.</param>
        /// <returns>A result with either the text or an error.</returns>
        public Result Process( string template )
        {
            StringBuilder script = new StringBuilder();
            _writer.Init( template );
            Match m = _rTag.Match( template );
            int lastTextIdx = 0;
            while( m.Success )
            {
                int lenText = m.Index - lastTextIdx;
                if( lenText > 0 ) script.Append( "$writer" ).Append( '[' ).Append( _writer.AddText( lastTextIdx, lenText ) ).Append( "];" );
                lastTextIdx = m.Index + m.Length;
                if( template[m.Index + 2] == '=' )
                {
                    script.Append( "$writer" ).Append( ".Write(" ).Append( template, m.Index + 3, m.Length - 5 ).Append( ");" );
                }
                else script.Append( template, m.Index + 2, m.Length - 4 ).Append( ' ' );
                m = m.NextMatch();
            }
            if( lastTextIdx == 0 ) return new Result( template, null, null );
            int lenRemain = template.Length - lastTextIdx;
            if( lenRemain > 0 )
            {
                script.Append( "$writer" ).Append( '[' ).Append( _writer.AddText( lastTextIdx, lenRemain ) ).Append( "];" );
            }
            var s = script.ToString();
            var error = ScriptEngine.Evaluate( s, _ctx ) as RuntimeError;
            return error != null ? new Result( null, error.Message, s ) : new Result( _writer.ToString(), null, s );
        }
    }
}
