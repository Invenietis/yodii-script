using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Yodii.Script.Debugger
{
    interface IVariableList
    {
        List<Variable> Vars { get; }
        Variable FindByName( string name );
    }
}
