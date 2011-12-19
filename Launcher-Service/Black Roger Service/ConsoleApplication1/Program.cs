using System;
using System.Text;
using System.Net.Sockets;
using System.IO;
using System.Threading;
using System.Net;

namespace ConsoleApplication1
{
    class Program
    {
        class Server
        {
            // Global methods
            private static TcpListener tcpListener;
            private static Thread listenThread;
            static NetworkStream clientStream;
            static StreamWriter swSender;

            private string endl = "\r\n";

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
                swSender = new System.IO.StreamWriter(clientStream);


                byte[] message = new byte[4096];
                int bytesRead;

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

                    // Message has successfully been received
                    ASCIIEncoding encoder = new ASCIIEncoding();

                    // Output message
                    //Console.WriteLine("To: " + tcpClient.Client.LocalEndPoint);
                    //Console.WriteLine("From: " + tcpClient.Client.RemoteEndPoint);
                    //Console.WriteLine(encoder.GetString(message, 0, bytesRead));
                    if (encoder.GetString(message, 0, bytesRead).Contains("GET"))
                        Parse(encoder.GetString(message, 0, bytesRead));


                } while (clientStream.DataAvailable);

                // Release connections
                clientStream.Close();
                tcpClient.Close();
            }

            private static void Parse(string text)
            {
                string[] split;
                char[] splitchar = { '/' };
                split = text.Split(splitchar);
                switch (split[1])
                {
                    case "login":
                        {
                            Console.WriteLine(split[1]);
                            Response("Fai2l2");

                            break;
                        }
                    case "run":
                        {
                            Console.WriteLine(split[1]);
                            break;
                        }
                }
            }

            private static void Response(string text)
            {
                string body = null;
                string head = null;
                head += ("HTTP/1.0 200 OK\n");
                head += ("Content-Type: text/html\n");
                head += ("Connection: close\n");
                head += ("\n");
                body = text + "\n";
                swSender.WriteLine(head + body);
                swSender.Flush();
            }
        }

        static void Main(string[] args)
        {
            Server server = new Server();
            Thread ServerStart = new Thread(new ThreadStart(server.StartServer));
            ServerStart.Start();
        }
    }
}
