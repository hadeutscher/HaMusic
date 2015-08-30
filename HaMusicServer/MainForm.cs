/* Copyright (C) 2015 haha01haha01

* This Source Code Form is subject to the terms of the Mozilla Public
* License, v. 2.0. If a copy of the MPL was not distributed with this
* file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using HaMusicLib;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Windows.Forms;

namespace HaMusicServer
{
    public partial class MainForm : Form
    {
        private Thread listenerThread;
        private List<Client> clients = new List<Client>();
        private IHaMusicPlayer player;
        private Mover mover;
        private ServerDataSource dataSource;

        public MainForm()
        {
            HaProtoImpl.Entity = HaProtoImpl.HaMusicEntity.Server;
            InitializeComponent();
            DataSource = new ServerDataSource();
            DataSource.Playlists.Add(new Playlist());
            Mover = new Mover(DataSource);
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
            BroadcastMessage(HaProtoImpl.Opcode.SETPLAYING, new HaProtoImpl.SETPLAYING() { playing = playing });
        }

        public void BroadcastMessage(HaProtoImpl.Opcode type, byte[] data, Client exempt = null, bool caching = false)
        {
            lock (clients)
            {
                for (int i = 0; i < clients.Count; i++)
                {
                    Client c = clients[i];
                    if (c == exempt)
                        continue;
                    if (caching && c.InCache(type, data))
                        continue;
                    HaProtoImpl.Send(c.Socket, type, data);
                    if (caching)
                        c.SetCache(type, data);
                }
            }
        }

        public void BroadcastMessage(HaProtoImpl.Opcode type, HaProtoImpl.HaProtoPacket packet, Client exempt = null, bool caching = false)
        {
            BroadcastMessage(type, packet.Build(), exempt, caching);
        }

        public void BroadcastPosition()
        {
            Tuple<int, int> posInfo = player.GetPos();
            int pos = posInfo.Item1;
            int max = posInfo.Item2;
            lock (dataSource.Lock)
            {
                dataSource.Position = pos;
                dataSource.Maximum = max;
            }
            if (pos != -1)
                BroadcastMessage(HaProtoImpl.Opcode.SEEK, new HaProtoImpl.SEEK() { pos = pos, max = max }, null, true);
        }

        private void broadcastTimer_Tick(object sender, EventArgs e)
        {
            BroadcastPosition();
        }

        public void AnnounceIndexChange()
        {
            player.OnIndexChanged();
        }

        public void SetPlaying(bool p)
        {
            player.SetPlaying(p);
        }

        public void SetVolume(int vol)
        {
            player.SetVolume(vol);
        }

        public Mover Mover
        {
            get
            {
                return mover;
            }

            set
            {
                mover = value;
            }
        }

        public ServerDataSource DataSource
        {
            get
            {
                return dataSource;
            }

            set
            {
                dataSource = value;
            }
        }

        public void SetPosition(int pos)
        {
            player.Seek(pos);
        }
        
        private void MainForm_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.S && e.Control)
            {
                SaveFileDialog sfd = new SaveFileDialog() { Title = "Select save location", Filter = "Text files (*.txt)|*.txt" };
                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    File.WriteAllLines(sfd.FileName, logBox.Items.Cast<string>());
                }
            }
        }

        private void MainForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            player.Close();
        }

        public object InvokeIfRequired(Delegate method)
        {
            if (InvokeRequired)
                return Invoke(method);
            else
                return method.DynamicInvoke();
        }

        public void BeginInvokeIfRequired(Delegate method)
        {
            if (InvokeRequired)
                BeginInvoke(method);
            else
                method.DynamicInvoke();
        }
    }
}
