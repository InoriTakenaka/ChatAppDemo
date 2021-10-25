using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Net;

namespace ConsoleApp {
    public delegate void ClientDisconnect(object sender, EventArgs e);
    class Session {

        public event ClientDisconnect OnClientDisconnected;

        Socket client_;
        public Session(Socket client) {
            client_ = client;
        }

        public void RequestHandler() {
            while (!IsAlive()) {
                int pid = Thread.CurrentThread.ManagedThreadId;
                string msg = Recive();
                if (string.IsNullOrEmpty(msg))
                    continue;
                else {
                    Console.WriteLine($"Thread{pid}::{DateTime.Now.ToLongDateString()}::RECIVE::{msg}");
                }
            }
            OnClientDisconnected(this, new EventArgs());
        }

        public void StartSession() {
            Task.Run(RequestHandler);
        }

        public string Recive() {
            byte[] buffer = new byte[512];
            //
            int rec;

            try {
                rec = client_.Receive(buffer, SocketFlags.None);

                if (rec > 0) {

                    string msg = Encoding.UTF8.GetString(buffer);

                    string echo = $"[{DateTime.Now.ToShortTimeString()}]::SENT::{msg}";

                    Console.WriteLine(echo);

                    byte[] msgBuffer = Encoding.UTF8.GetBytes(echo);
                    int sent = client_.Send(msgBuffer, SocketFlags.None);
                    if (sent == msgBuffer.Count()) {
                        Console.WriteLine($"[{DateTime.Now.ToShortTimeString()}]echo success::{echo}");
                    }
                    return msg;
                }
            } catch (SocketException) {
                Console.WriteLine($"[{ DateTime.Now.ToShortTimeString()} occur, remote host disconnected.");
            } finally {
                this.client_.Close();
                this.client_ = null;
                this.OnClientDisconnected(this, new EventArgs());
            }

            return string.Empty;
        }
        /// <summary>
        /// true if the connection has been closed, reset, or terminated;
        /// otherwise, returns false.
        /// </summary>
        /// <returns></returns>
        public bool IsAlive() {

            return client_.Poll(100, SelectMode.SelectRead);
        }

        internal Task<bool> SendToClient(byte[] buffer) {
            throw new NotImplementedException();
        }
    }
    class SocketServer {

        Socket socket_;
        string host_;
        int port_;
        List<Session> clients_;
        private void CreateInstance() {
            socket_ = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        }
        public SocketServer(string host, int port) {
            host_ = host;
            port_ = port;
            clients_ = new List<Session>();
            CreateInstance();
        }

        public void StasrtListen() {
            IPEndPoint endPoint = new IPEndPoint(IPAddress.Parse(host_), port_);
            socket_.Bind(endPoint);
            Listen();
        }

        public async Task<bool> SendToClient(byte[] buffer, Session session) {
            try {
                bool sendResult = await session.SendToClient(buffer);
                if (sendResult) {
                    Console.WriteLine($"[{DateTime.Now.ToShortTimeString()}]::message write to client successfully!");
                    return true;
                } else throw new InvalidOperationException();
            } catch (Exception err) {
                Console.WriteLine($"[{DateTime.Now.ToShortTimeString()}]::message write to client failed!{err.Message}");
                return false;
            }
        }

        private void Listen() {
            while (true) {
                socket_.Listen(10);
                Socket client = socket_.Accept();
                Session session = new Session(client);
                session.StartSession();
                session.OnClientDisconnected += ClientDisconnectHandler;
                clients_.Add(session);
            }
        }

        private void ClientDisconnectHandler(object sender, EventArgs e) {
            var client = sender as Session;
            clients_.Remove(client);
        }
    }
    class Program {

        static void Main(string[] args) {
            SocketServer server = new SocketServer("127.0.0.1", 8000);
            server.StasrtListen();
        }
    }
}
