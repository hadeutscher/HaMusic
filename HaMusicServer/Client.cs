/* Copyright (C) 2015 haha01haha01

* This Source Code Form is subject to the terms of the Mozilla Public
* License, v. 2.0. If a copy of the MPL was not distributed with this
* file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using HaMusicLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace HaMusicServer
{
    public class Client
    {
        Socket s;
        Thread t;
        Action<string> log;
        MainForm mf;

        public Client(MainForm mf, Socket s, Action<string> log)
        {
            s.SendTimeout = 1000;
            this.s = s;
            this.t = new Thread(new ThreadStart(Proc));
            this.log = log;
            this.mf = mf;
        }

        private void Proc()
        {
            try
            {
                while (true)
                {
                    string data;
                    HaProtoImpl.ClientToServer type = HaProtoImpl.C2SReceive(s, out data);
                    int vol;
                    log("Got command " + type.ToString() + " + " + data);
                    bool updatePlaylist = false;
                    switch (type)
                    {
                        case HaProtoImpl.ClientToServer.ADD:
                            lock (mf.playlist)
                            {
                                if (mf.Index == mf.playlist.Count)
                                    updatePlaylist = true;
                                mf.playlist.Add(data);
                            }
                            mf.BroadcastMessage(HaProtoImpl.ServerToClient.PL_INFO, mf.GetPlaylistStr());
                            break;
                        case HaProtoImpl.ClientToServer.REMOVE:
                            lock (mf.playlist)
                            {
                                int i = int.Parse(data);
                                mf.playlist.RemoveAt(i);
                                if (mf.Index == i)
                                    mf.Index = -1;
                                else if (mf.Index > i)
                                    mf.Index--;
                            }
                            mf.BroadcastMessage(HaProtoImpl.ServerToClient.PL_INFO, mf.GetPlaylistStr());
                            break;
                        case HaProtoImpl.ClientToServer.SETIDX:
                            lock (mf.playlist)
                            {
                                mf.Index = int.Parse(data);
                            }
                            break;
                        case HaProtoImpl.ClientToServer.CLEAR:
                            lock (mf.playlist)
                            {
                                mf.playlist.Clear();
                                mf.Index = -1;
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
                                    mf.Index = i - 1;
                                else if (mf.Index == i - 1)
                                    mf.Index = i;
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
                                    mf.Index = i + 1;
                                else if (mf.Index == i + 1)
                                    mf.Index = i;
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
                            mf.BroadcastPlayPauseInfo(mf.Playing);
                            break;
                    }
                    if (updatePlaylist)
                        mf.OnPlaylistChanged();
                }
            }
            catch (Exception)
            {
                mf.OnThreadExit(this);
                return;
            }
        }

        public Thread Thread { get { return t; } }
        public Socket Socket { get { return s; } }
    }
}
