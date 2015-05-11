using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Yodii.Script.Debugger
{
    /// <summary>
    /// Basic class parsing an <see cref="Expr"/> into a list of atomic breakable <see cref="Expr"/>
    /// </summary>
    public class BreakableVisitor : ExprVisitor
    {
        readonly List<Expr> _breakableExprs = new List<Expr>();
        /// <summary>
        /// Reads the full AST, to find all breakable atomic <see cref="Expr"/>
        /// </summary>
        /// <param name="e">An Expr to parse as Breakables Exprs</param>
        /// <returns></returns>
        public override Expr VisitExpr( Expr e )
        {
            Console.WriteLine( e.ToString() );
            if( e.IsBreakable ) _breakableExprs.Add( e );
            return base.VisitExpr( e );
        }
        public IReadOnlyList<Expr> BreakableExprs
        {
            get { return _breakableExprs; }
        }
    }
}
