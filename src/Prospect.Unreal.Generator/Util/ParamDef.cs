using System;
using System.Collections.Generic;
using System.Text;

namespace Prospect.Unreal.Generator.Util
{
    internal class ParamDef
    {
        public ParamDef(string read, string write)
        {
            Read = read;
            Write = write;
        }

        public string Read { get; }
        public string Write { get; }
    }
}
