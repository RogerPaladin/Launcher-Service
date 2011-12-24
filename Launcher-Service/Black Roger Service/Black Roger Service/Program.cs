﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using Microsoft.Win32;

namespace Service
{
    class Program : ServiceBase
    {
        static string Version = "1.6";
        static string HashAlgo = "sha512";
        static bool versioncheck = false;
        static bool newplayer = false;
        static bool downloaded = false;
        static bool LoggedIn = false;
        static string md = string.Empty;
        static string Username;
        static string mydoc = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
        static RegistryKey savekey = Registry.CurrentUser.CreateSubKey(@"software\Black Roger\");
        static RegistryKey readKey = Registry.CurrentUser.OpenSubKey(@"software\Black Roger\");
        //static string path = readKey.GetValue("Path").ToString();
        static string path = null;
        static System.Diagnostics.Process MyProc = new System.Diagnostics.Process();
        static CustomTimer dispatcherTimer = new CustomTimer(1000, 1000);
        static string[] recentWorld = new string[10];
        static string[] recentIP = new string[10];
        static int[] recentPort = new int[10];
        static string ip;

        class Server
        {
            // Global methods
            private static TcpListener tcpListener;
            private static Thread listenThread;
            static NetworkStream clientStream;
            static UTF8Encoding utf8 = new UTF8Encoding();
            static Encoding win1251 = Encoding.GetEncoding("Windows-1251");

            /// <summary>
            /// Main server method
            /// </summary>
            public void StartServer()
            {
                tcpListener = new TcpListener(IPAddress.Any, 9005);
                listenThread = new Thread(new ThreadStart(ListenForClients));
                listenThread.Start();
            }

            /// <summary>
            /// Listens for client connections
            /// </summary>
            private static void ListenForClients()
            {
                tcpListener.Start();

                while (true)
                {
                    try
                    {
                        // Blocks until a client has connected to the server
                        TcpClient client = tcpListener.AcceptTcpClient();

                        // Create a thread to handle communication
                        // with connected client
                        Thread clientThread = new Thread(new ParameterizedThreadStart(HandleClientComm));
                        clientThread.Start(client);
                    }
                    catch (Exception)
                    {

                    }
                }
            }

            /// <summary>
            /// Handles client connections
            /// </summary>
            /// <param name="client"></param>
            private static void HandleClientComm(object client)
            {
                TcpClient tcpClient = (TcpClient)client;
                clientStream = tcpClient.GetStream();
                //swSender = new System.IO.StreamWriter(clientStream);


                byte[] message = new byte[4096];
                int bytesRead;

                try
                {
                    do
                    {
                        bytesRead = 0;

                        try
                        {
                            // Blocks until a client sends a message                    
                            bytesRead = clientStream.Read(message, 0, 4096);
                        }
                        catch (Exception)
                        {
                            // A socket error has occured
                            break;
                        }

                        if (bytesRead == 0)
                        {
                            // The client has disconnected from the server
                            break;
                        }

                        // Output message
                        //Console.WriteLine("To: " + tcpClient.Client.LocalEndPoint);
                        //Console.WriteLine("From: " + tcpClient.Client.RemoteEndPoint);
                        Console.WriteLine(utf8.GetString(message, 0, bytesRead));
                        if (utf8.GetString(message, 0, bytesRead).Contains("GET"))
                            Parse(utf8.GetString(message, 0, bytesRead));


                    } while (clientStream.DataAvailable);
                }
                catch
                {
                }

                // Release connections
                clientStream.Close();
                tcpClient.Close();
            }

            private static void Parse(string text)
            {
                string[] split;
                string[] Messages;
                string Message = null;
                char[] splitchar = { '/' };
                split = text.Split(splitchar);
                switch (split[1])
                {
                    case "login":
                        {
                            if (!LoggedIn)
                            {
                                if (Login(split[2], split[3]))
                                {
                                    if (split[4].Equals("true"))
                                    {
                                        savekey.SetValue("Login", split[2]);
                                        savekey.SetValue("Pass", split[3]);
                                        savekey.SetValue("AutoLogin", "1");
                                    }
                                    LoggedIn = true;
                                    Response("chat");
                                }
                                else
                                    Response("Fail");
                            }
                            else
                            {
                                Response("Fail");
                            }

                            break;
                        }
                    case "run":
                        {
                            FileCheck();
                            break;
                        }
                    case "chat":
                        {
                            Messages = Chat();
                            for (int i = 0; i < 20; i++)
                            {
                                Message += (Messages[i] + "\r\n");
                            }
                            Response(Message);
                            break;
                        }
                    case "send":
                        {
                            if (LoggedIn)
                                Send((split[2]));
                            else
                            {
                                Response("Fail");
                                Console.WriteLine("Fail send");
                            }
                            break;
                        }
                }
            }

            private static void Response(string text)
            {
                try
                {
                    string body = null;
                    string head = null;
                    head += ("HTTP/1.0 200 OK\n");
                    head += ("Content-Type: text/html\n");
                    head += ("Connection: close\n");
                    head += ("\n");
                    body = text.Trim();
                    byte[] buffer = win1251.GetBytes(head + body);
                    clientStream.Write(buffer, 0, buffer.Length);
                    clientStream.Flush();
                }
                catch
                {
                    Console.WriteLine("Cant send response");
                }
            }
        }

        static void Main(string[] args)
        {
            ServiceBase.Run(new Program());
        }

        public Program()
        {
            this.ServiceName = "Black Roger";
        }

        protected override void OnStart(string[] args)
        {
            base.OnStart(args);

            Server server = new Server();
            Thread ServerStart = new Thread(new ThreadStart(server.StartServer));
            ServerStart.Start();
            dispatcherTimer.Tick += new TickDelegate(timer_Tick);
        }

        protected override void OnStop()
        {
            base.OnStop();

            //TODO: clean up any variables and stop any threads
        }

        public static void RunGame()
        {
            IPAddress[] addresslist = Dns.GetHostAddresses("rogerpaladin.dyndns.org");
            foreach (IPAddress theaddress in addresslist)
            {
                ip = theaddress.ToString();
            }
            
            if (File.Exists(path + "Terraria.exe"))
            {
                if (path.Contains("steamapps\\common\\terraria\\"))
                {
                    MyProc.StartInfo.FileName = "steam://rungameid/105600";
                    MyProc.Start();
                    dispatcherTimer.Start();
                    OpenRecent();
                    NewRecent();
                }
                else
                {
                    MyProc.StartInfo.FileName = path + "Terraria.exe";
                    MyProc.Start();
                    dispatcherTimer.Start();
                    OpenRecent();
                    NewRecent();
                }
            }
            else
            {
                Console.WriteLine("Terraria not found!");
            }
        }

        private static void HidePlayers()
        {
            string directory = mydoc + "\\My Games\\Terraria\\Players\\";
            DirectoryInfo dir = new DirectoryInfo(directory);
            FileInfo[] plrfiles = dir.GetFiles("*.plr");
            if (!Directory.Exists(mydoc + "\\My Games\\Terraria\\Players\\tmp\\"))
                dir.CreateSubdirectory("tmp");
            foreach (FileInfo f in plrfiles)
            {
                File.Move(directory + f.Name, directory + "tmp\\" + f.Name);
            }
        }

        private static void ShowPlayers()
        {
            string directory = mydoc + "\\My Games\\Terraria\\Players\\tmp\\";
            if (Directory.Exists(directory))
            {
                DirectoryInfo dir = new DirectoryInfo(directory);
                FileInfo[] plrfiles = dir.GetFiles("*.plr");
                foreach (FileInfo f in plrfiles)
                {
                    File.Move(directory + f.Name, directory + "..\\" + f.Name);
                }
                Directory.Delete(directory);
            }
            else
            {
                Console.WriteLine("ShowPlayers directory not exist");
            }
        }

        public static void VersionCheck()
        {
            try
            {
                string patches = new WebClient().DownloadString("http://rogerpaladin.dyndns.org/launcher/Launcher.md5");
                using (var fs = new FileStream(path + "Launcher.exe", FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    md = MD(fs);
                }
                if (!patches.ToLower().Contains(md.ToLower()))
                {
                        Console.WriteLine("Updating!");
                        new System.Net.WebClient().DownloadFile("http://rogerpaladin.dyndns.org/launcher/Launcher.exe", path + "Launcher1.exe");
                        FileInfo file = new FileInfo(path + "Launcher1.exe");
                        file.Attributes = FileAttributes.Hidden;
                        MyProc.StartInfo.FileName = path + "Launcher1.exe";
                        MyProc.Start();
                        Process.GetCurrentProcess().Kill();
                }
                else
                    if (versioncheck == true)
                    {
                        Console.WriteLine("No updates found!");
                    }
            }
            catch
            {
                Console.WriteLine("Косячина :p!");
            }
        }

        public static void FileCheck()
        {
            if (File.Exists(path + "Terraria.exe"))
            {
                try
                {
                    if (path.Contains("steamapps\\common\\terraria\\"))
                    {
                        string patches = new WebClient().DownloadString("http://rogerpaladin.dyndns.org/launcher/steam/Terraria.md5");
                        using (var fs = new FileStream(path + "Terraria.exe", FileMode.Open, FileAccess.Read, FileShare.Read))
                        {
                            md = MD(fs);
                        }
                        if (!patches.ToLower().Contains(md.ToLower()))
                        {
                                new System.Net.WebClient().DownloadFile("http://rogerpaladin.dyndns.org/launcher/steam/Terraria.exe", path + "Terraria.exe");
                                Console.WriteLine("Downloading!");
                                RunGame();
                        }
                        else
                        {
                            RunGame();
                        }
                    }
                    else
                    {
                        string patches = new WebClient().DownloadString("http://rogerpaladin.dyndns.org/launcher/crack/Terraria.md5");

                        using (var fs = new FileStream(path + "Terraria.exe", FileMode.Open, FileAccess.Read, FileShare.Read))
                        {
                            md = MD(fs);
                        }
                        if (!patches.ToLower().Contains(md.ToLower()))
                        {
                                new System.Net.WebClient().DownloadFile("http://rogerpaladin.dyndns.org/launcher/crack/Terraria.exe", path + "Terraria.exe");
                                Console.WriteLine("Downloading!");
                                RunGame();
                        }
                        else
                        {
                            RunGame();
                        }
                    }
                }
                catch
                {
                    Console.WriteLine("Косячина :p!");
                }
            }
            else
            {
                Console.WriteLine("Terraria.exe not found!");
            }
        }

        public static void DownloadProfile()
        {
            try
            {
                HidePlayers();
                new System.Net.WebClient().DownloadFile("http://rogerpaladin.dyndns.org/profiles/" + Username.ToLower() + ".plr", mydoc + "\\My Games\\Terraria\\Players\\" + Username.ToLower() + ".plr");
                Console.WriteLine("Profile " + Username + " loaded successfully!");
            }
            catch
            {
                Console.WriteLine("Profile " + Username + " is not found on server!");
            }
        }

        public static void OpenRecent()
        {
            if (File.Exists(mydoc + "\\My Games\\Terraria\\servers.dat"))
                using (FileStream fileStream = new FileStream(mydoc + "\\My Games\\Terraria\\servers.dat", FileMode.Open))
                {
                    using (BinaryReader binaryReader = new BinaryReader(fileStream))
                    {
                        binaryReader.ReadInt32();
                        for (int i = 0; i < 10; i++)
                        {
                            recentWorld[i] = binaryReader.ReadString();
                            recentIP[i] = binaryReader.ReadString();
                            recentPort[i] = binaryReader.ReadInt32();
                        }
                    }
                }
        }

        public static void SaveRecent()
        {
            using (FileStream fileStream = new FileStream(mydoc + "\\My Games\\Terraria\\servers.dat", FileMode.Create))
            {
                using (BinaryWriter binaryWriter = new BinaryWriter(fileStream))
                {
                    binaryWriter.Write(37);
                    for (int i = 0; i < 10; i++)
                    {
                        binaryWriter.Write(recentWorld[i]);
                        binaryWriter.Write(recentIP[i]);
                        binaryWriter.Write(recentPort[i]);
                    }
                }
            }
        }

        public static void NewRecent()
        {
            for (int i = 0; i < 10; i++)
            {
                if (recentIP[i] == ip && recentPort[i] == 7777)
                {
                    for (int j = i; j < 9; j++)
                    {
                        recentIP[j] = recentIP[j + 1];
                        recentPort[j] = recentPort[j + 1];
                        recentWorld[j] = recentWorld[j + 1];
                    }
                }
            }
            for (int k = 9; k > 0; k--)
            {
                recentIP[k] = recentIP[k - 1];
                recentPort[k] = recentPort[k - 1];
                recentWorld[k] = recentWorld[k - 1];
            }
            recentIP[0] = ip;
            recentPort[0] = 7777;
            recentWorld[0] = "Black Roger";
            SaveRecent();
        }

        public static void timer_Tick()
        {
            if (Process.GetProcesses().Any(clsProcess => clsProcess.ProcessName.Equals("Terraria")))
            {
                if (!downloaded && !newplayer)
                {
                    DownloadProfile();
                    downloaded = true;
                }
            }
            else
            {
                if (downloaded && !newplayer)
                {
                    dispatcherTimer.Stop();

                    if (File.Exists(mydoc + "\\My Games\\Terraria\\Players\\" + Username + ".plr"))
                        File.Delete(mydoc + "\\My Games\\Terraria\\Players\\" + Username + ".plr");

                    if (File.Exists(mydoc + "\\My Games\\Terraria\\Players\\" + Username + ".plr.bak"))
                        File.Delete(mydoc + "\\My Games\\Terraria\\Players\\" + Username + ".plr.bak");

                    ShowPlayers();
                    downloaded = false;
                }
            }
        }

        public static string HashPassword(string password)
        {
            if (string.IsNullOrEmpty(password) || password == "non-existant password")
                return "non-existant password";

            Func<HashAlgorithm> func;
            if (!HashTypes.TryGetValue(HashAlgo.ToLower(), out func))
                throw new NotSupportedException(String.Format("Hashing algorithm {0} is not supported", (HashAlgo.ToLower())));

            using (var hash = func())
            {
                var bytes = hash.ComputeHash(Encoding.ASCII.GetBytes(password));
                return bytes.Aggregate("", (s, b) => s + b.ToString("X2"));
            }
        }

        public static string MD(Stream stream)
        {
            using (var sha = MD5CryptoServiceProvider.Create())
            {
                var bytes = sha.ComputeHash(stream);
                return bytes.Aggregate("", (s, b) => s + b.ToString("X2"));
            }
        }

        public static readonly Dictionary<string, Func<HashAlgorithm>> HashTypes = new Dictionary<string, Func<HashAlgorithm>>
        {
            {"sha512", () => new SHA512Managed()},
            {"sha256", () => new SHA256Managed()},
            {"md5", () => new MD5Cng()},
            {"sha512-xp", () => SHA512.Create()},
            {"sha256-xp", () => SHA256.Create()},
            {"md5-xp", () => MD5.Create()},
        };
        
        public static bool Login(string name, string pass)
        {
            try
            {
                WebClient client = new WebClient();
                Stream data = client.OpenRead("http://rogerpaladin.dyndns.org:7878/login/" + name.ToLower() + "/" + HashPassword(pass) + "/");
                StreamReader reader = new StreamReader(data);
                string s = reader.ReadToEnd();
                if (s.Contains("Success"))
                {
                    Console.WriteLine("Success!");
                    Username = name;
                    return true;
                }
                else
                {
                    Console.WriteLine("Incorrect password or user " + name + " not found!");
                    return false;
                }
            }
            catch
            {
                Console.WriteLine("Can't connect to DB");
                return false;
            }
        }

        public static string[] Chat()
        {
            Encoding win1251 = Encoding.GetEncoding("Windows-1251");
            string[] Messages = new string[21];
            int m = 0;
            char[] splitchar1 = { '\'' };
            byte[] bytes;
            char[] chars;
            WebClient client = new WebClient();
            bytes = client.DownloadData("http://rogerpaladin.dyndns.org:7878/chat/");
            chars = win1251.GetChars(bytes);
            string s = new string(chars);
            string[] split1 = s.Split(splitchar1);
            for (int i = 1; i < 41; i++)
            {
                Messages[m] = split1[i];
                i = i + 1;
                m++;
            }
            return Messages;
        }

        public static bool Send(string text)
        {
            try
            {
                WebClient client = new WebClient();
                Stream data = client.OpenRead("http://rogerpaladin.dyndns.org:7878/send/" + Username + "/All/" + text + "/");
                //Stream data = client.OpenRead("http://192.168.1.33:7879/send/" + Username + "/All/" + text + "/");
                StreamReader reader = new StreamReader(data);
                string s = reader.ReadToEnd();
                if (s.Contains("Success"))
                {
                    Console.WriteLine("Success!");
                    return true;
                }
                else
                {
                    Console.WriteLine("Fail");
                    return false;
                }
            }
            catch
            {
                Console.WriteLine("Can't connect to DB");
                return false;
            }
        }
    }
}
