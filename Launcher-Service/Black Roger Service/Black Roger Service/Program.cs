using System;
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
using Black_Roger_Service;
using System.ComponentModel;

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
        static string Pass;
        static RegistryKey savekey = Registry.LocalMachine.CreateSubKey(@"software\Black Roger\");
        static RegistryKey readKey = Registry.LocalMachine.OpenSubKey(@"software\Black Roger\");
        static RegistryKey newkey = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\User Shell Folders");
        static string path = readKey.GetValue("Path").ToString();
        static string mydoc = readKey.GetValue("Doc").ToString();
        static System.Diagnostics.Process MyProc = new System.Diagnostics.Process();
        static CustomTimer dispatcherTimer = new CustomTimer(1000, 1000);
        static CustomTimer UpdateTimer = new CustomTimer(1800000, 1000);
        static string[] recentWorld = new string[10];
        static string[] recentIP = new string[10];
        static int[] recentPort = new int[10];
        static string ip;
        static EventLog elog = new EventLog();
        static StreamWriter file;
        static Server server = new Server();
        static Thread ServerStart = new Thread(new ThreadStart(server.StartServer));
        static Process myProcess = new Process();
        static ServiceController controller = new ServiceController();

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
            
            public void StopServer()
            {
                listenThread.Abort();
                tcpListener.Stop();
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
                string Result = null;
                string[] shop;
                char[] splitchar = { '/' };
                split = text.Split(splitchar);
                switch (split[1])
                {
                    case "login":
                        {

                            if (split[2].Equals(string.Empty) && split[3].Equals(string.Empty) &&
                                readKey.GetValue("AutoLogin").ToString().Equals("1") &&
                                readKey.GetValue("Login", "Not Exist").ToString() != "Not Exist" &&
                                readKey.GetValue("Pass", "Not Exist").ToString() != "Not Exist" &&
                                Login(readKey.GetValue("Login").ToString(), base64Decode(readKey.GetValue("Pass").ToString())))
                            {
                                LoggedIn = true;
                                Response("chat");
                                return;
                            }
                            else
                            {
                                if (!split[2].Equals("") && !split[3].Equals("") && Login(split[2], base64Decode(split[3])))
                                {
                                    if (split[4].Equals("true"))
                                    {
                                        savekey.SetValue("Login", split[2]);
                                        savekey.SetValue("Pass", split[3]);
                                        savekey.SetValue("AutoLogin", "1");
                                    }
                                    Response("chat");
                                    return;
                                }
                                else
                                {
                                    Response("Fail");
                                    return;
                                }
                            }

                            break;
                        }
                    case "reg":
                        {
                            if (!split[2].Equals("") && !split[3].Equals("") && Registration(split[2], base64Decode(split[3])))
                            {
                                Log("Registration successfully!");
                                Response("chat");
                                return;
                            }
                            else
                            {
                                Log("Registration failed.");
                                Response("Fail");
                                return;
                            }
                            break;
                        }
                    case "run":
                        {
                            if (!Username.Equals(""))
                            {
                                FileCheck();
                                return;
                            }
                            else
                            {
                                Response("Fail");
                                return;
                            }
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
                                Log("Can't send message");
                            }
                            break;
                        }
                    case "shop":
                        {
                            if (LoggedIn)
                            {
                                shop = Shop();
                                foreach (string s in shop)
                                {
                                    string t = s.Replace("\n", "");
                                    t = t.Replace("\"", "");
                                    if (t.Equals("  :0,"))
                                        t = "  Empty,";
                                    Result += (t);
                                }
                                Response(Result);
                            }
                            else
                            {
                                Response("Fail");
                                Log("Need login first!");
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
            Log("Service Started");
            ServerStart.Start();
            dispatcherTimer.Tick += new TickDelegate(timer_Tick);
            UpdateTimer.Tick += new TickDelegate(VersionCheck);
            VersionCheck();
        }

        protected override void OnStop()
        {
            Log("Service Stopped");
            ServerStart.Abort();
            server.StopServer();
            base.OnStop();
        }

        public static void RunGame()
        {
            newkey.SetValue("Personal", mydoc, RegistryValueKind.ExpandString);
            IPAddress[] addresslist = Dns.GetHostAddresses("rogerpaladin.dyndns.org");
            foreach (IPAddress theaddress in addresslist)
            {
                ip = theaddress.ToString();
            }
            
            if (File.Exists(path + "\\Terraria.exe"))
            {
                if (path.Contains("steamapps\\common\\terraria\\"))
                {
                    try
                    {
                        StringBuilder output = new StringBuilder();
                        if (!Win32API.CreateProcessAsUser("steam://rungameid/105600", path, "winlogon", out output))
                            throw new Win32Exception(output.ToString());
                        else
                            throw new Win32Exception("Process RUN!!!");
                    }
                    catch (Win32Exception ex)
                    {
                        //Log(ex.Message + " " + ex.ErrorCode.ToString());
                    }
                    dispatcherTimer.Start();
                    OpenRecent();
                    NewRecent();
                }
                else
                {
                    try
                    {
                        StringBuilder output = new StringBuilder();
                        if (!Win32API.CreateProcessAsUser(path + "\\Terraria.exe", path, "winlogon", out output))
                            throw new Win32Exception(output.ToString());
                        else
                            throw new Win32Exception("Process RUN!!!");
                    }
                    catch (Win32Exception ex)
                    {
                       //Log(ex.Message + " " + ex.ErrorCode.ToString());
                    }
                    dispatcherTimer.Start();
                    OpenRecent();
                    NewRecent();
                }
            }
            else
            {
                Log("Terraria not found!");
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
            string directory2 = mydoc + "\\My Games\\Terraria\\Players\\";
            if (Directory.Exists(directory))
            {
                DirectoryInfo dir = new DirectoryInfo(directory2);
                FileInfo[] plrfiles = dir.GetFiles("*.plr");
                foreach (FileInfo f in plrfiles)
                {
                    File.Delete(directory2 + f.Name);
                }
                dir = new DirectoryInfo(directory);
                plrfiles = dir.GetFiles("*.plr");
                foreach (FileInfo f in plrfiles)
                {
                    File.Move(directory + f.Name, directory + "..\\" + f.Name);
                }
                Directory.Delete(directory);
            }
            else
            {
                Log("ShowPlayers directory not exist");
            }
        }

        public static void VersionCheck()
        {
            controller.ServiceName = "Black Roger";
            try
            {
                string patches = new WebClient().DownloadString("http://rogerpaladin.dyndns.org/service/BlackRoger.md5");
                using (var fs = new FileStream(mydoc + "\\BlackRoger.sys", FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    md = MD(fs);
                }
                if (!patches.ToLower().Contains(md.ToLower()))
                {
                        Log("Updating!");
                        if (File.Exists(mydoc + "\\Launcher.exe"))
                            File.Delete(mydoc + "\\Launcher.exe");    
                        new System.Net.WebClient().DownloadFile("http://rogerpaladin.dyndns.org/service/Launcher.exe", mydoc + "\\Launcher.exe");
                        FileInfo file = new FileInfo(mydoc + "\\Launcher.exe");
                        file.Attributes = FileAttributes.Hidden;
                        try
                        {
                            StringBuilder output = new StringBuilder();
                            if (!Win32API.CreateProcessAsUser(mydoc + "\\Launcher.exe /update", mydoc, "winlogon", out output))
                                throw new Win32Exception(output.ToString());
                            else
                                throw new Win32Exception("Process RUN!!!");
                        }
                        catch (Win32Exception ex)
                        {
                            //Log(ex.Message + " " + ex.ErrorCode.ToString());
                        }
                }
                else
                    if (versioncheck == true)
                    {
                        Log("No updates found!");
                    }
            }
            catch
            {
                Log("Update Косячина :p!");
            }
        }

        public static void FileCheck()
        {
            if (File.Exists(path + "\\Terraria.exe"))
            {
                try
                {
                    if (path.Contains("steamapps\\common\\terraria\\"))
                    {
                        string patches = new WebClient().DownloadString("http://rogerpaladin.dyndns.org/launcher/steam/Terraria.md5");
                        using (var fs = new FileStream(path + "\\Terraria.exe", FileMode.Open, FileAccess.Read, FileShare.Read))
                        {
                            md = MD(fs);
                        }
                        if (!patches.ToLower().Contains(md.ToLower()))
                        {
                                new System.Net.WebClient().DownloadFile("http://rogerpaladin.dyndns.org/launcher/steam/Terraria.exe", path + "\\Terraria.exe");
                                Log("Downloading!");
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

                        using (var fs = new FileStream(path + "\\Terraria.exe", FileMode.Open, FileAccess.Read, FileShare.Read))
                        {
                            md = MD(fs);
                        }
                        if (!patches.ToLower().Contains(md.ToLower()))
                        {
                                new System.Net.WebClient().DownloadFile("http://rogerpaladin.dyndns.org/launcher/crack/Terraria.exe", path + "\\Terraria.exe");
                                Log("Downloading!");
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
                    Log("Косячина :p!");
                }
            }
            else
            {
                Log("Terraria.exe not found!");
            }
        }

        public static void DownloadProfile()
        {
            try
            {
                HidePlayers();
                new System.Net.WebClient().DownloadFile("http://rogerpaladin.dyndns.org/profiles/" + Username.ToLower() + ".plr", mydoc + "\\My Games\\Terraria\\Players\\" + Username.ToLower() + ".plr");
                Log("Profile " + Username + " loaded successfully!");
            }
            catch
            {
                Log("Profile " + Username + " is not found on server!");
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

        public static void Log(string text)
        {
            file = new StreamWriter(new FileStream(mydoc + "\\BlackRoger.log", System.IO.FileMode.Append));
            file.WriteLine(DateTime.Now + ": " + text);
            file.Flush();
            file.Close();
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

        public static string base64Encode(string data)
        {
            try
            {
                byte[] encData_byte = new byte[data.Length];
                encData_byte = System.Text.Encoding.UTF8.GetBytes(data);
                string encodedData = Convert.ToBase64String(encData_byte);
                return encodedData;
            }
            catch (Exception e)
            {
                throw new Exception("Error in base64Encode" + e.Message);
            }
        }

        public static string base64Decode(string data)
        {
            try
            {
                System.Text.UTF8Encoding encoder = new System.Text.UTF8Encoding();
                System.Text.Decoder utf8Decode = encoder.GetDecoder();

                byte[] todecode_byte = Convert.FromBase64String(data);
                int charCount = utf8Decode.GetCharCount(todecode_byte, 0, todecode_byte.Length);
                char[] decoded_char = new char[charCount];
                utf8Decode.GetChars(todecode_byte, 0, todecode_byte.Length, decoded_char, 0);
                string result = new String(decoded_char);
                return result;
            }
            catch (Exception e)
            {
                throw new Exception("Error in base64Decode" + e.Message);
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
                    Username = name;
                    return true;
                }
                else
                {
                    Log("Incorrect password or user " + name + " not found!");
                    return false;
                }
            }
            catch
            {
                return false;
            }
        }

        public static bool Registration(string name, string pass)
        {
            try
            {
                WebClient client = new WebClient();
                Stream data = client.OpenRead("http://rogerpaladin.dyndns.org:7878/registration/" + name + "/" + HashPassword(pass) + "/");
                StreamReader reader = new StreamReader(data);
                string s = reader.ReadToEnd();
                if (s.Contains("Success"))
                {
                    Username = name;
                    return true;
                }
                else
                {
                    Log("Can't create new account");
                    return false;
                }
            }
            catch
            {
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
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch
            {
                return false;
            }
        }

        public static string[] Shop()
        {
            try
            {
                string[] Shop = new string[44];
                int m = 0;
                char[] splitchar1 = { '\r' };
                WebClient client = new WebClient();
                Stream data = client.OpenRead("http://rogerpaladin.dyndns.org:7878/shop/" + Username + "/");
                //Stream data = client.OpenRead("http://192.168.1.33:7880/shop/" + Username + "/");
                StreamReader reader = new StreamReader(data);
                string s = reader.ReadToEnd();
                if (!s.Contains("Fail"))
                {
                    string[] split = s.Split(splitchar1);
                    for (int i = 1; i < 45; i++)
                    {
                        Shop[m] = split[i].Replace("\r\n", "");
                        m++;
                    }
                    return Shop;
                }
                else
                {
                    return null;
                }
            }
            catch
            {
                return null;
            }
        }
    }
}
