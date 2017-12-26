/* Copyright (C) 2017 Yuval Deutscher

* This Source Code Form is subject to the terms of the Mozilla Public
* License, v. 2.0. If a copy of the MPL was not distributed with this
* file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using HaMusicLib;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace HaMusicServer
{
    public class ServerAsync
    {
        private List<ClientAsync> clients = new List<ClientAsync>();

        public List<ClientAsync> Clients { get => clients; }

        public async void Run()
        {
            TcpListener listener = new TcpListener(IPAddress.Any, 5151);
            listener.Start();
            while (true)
            {
                TcpClient client = await listener.AcceptTcpClientAsync();
                client.SendTimeout = 1000;
                HandleConnection(client);
            }
        }

        async void HandleConnection(TcpClient tcpClient)
        {
            ClientAsync client = new ClientAsync(tcpClient);
            clients.Add(client);
            await client.Proc();
            clients.Remove(client);
        }

        public void BroadcastMessage(HaProtoImpl.Opcode type, HaProtoImpl.HaProtoPacket packet, ClientAsync exempt = null)
        {
            byte[] data = packet.Build();
            for (int i = 0; i < clients.Count; i++)
            {
                ClientAsync c = clients[i];
                if (c == exempt)
                    continue;
                // Intentionally not awaiting to disallow races with "clients" changes, e.g. from accept
                HaProtoImpl.SendAsync(c.Stream, type, data).Wait();
            }
        }
    }
}
