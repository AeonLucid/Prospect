using System;
using System.Runtime.InteropServices;

namespace Prospect.Launcher.Invoke.Structs;

[StructLayout(LayoutKind.Sequential)]
internal struct ProcessInformation
{
    public IntPtr hProcess;
    public IntPtr hThread;
    public uint dwProcessId;
    public uint dwThreadId;
}