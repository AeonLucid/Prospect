using System.Collections.Generic;

namespace Prospect.Unreal.Generator
{
    internal class NetControlMessageEntry
    {
        public string Name { get; set; }

        public int Index { get; set; }

        public List<string> Params { get; set; } = new List<string>();
    }
}
