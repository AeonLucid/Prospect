using System;
using System.Runtime.InteropServices;
using Prospect.Launcher.Invoke.Structs;

namespace Prospect.Launcher.Invoke
{
    internal static class Kernel32
    {
        [DllImport("kernel32.dll", SetLastError=true, CharSet=CharSet.Auto)]
        public static extern bool CreateProcess(string lpApplicationName,
            string lpCommandLine, IntPtr lpProcessAttributes,
            IntPtr lpThreadAttributes,
            bool bInheritHandles, ProcessCreationFlags dwCreationFlags,
            IntPtr lpEnvironment, string lpCurrentDirectory,
            ref StartupInfo lpStartupInfo,
            out ProcessInformation lpProcessInformation);

        public static bool CreateProcess(
            string lpApplicationName,
            string lpCommandLine,
            ProcessCreationFlags dwCreationFlags,
            ref StartupInfo lpStartupInfo,
            out ProcessInformation lpProcessInformation) => CreateProcess(lpApplicationName, lpCommandLine, IntPtr.Zero,
            IntPtr.Zero, false, dwCreationFlags, IntPtr.Zero, null, ref lpStartupInfo, out lpProcessInformation);
        
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool ReadProcessMemory(
            IntPtr hProcess,
            IntPtr lpBaseAddress,
            byte[] lpBuffer,
            Int32 nSize,
            out IntPtr lpNumberOfBytesRead);
        
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern unsafe bool WriteProcessMemory(
            IntPtr hProcess,
            IntPtr lpBaseAddress,
            byte* lpBuffer,
            Int32 nSize,
            out IntPtr lpNumberOfBytesWritten
        );
        
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool VirtualProtectEx(IntPtr hProcess, IntPtr lpAddress, int dwSize, uint flNewProtect, out uint lpflOldProtect);
    }
}