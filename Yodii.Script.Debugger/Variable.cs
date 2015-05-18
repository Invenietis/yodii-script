using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Yodii.Script.Debugger
{
    public class Variable
    {
        string _name;
        RefRuntimeObj _obj;

        public Variable( string name, RefRuntimeObj obj )
        {
            _name = name;
            _obj = obj;
        }
        public string Name
        {
            get { return _name; }
        }
        public RefRuntimeObj Object
        {
            get { return _obj; }
            set { _obj = value; }
        }
    }
}
