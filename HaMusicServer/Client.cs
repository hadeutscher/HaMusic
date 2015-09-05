/* Copyright (C) 2015 haha01haha01

* This Source Code Form is subject to the terms of the Mozilla Public
* License, v. 2.0. If a copy of the MPL was not distributed with this
* file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using HaMusicLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace HaMusicServer
{
    public class Client
    {
        Socket s;
        Thread t;
        Action<string> log;
        MainForm mainForm;
        string id;
        Dictionary<HaProtoImpl.Opcode, byte[]> packetCache = new Dictionary<HaProtoImpl.Opcode, byte[]>();

        public Client(MainForm mainForm, Socket s, Action<string> log)
        {
            s.SendTimeout = 1000;
            id = ((IPEndPoint)s.RemoteEndPoint).Address.ToString();
            this.s = s;
            this.t = new Thread(new ThreadStart(Proc));
            this.log = log;
            this.mainForm = mainForm;
        }

        private void Proc()
        {
            try
            {
                log(string.Format("{0} Connected", id));
                while (true)
                {
                    bool announceIndexChanges = false;
                    byte[] data;
                    HaProtoImpl.Opcode type = HaProtoImpl.Receive(s, out data);
                    log(string.Format("{0}: {1}", id, type.ToString()));
                    HaProtoImpl.HaProtoPacket packet;
                    switch (type)
                    {
                        case HaProtoImpl.Opcode.GETDB:
                            lock (mainForm.DataSource.Lock)
                            {
                                HaProtoImpl.Send(s, HaProtoImpl.Opcode.SETDB, new HaProtoImpl.SETDB() { dataSource = mainForm.DataSource });
                            }
                            break;
                        case HaProtoImpl.Opcode.SETDB:
                            // Why would anyone try to set the server's DB?
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
                            packet = HaProtoImpl.ApplyPacketToDatabase(type, data, mainForm.DataSource, out announceIndexChanges);
                            mainForm.BroadcastMessage(type, packet);
                            break;
                        case HaProtoImpl.Opcode.SKIP:
                            long uid;
                            lock (mainForm.DataSource.Lock)
                            {
                                mainForm.DataSource.CurrentItem = mainForm.Mover.Next();
                                uid = mainForm.DataSource.CurrentItem == null ? -1 : mainForm.DataSource.CurrentItem.UID;
                            }
                            mainForm.BroadcastMessage(HaProtoImpl.Opcode.SETSONG, new HaProtoImpl.SETSONG() { uid = uid });
                            announceIndexChanges = true;
                            break;
                        case HaProtoImpl.Opcode.SETVOL:
                            HaProtoImpl.SETVOL setvol = HaProtoImpl.SETVOL.Parse(data);
                            lock (mainForm.DataSource.Lock)
                            {
                                mainForm.DataSource.Volume = setvol.volume;
                            }
                            mainForm.SetVolume(setvol.volume);
                            mainForm.BroadcastMessage(type, data, this);
                            break;
                        case HaProtoImpl.Opcode.SEEK:
                            HaProtoImpl.SEEK seek = HaProtoImpl.SEEK.Parse(data);
                            lock (mainForm.DataSource.Lock)
                            {
                                mainForm.DataSource.Position = seek.pos;
                                seek.max = mainForm.DataSource.Maximum;
                            }
                            mainForm.SetPosition(seek.pos);
                            mainForm.BroadcastMessage(type, seek, this);
                            break;
                        case HaProtoImpl.Opcode.SETPLAYING:
                            HaProtoImpl.SETPLAYING setplaying = HaProtoImpl.SETPLAYING.Parse(data);
                            lock (mainForm.DataSource.Lock)
                            {
                                mainForm.DataSource.Playing = setplaying.playing;
                            }
                            mainForm.SetPlaying(setplaying.playing);
                            mainForm.BroadcastMessage(type, data, this);
                            break;
                        default:
                            throw new NotSupportedException();
                    }
                    if (announceIndexChanges)
                    {
                        mainForm.AnnounceIndexChange();
                    }
                }
            }
            catch (Exception e)
            {
                log(string.Format("Exception in {0} : {1}", id, e.Message));
                try
                {
                    // Try to close the socket, if it's already closed than w/e
                    s.Close();
                }
                catch { }
                mainForm.OnThreadExit(this);
                return;
            }
        }

        public bool InCache(HaProtoImpl.Opcode opcode, byte[] data)
        {
            return packetCache.ContainsKey(opcode) && packetCache[opcode] == data;
        }

        public void SetCache(HaProtoImpl.Opcode opcode, byte[] data)
        {
            packetCache[opcode] = data;
        }

        public Thread Thread { get { return t; } }
        public Socket Socket { get { return s; } }
    }
}
