using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Net;


namespace ChatClient {
    class Client {
        private Socket socket_;
        public Client() {
            socket_ = new Socket(SocketType.Stream, ProtocolType.Tcp);
        }

        public bool
        Connect(string IpAddress, int port) {
            IPEndPoint endPoint = new IPEndPoint(IPAddress.Parse(IpAddress), port);
            IAsyncResult iar = socket_.BeginConnect(endPoint, null, null);
            bool connectSuccess = iar.AsyncWaitHandle.WaitOne(2000, true);
            if (connectSuccess) {
                Console.WriteLine("connect success.");
                return true;
            } else {
                Console.WriteLine("connection timeout.");
                return false;
            }
        }

        public bool
        SendMessage(byte[] buffer) {
            int count = socket_.Send(buffer, 0, buffer.Length, SocketFlags.None);
            Console.WriteLine($"send to server , {count} bytes");
            byte[] rec = new byte[1024];
            socket_.Receive(rec, 0, 1024, SocketFlags.None);
            string msg = Encoding.UTF8.GetString(rec);
            Console.WriteLine(msg);
            return true;
        }
    }
    class Program {
        static void Main(string[] args) {
            Client client = new Client();
            bool result = client.Connect("127.0.0.1", 8000);
            if (result) {
                while (true) {
                    string msg = Console.ReadLine();
                    byte[] buffer = Encoding.UTF8.GetBytes(msg);
                    client.SendMessage(buffer);
                }
            }
        }
    }
}
