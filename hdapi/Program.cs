using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace hdapi
{
    class Program
    {
        private static Socket apiClient = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        private static ManualResetEvent connectDone = new ManualResetEvent(false);
        private static ManualResetEvent sendDone = new ManualResetEvent(false);
        public static string symbol = "";
        static void Main(string[] args)
        {
            symbol = args[0];//command line input            
            //symbol = "PA"; //Gold
            //Console.WriteLine(symbol);

            IPAddress[] srvIp = Dns.GetHostAddresses("localhost");
            IPEndPoint remoteEP = new IPEndPoint(srvIp[0], 58588);

            apiClient = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            apiClient.BeginConnect(remoteEP, new AsyncCallback(ConnectCallback), apiClient);
            connectDone.WaitOne();            
            Send(apiClient, string.Format("{0}\r\n", symbol));
            Receive(apiClient);

            Console.Read();
        }

        private static void ConnectCallback(IAsyncResult ar)
        {
            Socket client = (Socket)ar.AsyncState;
            client.EndConnect(ar);
            connectDone.Set();
        }

        private static void Send(Socket client, String data)
        {
            try
            {
                byte[] byteData = Encoding.ASCII.GetBytes(data);
                client.BeginSend(byteData, 0, byteData.Length, 0, new AsyncCallback(SendCallback), client);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        private static void SendCallback(IAsyncResult ar)
        {
            try
            {
                Socket client = (Socket)ar.AsyncState;
                int bytesSent = client.EndSend(ar);

                sendDone.Set();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        private static void Receive(Socket client)
        {
            StateObject state = new StateObject();
            state.workSocket = client;
            client.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0, new AsyncCallback(ReceiveCallback), state);            
        }

        public class StateObject
        {
            public Socket workSocket = null;
            public const int BufferSize = 500000; //500k
            public byte[] buffer = new byte[BufferSize];
            public StringBuilder sb = new StringBuilder();
        }

        private static void ReceiveCallback(IAsyncResult ar)
        {
            StateObject state = (StateObject)ar.AsyncState;
            Socket client = state.workSocket;
            if (apiClient.Connected)
            {
                int bytesRead = client.EndReceive(ar);
                if (bytesRead > 0)
                {
                    string filename = string.Format("{0}.csv", symbol);
                    if(File.Exists(filename))
                    {
                        File.Delete(filename);
                    }

                    string msg = Encoding.Default.GetString(state.buffer, 0, bytesRead);
                    Console.WriteLine(msg);
                    File.WriteAllText(filename, msg);                    
                }
            }
        }
    }
}
