using System;
using System.Text;
using System.Net.Sockets;
using System.IO;
using System.Threading;
using System.Net;
using System.Security.Cryptography;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Win32;

namespace ConsoleApplication1
{
    class Program
    {
        static string Version = "1.6";
        static string HashAlgo = "sha512";
        static bool versioncheck = false;
        static bool newplayer = false;
        static bool noudp = false;
        static bool baloonview = true;
        static bool downloaded = false;
        string md;
        static string Username;
        static bool LoggedIn = false;
        string mydoc = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
        static RegistryKey savekey = Registry.CurrentUser.CreateSubKey(@"software\Black Roger\");
        static RegistryKey readKey = Registry.CurrentUser.OpenSubKey(@"software\Black Roger\");
        static System.Diagnostics.Process MyProc = new System.Diagnostics.Process();

        class Server
        {
            // Global methods
            private static TcpListener tcpListener;
            private static Thread listenThread;
            static NetworkStream clientStream;
            static StreamWriter swSender;
            static UTF8Encoding encoder = new UTF8Encoding();

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
                        Console.WriteLine(encoder.GetString(message, 0, bytesRead));
                        if (encoder.GetString(message, 0, bytesRead).Contains("GET"))
                            Parse(encoder.GetString(message, 0, bytesRead));


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
                                        savekey.SetValue("AutoLogin", "1");
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
                            Console.WriteLine(split[1]);
                            break;
                        }
                    case "chat":
                        {
                            Messages = Chat();
                            for (int i = 0; i < 20; i++)
                            {
                                Message += Messages[i] + "\r\n";
                            }
                            Response(Message);
                            Console.WriteLine(Message);
                            break;
                        }
                    case "send":
                        {
                            //if (LoggedIn)
                                Send(split[2]);
                            //else
                            //{
                                //Response("Fail");
                            //}
                            Console.WriteLine(split[2]);
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
                    byte[] buffer = encoder.GetBytes(head + body);
                    clientStream.Write(buffer, 0, buffer.Length);
                    clientStream.Flush();
                    //swSender.WriteLine(head + body);
                    //swSender.Flush();
                }
                catch
                {
                    Console.WriteLine("Cant send response");
                }
            }
        }

        static void Main(string[] args)
        {
            Server server = new Server();
            Thread ServerStart = new Thread(new ThreadStart(server.StartServer));
            ServerStart.Start();
        }

        public static bool Login(string name, string pass)
        {
            try
            {
                WebClient client = new WebClient();
                Stream data = client.OpenRead("http://rogerpaladin.dyndns.org:7878/login/" + name.ToLower() + "/" + HashPassword(pass) + "/");
                //Stream data = client.OpenRead("http://192.168.1.33:7879/login/" + name.ToLower() + "/" + HashPassword(pass) + "/");
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
            string[] Messages = new string[21];
            int m = 0;
            char[] splitchar1 = { '\'' };
            WebClient client = new WebClient();
            Stream data = client.OpenRead("http://rogerpaladin.dyndns.org:7878/chat/");
            StreamReader reader = new StreamReader(data);
            string s = reader.ReadToEnd();
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
            Stream data = client.OpenRead("http://rogerpaladin.dyndns.org:7878/send/" + Username + "/All/" + text);
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
    }
}
