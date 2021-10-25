namespace Prospect.Launcher
{
    public static class Patches
    {
        private const string DefaultPlayFabUrl = ".playfabapi.com";

        public static bool ChangePlayFabUrl(MemoryHelper memory, string value)
        {
            if (value.Length > DefaultPlayFabUrl.Length)
            {
                return false;
            }

            var lengthWithNull = (uint)(value.Length + 1);
            
            // Modify wide string.
            memory.WriteU32(0xCA45A9 + 2, lengthWithNull * 2); // mov     r8d, 20h
            memory.WriteUnicode(0x4431860, value);             // text "UTF-16LE", '.playfabapi.com',0
            
            // Modify normal string.
            memory.WriteU32(0x605F44 + 1, lengthWithNull); // mov     edx, 10h
            memory.WriteU32(0x605F86 + 2, lengthWithNull); // mov     r9d, 10h
            memory.WriteASCII(0x4431588, value);           // '.playfabapi.com',0
            
            return true;
        }
    }
}