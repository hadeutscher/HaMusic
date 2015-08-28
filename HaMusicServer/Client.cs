/* Copyright (C) 2015 haha01haha01

* This Source Code Form is subject to the terms of the Mozilla Public
* License, v. 2.0. If a copy of the MPL was not distributed with this
* file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using HaMusicLib;
using System;
using System.Collections.Generic;
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
        MainForm mf;
        string id;
        Dictionary<HaProtoImpl.ServerToClient, string> packetCache = new Dictionary<HaProtoImpl.ServerToClient, string>();

        public Client(MainForm mf, Socket s, Action<string> log)
        {
            s.SendTimeout = 1000;
            id = ((IPEndPoint)s.RemoteEndPoint).Address.ToString();
            this.s = s;
            this.t = new Thread(new ThreadStart(Proc));
            this.log = log;
            this.mf = mf;
        }

        private void Proc()
        {
            try
            {
                log(string.Format("{0} Connected", id));
                while (true)
                {
                    string data;
                    HaProtoImpl.ClientToServer type = HaProtoImpl.C2SReceive(s, out data);
                    int vol;
                    log(string.Format("{0}: {1} + {2}", id, type.ToString(), data));
                    switch (type)
                    {
                        case HaProtoImpl.ClientToServer.ADD:
                            lock (mf.playlist)
                            {
                                mf.playlist.Add(data);
                                if (mf.Index == mf.playlist.Count - 1)
                                {
                                    mf.OnPlaylistChanged();
                                }
                            }
                            mf.BroadcastMessage(HaProtoImpl.ServerToClient.PL_INFO, mf.GetPlaylistStr());
                            break;
                        case HaProtoImpl.ClientToServer.REMOVE:
                            lock (mf.playlist)
                            {
                                int i = int.Parse(data);
                                mf.playlist.RemoveAt(i);
                                if (mf.Index == i)
                                    mf.SetIndex(-1);
                                else if (mf.Index > i)
                                    mf.SetIndex(mf.Index - 1);
                            }
                            mf.BroadcastMessage(HaProtoImpl.ServerToClient.PL_INFO, mf.GetPlaylistStr());
                            break;
                        case HaProtoImpl.ClientToServer.SETIDX:
                            lock (mf.playlist)
                            {
                                mf.SetIndex(int.Parse(data), true);
                            }
                            break;
                        case HaProtoImpl.ClientToServer.CLEAR:
                            lock (mf.playlist)
                            {
                                mf.playlist.Clear();
                                mf.SetIndex(-1);
                            }
                            mf.BroadcastMessage(HaProtoImpl.ServerToClient.PL_INFO, mf.GetPlaylistStr());
                            break;
                        case HaProtoImpl.ClientToServer.GETPL:
                            HaProtoImpl.S2CSend(s, mf.GetPlaylistStr(), HaProtoImpl.ServerToClient.PL_INFO);
                            break;
                        case HaProtoImpl.ClientToServer.GETIDX:
                            HaProtoImpl.S2CSend(s, mf.Index.ToString(), HaProtoImpl.ServerToClient.IDX_INFO);
                            break;
                        case HaProtoImpl.ClientToServer.GETVOL:
                            HaProtoImpl.S2CSend(s, mf.Volume.ToString(), HaProtoImpl.ServerToClient.VOL_INFO);
                            break;
                        case HaProtoImpl.ClientToServer.SETVOL:
                            vol = int.Parse(data);
                            mf.Volume = vol;
                            mf.BroadcastMessage(HaProtoImpl.ServerToClient.VOL_INFO, vol.ToString(), this);
                            break;
                        case HaProtoImpl.ClientToServer.UP:
                            lock (mf.playlist)
                            {
                                int i = int.Parse(data);
                                if (i < 0 || i >= mf.playlist.Count || i == 0)
                                    break;
                                string x = mf.playlist[i];
                                mf.playlist.RemoveAt(i);
                                mf.playlist.Insert(i - 1, x);
                                if (mf.Index == i)
                                    mf.SetIndex(i - 1);
                                else if (mf.Index == i - 1)
                                    mf.SetIndex(i);
                            }
                            mf.BroadcastMessage(HaProtoImpl.ServerToClient.PL_INFO, mf.GetPlaylistStr());
                            break;
                        case HaProtoImpl.ClientToServer.DOWN:
                            lock (mf.playlist)
                            {
                                int i = int.Parse(data);
                                if (i < 0 || i >= mf.playlist.Count || i == mf.playlist.Count - 1)
                                    break;
                                string x = mf.playlist[i];
                                mf.playlist.RemoveAt(i);
                                mf.playlist.Insert(i + 1, x);
                                if (mf.Index == i)
                                    mf.SetIndex(i + 1);
                                else if (mf.Index == i + 1)
                                    mf.SetIndex(i);
                            }
                            mf.BroadcastMessage(HaProtoImpl.ServerToClient.PL_INFO, mf.GetPlaylistStr());
                            break;
                        case HaProtoImpl.ClientToServer.SEEK:
                            int pos = int.Parse(data);
                            mf.Position = pos;
                            mf.BroadcastPosition(this);
                            break;
                        case HaProtoImpl.ClientToServer.GETPOS:
                            mf.SendPositionToClient(this);
                            break;
                        case HaProtoImpl.ClientToServer.PAUSE:
                            mf.Playing = false;
                            mf.BroadcastPlayPauseInfo(mf.Playing);
                            break;
                        case HaProtoImpl.ClientToServer.PLAY:
                            mf.Playing = true;
                            mf.BroadcastPlayPauseInfo(mf.Playing);
                            break;
                        case HaProtoImpl.ClientToServer.GETSTATE:
                            HaProtoImpl.S2CSend(s, mf.Playing ? "1" : "0", HaProtoImpl.ServerToClient.PLAY_PAUSE_INFO);
                            break;
                        case HaProtoImpl.ClientToServer.GETMOVE:
                            HaProtoImpl.S2CSend(s, ((int)mf.mover.Mode).ToString(), HaProtoImpl.ServerToClient.MOVE_INFO);
                            break;
                        case HaProtoImpl.ClientToServer.SETMOVE:
                            mf.mover.Mode = (HaProtoImpl.MoveType)int.Parse(data);
                            mf.BroadcastMessage(HaProtoImpl.ServerToClient.MOVE_INFO, data);
                            break;
                        case HaProtoImpl.ClientToServer.SKIP:
                            lock (mf.playlist)
                            {
                                mf.SetIndex(mf.mover.AdvanceIndex(), true);
                            }
                            break;
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
                mf.OnThreadExit(this);
                return;
            }
        }

        public bool InCache(HaProtoImpl.ServerToClient opcode, string data)
        {
            return packetCache.ContainsKey(opcode) && packetCache[opcode] == data;
        }

        public void SetCache(HaProtoImpl.ServerToClient opcode, string data)
        {
            packetCache[opcode] = data;
        }

        public Thread Thread { get { return t; } }
        public Socket Socket { get { return s; } }
    }
}
