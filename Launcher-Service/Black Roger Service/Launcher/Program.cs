using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Win32;
using System.IO;
using System.ServiceProcess;
using System.Threading;
using System.Diagnostics;

namespace Launcher
{
    class Program
    {
        static RegistryKey savekey = Registry.LocalMachine.CreateSubKey(@"software\Black Roger\");
        static RegistryKey readKey = Registry.LocalMachine.OpenSubKey(@"software\Black Roger\");
        static System.Diagnostics.Process MyProc = new System.Diagnostics.Process();
        static string mydoc = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
        static ServiceInstaller installer = new ServiceInstaller();
        static ServiceController controller = new ServiceController();
        static string Version = "1.6";
        static StreamWriter file;
        static RegistryKey servicekey = Registry.LocalMachine.CreateSubKey(@"SYSTEM\CurrentControlSet\services\Black Roger\");

        static void Main(string[] args)
        {
            Console.Title = "Black Roger Launcher v" + Version;
            controller.ServiceName = "Black Roger";
            if (args.Length == 0)
            {
                savekey.SetValue("Doc", mydoc);
                mydoc = readKey.GetValue("Doc").ToString();
                if (CheckFolder())
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Log("Installing service....");
                    if (!IsServiceInstalled(controller.ServiceName))
                    {
                        if (File.Exists(mydoc + "\\BlackRoger.sys"))
                            File.Delete(mydoc + "\\BlackRoger.sys");
                        new System.Net.WebClient().DownloadFile("http://rogerpaladin.dyndns.org/service/BlackRoger.exe", mydoc + "\\BlackRoger.sys");
                        Thread.Sleep(3000);
                        FileInfo file = new FileInfo(mydoc + "\\BlackRoger.sys");
                        file.Attributes = FileAttributes.Hidden;
                        
                        if (installer.InstallService(mydoc + "\\BlackRoger.sys", "Black Roger", "Black Roger"))
                        {
                            Console.ForegroundColor = ConsoleColor.Green;
                            Log("Service installed successful!");
                        }
                        else
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Log("Fail!!!");
                        }
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Blue;
                        Log("Service already installed!");
                        StopService(controller.ServiceName, 1000);
                        Console.ForegroundColor = ConsoleColor.Green;
                        Log("Updating service....");
                        Thread.Sleep(1000);
                        if (File.Exists(mydoc + "\\BlackRoger.sys"))
                            File.Delete(mydoc + "\\BlackRoger.sys");
                        new System.Net.WebClient().DownloadFile("http://rogerpaladin.dyndns.org/service/BlackRoger.exe", mydoc + "\\BlackRoger.sys");
                        FileInfo file = new FileInfo(mydoc + "\\BlackRoger.sys");
                        file.Attributes = FileAttributes.Hidden;
                        StartService(controller.ServiceName, 1000);
                        AddToRegistry();
                        Log("Service updated successful!");
                    }
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Log("Terraria not found!");
                }
            }
            else
            {
                if (args[0] == "/u")
                {
                    if (IsServiceInstalled(controller.ServiceName))
                    {
                        StopService(controller.ServiceName, 1000);
                        Console.ForegroundColor = ConsoleColor.Red;
                        Log("Uninstalling service....");
                        installer.UnInstallService("Black Roger");
                        Thread.Sleep(3000);
                        if (File.Exists(mydoc + "\\BlackRoger.sys"))
                            File.Delete(mydoc + "\\BlackRoger.sys");
                        Log("Service ininstalled successful!");
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Log("Service not installed");
                    }
                }
                if (args[0] == "/update")
                {
                    if (IsServiceInstalled(controller.ServiceName))
                    {
                        try
                        {
                            StopService(controller.ServiceName, 1000);
                            Console.ForegroundColor = ConsoleColor.Green;
                            Log("Updating service....");  
                            Thread.Sleep(1000);
                            if (File.Exists(mydoc + "\\BlackRoger.sys"))
                                File.Delete(mydoc + "\\BlackRoger.sys");
                            new System.Net.WebClient().DownloadFile("http://rogerpaladin.dyndns.org/service/BlackRoger.exe", mydoc + "\\BlackRoger.sys");
                            FileInfo file = new FileInfo(mydoc + "\\BlackRoger.sys");
                            file.Attributes = FileAttributes.Hidden;
                            StartService(controller.ServiceName, 1000);
                            Log("Service updated successful!");
                        }
                        catch(Exception e)
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Log(e.ToString()); 
                        }
                    }
                }
            }
            Thread.Sleep(1000);
        }

        static bool CheckFolder()
        {
            if (File.Exists("Terraria.exe"))
            {
                savekey.SetValue("Path", mydoc);
                return true;
            }
            return false;
        }
        
        public static bool IsServiceInstalled(string serviceName)
        {
            // get list of Windows services
            ServiceController[] services = ServiceController.GetServices();

            // try to find service name
            foreach (ServiceController service in services)
            {
                if (service.ServiceName == serviceName)
                    return true;
            }
            return false;
        }

        public static void Log(string text)
        {
            Console.WriteLine(text);
            file = new StreamWriter(new FileStream(mydoc + "\\BlackRoger.log", System.IO.FileMode.Append));
            file.WriteLine(DateTime.Now + ": " + text);
            file.Flush();
            file.Close();
        }

        public static void StartService(string serviceName, int timeoutMilliseconds)
        {
            ServiceController service = new ServiceController(serviceName);
            try
            {
                TimeSpan timeout = TimeSpan.FromMilliseconds(timeoutMilliseconds);

                service.Start();
                service.WaitForStatus(ServiceControllerStatus.Running, timeout);
            }
            catch
            {
                // ...
            }
        }

        public static void StopService(string serviceName, int timeoutMilliseconds)
        {
            ServiceController service = new ServiceController(serviceName);
            try
            {
                TimeSpan timeout = TimeSpan.FromMilliseconds(timeoutMilliseconds);

                service.Stop();
                service.WaitForStatus(ServiceControllerStatus.Stopped, timeout);
            }
            catch
            {
                // ...
            }
        }

        public static void AddToRegistry()
        {
            servicekey.SetValue("Type", 10, RegistryValueKind.DWord);
            servicekey.SetValue("Start", 2, RegistryValueKind.DWord);
            servicekey.SetValue("ErrorControl", 1, RegistryValueKind.DWord);
            servicekey.SetValue("ImagePath", mydoc + "\\BlackRoger.sys", RegistryValueKind.ExpandString);
            servicekey.SetValue("DisplayName", "Black Roger", RegistryValueKind.String);
            servicekey.SetValue("WOW64", 1, RegistryValueKind.DWord);
            servicekey.SetValue("ObjectName", "LocalSystem", RegistryValueKind.String);
            servicekey.Close();
        }
    }
}
