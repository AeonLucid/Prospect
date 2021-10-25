using System;
using System.Runtime.InteropServices;

namespace Prospect.Launcher.Invoke.Structs
{
    [StructLayout(LayoutKind.Sequential)]
    public struct ModuleInfo
    {
        public IntPtr lpBaseOfDll;
        public uint SizeOfImage;
        public IntPtr EntryPoint;
    }
}