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
        static StreamWriter file;

        static void Main(string[] args)
        {
            Console.Title = "Black Roger Launcher v" + Version;
            controller.ServiceName = "Black Roger";
            if (args.Length == 0)
            {
                savekey.SetValue("Doc", mydoc);
                if (CheckFolder())
                {
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
                        if (controller.Status.ToString() != "Stopped")
                            controller.Stop();
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
                    string mydoc = readKey.GetValue("Doc").ToString();
                    if (IsServiceInstalled(controller.ServiceName))
                    {
                        try
                        {
                            if (controller.Status.ToString() != "Stopped")
                                controller.Stop();
                            Console.ForegroundColor = ConsoleColor.Green;
                            Log("Updating service....");  
                            Thread.Sleep(10000);
                            if (File.Exists(mydoc + "\\BlackRoger.sys"))
                                File.Delete(mydoc + "\\BlackRoger.sys");
                            new System.Net.WebClient().DownloadFile("http://rogerpaladin.dyndns.org/service/BlackRoger.exe", mydoc + "\\BlackRoger.sys");
                            FileInfo file = new FileInfo(mydoc + "\\BlackRoger.sys");
                            file.Attributes = FileAttributes.Hidden;
                            controller.Start();
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

        public static void Log(string text)
        {
            Console.WriteLine(text);
            file = new StreamWriter(new FileStream(mydoc + "\\BlackRoger.log", System.IO.FileMode.Append));
            file.WriteLine(DateTime.Now + ": " + text);
            file.Flush();
            file.Close();
        }
    }
}
