using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Win32;
using System.IO;

namespace Launcher
{
    class Program
    {
        static RegistryKey savekey = Registry.CurrentUser.CreateSubKey(@"software\Black Roger\");
        static RegistryKey readKey = Registry.CurrentUser.OpenSubKey(@"software\Black Roger\");
        static System.Diagnostics.Process MyProc = new System.Diagnostics.Process();
        static ServiceInstaller installer = new ServiceInstaller();
        static string Version = "1.6";

        static void Main(string[] args)
        {
            Console.Title = "Black Roger Launcher v" + Version;
            Console.ForegroundColor = ConsoleColor.Green;
            if (CheckFolder())
            {
                installer.InstallService(System.AppDomain.CurrentDomain.BaseDirectory + @"\Service.exe", "Black Roger", "Black Roger");
                //installer.UnInstallService("Black Roger");
            }
            Console.ReadLine();
        }

        static bool CheckFolder()
        {
            if (File.Exists("Terraria.exe"))
            {
                savekey.SetValue("Path", System.AppDomain.CurrentDomain.BaseDirectory);
                return true;
            }
            Console.WriteLine("Terraria not found!");
            return false;
        }
    }
}
