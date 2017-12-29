/* Copyright (C) 2017 Yuval Deutscher

* This Source Code Form is subject to the terms of the Mozilla Public
* License, v. 2.0. If a copy of the MPL was not distributed with this
* file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using HaMusicLib;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace HaMusicServer
{
    public class ClientAsync
    {
        private TcpClient client;
        private NetworkStream stream;
        private IPAddress ip;
        private string id;

        public ClientAsync(TcpClient client)
        {
            this.client = client;
            stream = client.GetStream();
            ip = ((IPEndPoint)client.Client.RemoteEndPoint).Address;
            id = ip.ToString();
        }

        public string ID { get => id; }
        public IPAddress IP { get => ip; }
        public NetworkStream Stream { get => stream; }

        public void Send(HaProtoImpl.Opcode type, HaProtoImpl.HaProtoPacket packet)
        {
            Send(type, packet.Build());
        }

        public async void Send(HaProtoImpl.Opcode type, byte[] data)
        {
            try
            {
                await HaProtoImpl.SendAsync(stream, type, data);
            }
            catch (Exception e)
            {
                if (e is SocketException && ((SocketException)e).ErrorCode == 0)
                    Program.Logger.Log(string.Format("{0} exited normally", id));
                else
                    Program.Logger.Log(string.Format("Exception in {0} : {1}", id, Utils.GetErrorException(e)));
                Kill();
            }
        }

        public void Kill()
        {
            try
            {
                // Try to close the socket, if it's already closed then w/e
                client.Close();
            }
            catch { }
        }

        public async Task Proc()
        {
            try
            {
                Program.Logger.Log(string.Format("{0} Connected", id));
                while (true)
                {
                    bool announceIndexChanges = false;
                    var result = await HaProtoImpl.ReceiveAsync(stream);
                    HaProtoImpl.Opcode type = result.Item1;
                    byte[] data = result.Item2;
                    Program.Logger.Log(string.Format("{0}: {1}", id, type.ToString()));
                    HaProtoImpl.HaProtoPacket packet;
                    switch (type)
                    {
                        case HaProtoImpl.Opcode.GETDB:
                            // Intentionally waiting synchronously to prevent multiple-sends, e.g. by BroadcastMessage sending to this client
                            Send(HaProtoImpl.Opcode.SETDB, new HaProtoImpl.SETDB() { dataSource = Program.Core.DataSource });
                            break;
                        case HaProtoImpl.Opcode.SETDB:
                        case HaProtoImpl.Opcode.LIBRARY_ADD:
                        case HaProtoImpl.Opcode.LIBRARY_REMOVE:
                        case HaProtoImpl.Opcode.LIBRARY_RESET:
                            throw new NotSupportedException();
                        case HaProtoImpl.Opcode.ADD:
                        case HaProtoImpl.Opcode.REMOVE:
                        case HaProtoImpl.Opcode.CLEAR:
                        case HaProtoImpl.Opcode.SETSONG:
                        case HaProtoImpl.Opcode.ADDPL:
                        case HaProtoImpl.Opcode.DELPL:
                        case HaProtoImpl.Opcode.RENPL:
                        case HaProtoImpl.Opcode.SETMOVE:
                        case HaProtoImpl.Opcode.REORDER:
                        case HaProtoImpl.Opcode.INJECT:
                            packet = HaProtoImpl.ApplyPacketToDatabase(type, data, Program.Core.DataSource, out announceIndexChanges);
                            Program.Server.BroadcastMessage(type, packet);
                            break;
                        case HaProtoImpl.Opcode.SKIP:
                            long uid;
                            Program.Core.DataSource.CurrentItem = Program.Mover.Next();
                            uid = Program.Core.DataSource.CurrentItem == null ? -1 : Program.Core.DataSource.CurrentItem.UID;
                            Program.Server.BroadcastMessage(HaProtoImpl.Opcode.SETSONG, new HaProtoImpl.SETSONG() { uid = uid });
                            announceIndexChanges = true;
                            break;
                        case HaProtoImpl.Opcode.SETVOL:
                            HaProtoImpl.SETVOL setvol = HaProtoImpl.SETVOL.Parse(data);
                            Program.Core.DataSource.Volume = setvol.volume;
                            Program.Core.SetVolume(setvol.volume);
                            Program.Server.BroadcastMessage(type, setvol, this);
                            break;
                        case HaProtoImpl.Opcode.SEEK:
                            HaProtoImpl.SEEK seek = HaProtoImpl.SEEK.Parse(data);
                            Program.Core.DataSource.Position = seek.pos;
                            seek.max = Program.Core.DataSource.Maximum;
                            Program.Core.SetPosition(seek.pos);
                            Program.Server.BroadcastMessage(type, seek, this);
                            break;
                        case HaProtoImpl.Opcode.SETPLAYING:
                            HaProtoImpl.SETPLAYING setplaying = HaProtoImpl.SETPLAYING.Parse(data);
                            Program.Core.DataSource.Playing = setplaying.playing;
                            Program.Core.SetPlaying(setplaying.playing);
                            Program.Server.BroadcastMessage(type, setplaying);
                            break;
                        default:
                            throw new NotSupportedException();
                    }
                    if (announceIndexChanges)
                    {
                        Program.Core.AnnounceIndexChange();
                    }
                }
            }
            catch (Exception e)
            {
                if (e is SocketException && ((SocketException)e).ErrorCode == 0)
                    Program.Logger.Log(string.Format("{0} exited normally", id));
                else
                    Program.Logger.Log(string.Format("Exception in {0} : {1}", id, Utils.GetErrorException(e)));
                Kill();
            }
        }
    }
}
