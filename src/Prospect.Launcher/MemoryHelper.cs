using System;
using System.Buffers.Binary;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Prospect.Launcher.Invoke;
using Prospect.Launcher.Invoke.Structs;

namespace Prospect.Launcher
{
    public class MemoryHelper
    {
        private readonly IntPtr _handle;
        private readonly IntPtr _baseAddress;

        private MemoryHelper(IntPtr handle, IntPtr baseAddress)
        {
            _handle = handle;
            _baseAddress = baseAddress;
        }

        public void WriteU32(int offset, uint value)
        {
            Span<byte> data = stackalloc byte[sizeof(int)];
            BinaryPrimitives.WriteUInt32LittleEndian(data, value);
            Write(offset, data);
        }

        public void WriteASCII(int offset, string value)
        {
            // + 1 for NULL character
            var dataCount = Encoding.ASCII.GetByteCount(value);
            Span<byte> data = stackalloc byte[dataCount + 1];
            Encoding.ASCII.GetBytes(value, data);
            Write(offset, data);
        }

        public void WriteUnicode(int offset, string value)
        {
            // + 2 for NULL character
            var dataCount = Encoding.Unicode.GetByteCount(value);
            Span<byte> data = stackalloc byte[dataCount + 2];
            Encoding.Unicode.GetBytes(value, data);
            Write(offset, data);
        }
        
        public unsafe void Write(int offset, Span<byte> data)
        {
            var addr = _baseAddress + offset;

            // Modify protection.
            if (!Kernel32.VirtualProtectEx(_handle, addr, data.Length, 0x40, out var oldProtect))
            {
                throw new Exception($"Unable to change protection {Marshal.GetLastWin32Error()}.");
            }
            
            fixed (byte* pData = data)
            {
                var write = Kernel32.WriteProcessMemory(_handle, _baseAddress + offset, pData, data.Length, out _);

                // Restore protection.
                Kernel32.VirtualProtectEx(_handle, addr, data.Length, oldProtect, out _);
                
                if (!write)
                {
                    throw new Exception($"Unable to write process memory {Marshal.GetLastWin32Error()}.");
                }
            }
        }

        public byte[] Read(int offset, int count)
        {
            var buffer = new byte[count];
            
            if (!Kernel32.ReadProcessMemory(_handle, _baseAddress + offset, buffer, count, out _))
            {
                throw new Exception("Unable to read process memory.");
            }
            
            return buffer;
        }

        public static MemoryHelper CreateForHandle(IntPtr handle, string gameBinary)
        {
            var gameBinaryName = Path.GetFileName(gameBinary);
            
            var hMods = new IntPtr[512];
            var hModsHandle = GCHandle.Alloc(hMods, GCHandleType.Pinned);
            
            try
            {
                var hModsPtr = hModsHandle.AddrOfPinnedObject();
                var hModsSize = (uint)(Marshal.SizeOf(typeof(IntPtr)) * (hMods.Length));

                if (PSAPI.EnumProcessModules(handle, hModsPtr, hModsSize, out var cbNeeded) == 1)
                {
                    var moduleCount = (int)(cbNeeded / Marshal.SizeOf(typeof(IntPtr)));

                    for (var i = 0; i < moduleCount; i++)
                    {
                        var stringBuilder = new StringBuilder(1024);
                        
                        if (PSAPI.GetModuleFileNameEx(handle, hMods[i], stringBuilder, stringBuilder.Capacity) == 0)
                        {
                            continue;
                        }
                        
                        if (!gameBinaryName.Equals(Path.GetFileName(stringBuilder.ToString())))
                        {
                            continue;
                        }
                        
                        if (!PSAPI.GetModuleInformation(handle, hMods[i], out var modInfo, Marshal.SizeOf<ModuleInfo>()))
                        {
                            continue;
                        }
                        
                        return new MemoryHelper(handle, modInfo.lpBaseOfDll);
                    }
                }
            }
            finally
            {
                hModsHandle.Free();
            }
            
            return null;
        }
    }
}