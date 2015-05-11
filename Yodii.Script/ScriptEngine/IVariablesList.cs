using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Yodii.Script
{
    public interface IVariablesList
    {
        IReadOnlyList<Variable> Vars { get;}
        Variable FindByName( string name );

    }
}
