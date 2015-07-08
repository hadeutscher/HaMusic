/* Copyright (C) 2015 haha01haha01

* This Source Code Form is subject to the terms of the Mozilla Public
* License, v. 2.0. If a copy of the MPL was not distributed with this
* file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using HaMusicLib;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace HaMusicServer
{
    public partial class MainForm : Form
    {
        private Thread listenerThread;
        private int index = 0;
        private List<Client> clients = new List<Client>();
        private IHaMusicPlayer player;
        public List<string> playlist = new List<string>();
        public bool manualStop = false;

        public MainForm()
        {
            InitializeComponent();
            listenerThread = new Thread(new ThreadStart(ListenerMain));
            player = new NAudioPlayer(this, 50);
            player.PausePlayChanged += player_PausePlayChanged;
        }

        void player_PausePlayChanged(object sender, bool playing)
        {
            BroadcastPlayPauseInfo(playing);
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            listenerThread.Start();
        }

        private void ListenerMain()
        {
            Invoke((Action)delegate { broadcastTimer.Start(); });
            Socket listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            listener.Bind(new IPEndPoint(new IPAddress(new byte[] { 0, 0, 0, 0 }), 5151));
            listener.Listen(10);
            Action<string> log = delegate(string x)
            {
                Invoke((Action)delegate
                {
                    logBox.Items.Add(x + "\r\n");
                    logBox.SelectedIndex = logBox.Items.Count - 1;
                });
            };
            while (true)
            {
                Client c = new Client(this, listener.Accept(), log);
                lock (clients)
                {
                    clients.Add(c);
                }
                c.Thread.Start();
            }
        }

        public void OnThreadExit(Client c)
        {
            lock (clients)
            {
                if (clients.Contains(c))
                {
                    clients.Remove(c);
                }
            }
        }

        public void BroadcastPlayPauseInfo(bool playing)
        {
            BroadcastMessage(HaProtoImpl.ServerToClient.PLAY_PAUSE_INFO, playing ? "1" : "0");
        }

        public void BroadcastMessage(HaProtoImpl.ServerToClient type, string data, Client exempt = null)
        {
            lock (clients)
            {
                for (int i = 0; i < clients.Count; i++)
                {
                    if (clients[i] == exempt)
                        continue;
                    HaProtoImpl.S2CSend(clients[i].Socket, data, type);
                }
            }
        }

        public string GetPlaylistStr()
        {
            lock (playlist)
            {
                string packet = "";
                foreach (string file in playlist)
                {
                    packet += file + "\r\n";
                }
                return packet;
            }
        }

        public int Index
        {
            get
            {
                return index;
            }
            set
            {
                if (index == value)
                    return;
                index = value;
                player.OnIndexChanged();
                BroadcastMessage(HaProtoImpl.ServerToClient.IDX_INFO, index.ToString());
            }
        }

        public void SetIndexInternal(int index)
        {
            this.index = index;
        }

        public void OnPlaylistChanged()
        {
            player.OnIndexChanged();
        }

        public bool Playing
        {
            get
            {
                return player.IsPlaying();
            }
            set
            {
                player.SetPlaying(value);
            }
        }

        public int Volume
        {
            get
            {
                return player.GetVolume();
            }
            set
            {
                player.SetVolume(value);
            }
        }

        public int Position
        {
            get
            {
                return player.GetPos().Item1;
            }
            set
            {
                player.Seek(value);
            }
        }

        public void BroadcastPosition(Client exempt = null)
        {
            Tuple<int, int> posInfo = player.GetPos();
            int pos = posInfo.Item1;
            int max = posInfo.Item2;
            if (pos != -1)
                BroadcastMessage(HaProtoImpl.ServerToClient.MEDIA_SEEK_INFO, pos.ToString() + "," + max.ToString(), exempt);
        }

        public void SendPositionToClient(Client c)
        {
            Tuple<int, int> posInfo = player.GetPos();
            int pos = posInfo.Item1;
            int max = posInfo.Item2;
            if (pos != -1)
                HaProtoImpl.S2CSend(c.Socket, pos.ToString() + "," + max.ToString(), HaProtoImpl.ServerToClient.MEDIA_SEEK_INFO);
        }

        private void broadcastTimer_Tick(object sender, EventArgs e)
        {
            BroadcastPosition();
        }
    }
}
