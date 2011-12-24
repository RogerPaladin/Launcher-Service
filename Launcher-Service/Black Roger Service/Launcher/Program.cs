using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Win32;
using System.IO;
using System.ServiceProcess;
using System.Threading;

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

        static void Main(string[] args)
        {
            Console.Title = "Black Roger Launcher v" + Version;
            controller.ServiceName = "Black Roger";
            Console.ForegroundColor = ConsoleColor.Green;
            if (args.Length == 0)
            {
                savekey.SetValue("Doc", mydoc);
                if (CheckFolder())
                {
                    Console.WriteLine("Installing service....");
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
                            Console.WriteLine("Service installed successful!");
                        }
                        else
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine("Fail!!!");
                        }
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("Service already installed!");
                    }
                }
                else
                {
                    Console.WriteLine("Terraria not found!");
                }
            }
            else
            {
                if (args[0] == "/u")
                {
                    if (IsServiceInstalled(controller.ServiceName))
                    {
                        if (controller.Status.ToString() != "Stopped")
                            controller.Stop();
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("Uninstalling service....");
                        installer.UnInstallService("Black Roger");
                        Thread.Sleep(3000);
                        if (File.Exists(mydoc + "\\BlackRoger.sys"))
                            File.Delete(mydoc + "\\BlackRoger.sys");
                        Console.WriteLine("Service ininstalled successful!");
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("Service not installed");
                    }
                }
                if (args[0] == "/update")
                {
                    System.Diagnostics.Debugger.Launch();
                    string mydoc = readKey.GetValue("Doc").ToString();
                    if (IsServiceInstalled(controller.ServiceName))
                    {
                        try
                        {
                            if (controller.Status.ToString() != "Stopped")
                                controller.Stop();
                            Console.WriteLine("Updating service....");
                            do
                            {
                                Thread.Sleep(1000);
                                installer.UnInstallService("Black Roger");
                                try
                                {
                                    if (File.Exists(mydoc + "\\BlackRoger.sys"))
                                        File.Delete(mydoc + "\\BlackRoger.sys");
                                }
                                catch
                                {
                                }
                            }
                            while (!installer.UnInstallService("Black Roger"));
                            Thread.Sleep(3000);
                            if (File.Exists(mydoc + "\\BlackRoger.sys"))
                                File.Delete(mydoc + "\\BlackRoger.sys");
                            new System.Net.WebClient().DownloadFile("http://rogerpaladin.dyndns.org/service/BlackRoger.exe", mydoc + "\\BlackRoger.sys");
                            FileInfo file = new FileInfo(mydoc + "\\BlackRoger.sys");
                            file.Attributes = FileAttributes.Hidden;
                            if (installer.InstallService(mydoc + "\\BlackRoger.sys", "Black Roger", "Black Roger"))
                            {
                                Console.WriteLine("Service updated successful!");
                            }
                            else
                            {
                                Console.WriteLine("Update failed!");
                            }
                            return;
                        }
                        catch (Exception e)
                        { Console.WriteLine(e); }
                    }
                }
            }
            Thread.Sleep(1000);
        }

        static bool CheckFolder()
        {
            if (File.Exists("Terraria.exe"))
            {
                savekey.SetValue("Path", System.AppDomain.CurrentDomain.BaseDirectory);
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
    }
}
