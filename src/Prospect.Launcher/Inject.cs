using System;
using System.Runtime.InteropServices;
using System.Text;
using Prospect.Launcher.Invoke;

namespace Prospect.Launcher;

public static class Inject
{
    const int PROCESS_CREATE_THREAD = 0x0002;
    const int PROCESS_QUERY_INFORMATION = 0x0400;
    const int PROCESS_VM_OPERATION = 0x0008;
    const int PROCESS_VM_WRITE = 0x0020;
    const int PROCESS_VM_READ = 0x0010;

    const uint MEM_COMMIT = 0x00001000;
    const uint MEM_RESERVE = 0x00002000;
    const uint PAGE_READWRITE = 4;
        
    // https://codingvision.net/c-inject-a-dll-into-a-process-w-createremotethread
    public static unsafe bool Library(uint processId, string path)
    {
        IntPtr procHandle = Kernel32.OpenProcess(PROCESS_CREATE_THREAD | PROCESS_QUERY_INFORMATION | PROCESS_VM_OPERATION | PROCESS_VM_WRITE | PROCESS_VM_READ, false, (int)processId);

        var loadLibraryAddr = Kernel32.GetProcAddress(Kernel32.GetModuleHandle("kernel32.dll"), "LoadLibraryA");
        if (loadLibraryAddr == IntPtr.Zero)
        {
            Console.WriteLine("Failed GetProcAddress.");
            return false;
        }
            
        var allocMemAddress = Kernel32.VirtualAllocEx(procHandle, IntPtr.Zero, (uint)((path.Length + 1) * Marshal.SizeOf(typeof(char))), MEM_COMMIT | MEM_RESERVE, PAGE_READWRITE);
        if (allocMemAddress == IntPtr.Zero)
        {
            Console.WriteLine("Failed VirtualAllocEx.");
            return false;
        }

        var libraryBytes = Encoding.Default.GetBytes(path);
            
        fixed (byte* pLibraryBytes = libraryBytes)
        {
            Kernel32.WriteProcessMemory(procHandle, allocMemAddress, pLibraryBytes, libraryBytes.Length + 1, out _);
        }

        var threadHandle = Kernel32.CreateRemoteThread(procHandle, IntPtr.Zero, 0, loadLibraryAddr, allocMemAddress, 0, IntPtr.Zero);
        if (threadHandle == IntPtr.Zero)
        {
            Console.WriteLine("Failed CreateRemoteThread.");
            return false;
        }
            
        return true;
    }
}