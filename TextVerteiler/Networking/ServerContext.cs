﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Net;
using System.Net.Sockets;

namespace TextVerteiler.Networking
{
    public class ServerContext
    {

        public TcpListener listener { get; private set; }


        public IPAddress Ip { get; set; }


        public int Port { get; set; }


        public List<ClientContext> Clients { get; set; }

        public int MaxClients { get; set; }

        private List<string> TextStack = new List<string>();


        AsyncCallback BeginAcceptSocketCallback;

        

        //server stuff


        UdpClient UdpServer;


        public static string Broadscast = "224.0.0.255"; // 224.0.0.255-239.255.255.255 port 2222

        public static int BroadcastPort = 2222; //egal... da nur theoretischer (multicast)client


        public static IPEndPoint BroadcastServer = new IPEndPoint(IPAddress.Parse(Broadscast), BroadcastPort);

        byte[] ServerMessage;

        AsyncCallback BeginSendUdpServerCallback;

        public ServerContext(int port, int maxclients, ref List<string> TextStack, string OwnIP)
        {

            BeginSendUdpServerCallback = new AsyncCallback(OnBeginSendUdpServerCallbackFinished);
            ServerMessage = System.Text.Encoding.ASCII.GetBytes("OK:" + OwnIP);

            UdpServer = new UdpClient();
           
            UdpServer.JoinMulticastGroup(BroadcastServer.Address, 1);

            UdpServer.BeginSend(ServerMessage, ServerMessage.Length,BroadcastServer, BeginSendUdpServerCallback, null);

            MaxClients = maxclients;

            Ip = IPAddress.Any;
            this.Port = port;

            listener = new TcpListener(Ip, Port);
            Clients = new List<ClientContext>(MaxClients);

            listener.Start();



            BeginAcceptSocketCallback = new AsyncCallback(OnClientConnected);

            this.TextStack = TextStack;
        }

        private void OnBeginSendUdpServerCallbackFinished(IAsyncResult result)
        {
            UdpServer.EndSend(result);

            //next multicast package in 3 sec
            System.Threading.Thread.Sleep(FormMain.UDPBROADCAST_Delay);
            UdpServer.BeginSend(ServerMessage, ServerMessage.Length,BroadcastServer, BeginSendUdpServerCallback, null);
        }

        public void DoListen()
        {

            Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.IP);

            listener.BeginAcceptSocket(BeginAcceptSocketCallback, null);

        }

        public void OnClientConnected(IAsyncResult _ClientSocket)
        {

            if (this.Clients.Count <= MaxClients - 1)
            {

                Socket clientsocket = listener.EndAcceptSocket(_ClientSocket);

                ClientContext clientcontext = new ClientContext(clientsocket);

                this.Clients.Add(clientcontext);

                //listen for next
                DoListen();

            }
            else
            {
                //keine mehr annehmen
            }

        }

        public void DisconnectAllClients()
        {
            foreach (var client in this.Clients)
            {
                if (client.socket.Connected)
                {
                    client.socket.Shutdown(SocketShutdown.Both);
                    client.socket.Close();
                }
            }
        }

        public int GetRealClientsCount()
        {
            int count = Clients.Count;
            int clientsCount = 0;

            for (int i = 0; i < count; i++)
            {
                if (this.Clients[i].isConnected())
                {
                    clientsCount++;
                }
                else
                {
                    //delete
                    this.Clients[i].Close();
                    this.Clients.RemoveAt(i);
                    i--;
                    count = this.Clients.Count;
                }
            }

            return clientsCount;

        }

        public void SendText(string Text)
        {
            if (Clients.Count > 0)
            {

                byte[] message = Text.ToByteArray();


                foreach (var client in this.Clients)
                {
                    if (client.socket.IsBound)
                    {
                        client.Send(message);
                    }
                }
            }

            if (this.TextStack.Count() < FormMain.HistoryStackSize)
            {
                this.TextStack.Add(Text);
            }
            else if (this.TextStack.Count() >= FormMain.HistoryStackSize)
            {
                //am anfang löschen bis passt
                while (this.TextStack.Count() >= FormMain.HistoryStackSize)
                {
                    this.TextStack.RemoveAt(0);
                }

                this.TextStack.Add(Text);
            }



        }




    }
}