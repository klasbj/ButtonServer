/*
 * Copyright (c) 2013, Klas Björkqvist
 * See COPYING.txt for license information
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Net;
using System.Net.Sockets;

namespace ButtonServer
{
    class Server
    {
        private static Queue<string> messages = new Queue<string>();

        public static void EnqueueMessage(string s)
        {
            lock (messages)
            {
                messages.Enqueue(s);
            }
        }

        public static void Run()
        {
            try
            {
                Server server = new Server(Properties.Settings.Default.ServerPort);

                while (true)
                {
                    lock (messages)
                    {
                        while (messages.Count > 0)
                        {
                            string msg = messages.Dequeue();
                            // send message
                            server.Broadcast(msg);

                        }
                    }
                    Thread.Sleep(25);
                }
            }
            catch (ThreadAbortException)
            {
            }

            return;
        }

        private class Client
        {
            public Socket socket = null;
            public byte[] buffer = new byte[1024];
        }

        Socket listener;

        List<Client> clients = new List<Client>();

        private Server(int port)
        {
            listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            listener.Bind(new IPEndPoint(IPAddress.Any, port));
            listener.Listen(10);
            listener.BeginAccept(new AsyncCallback(AcceptCB), listener);
        }

        private void Broadcast(string msg)
        {
            Console.WriteLine("Server sends: " + msg);

            byte[] buf = Encoding.UTF8.GetBytes(msg + "\n");

            lock (clients)
            {
                foreach (var c in clients)
                {
                    if (c.socket != null && c.socket.Connected)
                    {
                        lock (c.socket)
                        {
                            try
                            {
                                c.socket.Send(buf, SocketFlags.None);
                            }
                            catch (SocketException) { }
                        }
                    }
                }
            }
            


        }

        private void AcceptCB(IAsyncResult ar)
        {
            Client cl = new Client();
            Socket listener = (Socket)ar.AsyncState;
            cl.socket = listener.EndAccept(ar);
            listener.BeginAccept(new AsyncCallback(AcceptCB), listener);

            cl.socket.BeginReceive(cl.buffer, 0, cl.buffer.Length, SocketFlags.None,
                new AsyncCallback(ReceiveFromClient), cl);

            lock (clients)
            {
                clients.Add(cl);
            }
        }

        private void ReceiveFromClient(IAsyncResult ar)
        {
            Client cl = (Client)ar.AsyncState;
            
            string str = Encoding.UTF8.GetString(cl.buffer);

            if (str == "quit" || !cl.socket.Connected)
            {
                // client doesn't want messages anymore :(
                lock (cl.socket)
                {
                    try
                    {
                        cl.socket.Close();
                        cl.socket = null;
                    }
                    catch (SocketException) { }
                }
            }
            else
            {
                try
                {
                    cl.socket.BeginReceive(cl.buffer, 0, cl.buffer.Length, SocketFlags.None,
                        new AsyncCallback(ReceiveFromClient), cl);
                }
                catch (SocketException) 
                {
                    cl.socket.Close();
                    cl.socket = null;
                }
            }
        }

    }
}
