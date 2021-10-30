using System;
using System.Collections.Generic;
using System.IO;
using Prospect.Launcher.Invoke;
using Prospect.Launcher.Invoke.Structs;

namespace Prospect.Launcher
{
    internal static class Program
    {
        /// <summary>
        ///     Steam AppId.
        /// </summary>
        private const string AppId = "1600360";
        
        /// <summary>
        ///     Executable name of the game.
        /// </summary>
        private const string FileName = "Prospect-Win64-Shipping.exe";
        
        private static void Main(string[] args)
        {
            if (args.Length != 1)
            {
                Console.WriteLine("Usage: ./Prospect.Launcher <Game directory>");
                return;
            }
            
            var gamePath = Path.Combine(args[0], "Prospect", "Binaries", "Win64");
            var gameBinary = Path.Combine(gamePath, FileName);
            var gameArgs = new List<string>();

            // 2EA46 = Default
            // A22AB = The Cycle Playtest
            gameArgs.Add("-empty");
            gameArgs.Add("-log");
            gameArgs.Add("-windowed");
            gameArgs.Add("-noeac");
            gameArgs.Add("-nointro");
            gameArgs.Add("-steam_auth");
            gameArgs.Add("PF_TITLEID=A22AB");
            
            // Ensure "steam_appid.txt" exists, to fix steam authentication.
            var steamAppId = Path.Combine(gamePath, "steam_appid.txt");

            if (!File.Exists(steamAppId))
            {
                File.WriteAllText(steamAppId, AppId);
            }
            
            // Spawn.
            var startupInfo = new StartupInfo();

            if (!Kernel32.CreateProcess(gameBinary, string.Join(' ', gameArgs), ProcessCreationFlags.CREATE_SUSPENDED, ref startupInfo, out var processInfo))
            {
                Console.WriteLine("Failed to spawn game.");
                return;
            }
            
            Console.WriteLine("Spawned.");

            // Inject DLL.
            var agentPath = Path.Combine(AppContext.BaseDirectory, "Libs", "Prospect.Agent.dll");

            if (!Inject.Library(processInfo.dwProcessId, agentPath))
            {
                Console.WriteLine("Failed to inject library.");
                return;
            }
            
            Console.WriteLine("Injected.");
            
            // Resume.
            Kernel32.ResumeThread(processInfo.hThread);

            Console.WriteLine("Resumed.");
        }
    }
}