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
        internal List<Client> clients = new List<Client>();
        private HaMusicPlayerManager player;
        private Mover mover;
        private ServerDataSource dataSource;
        public List<IPAddress> banlist = new List<IPAddress>();
        private Action<string> log = delegate(string x) { };
        private HaShell hashell = null;
        private StreamWriter sw;

        public static string defaultLogPath = Path.Combine(Program.GetLocalSettingsFolder(), "hms.log");
        public static string defaultSourcePath = Path.Combine(Program.GetLocalSettingsFolder(), "hms.db");
        public static string defaultBanlistPath = Path.Combine(Program.GetLocalSettingsFolder(), "banlist.txt");


        public MainForm()
        {
            HaProtoImpl.Entity = HaProtoImpl.HaMusicEntity.Server;
            InitializeComponent();
            CreateLogger();
            DataSource = new ServerDataSource();
            DataSource.Playlists.Add(new Playlist());
            Mover = new Mover(this);
            listenerThread = new Thread(new ThreadStart(ListenerMain));
            player = new HaMusicPlayerManager(new NAudioPlayer(this), this, 50);
            player.SongChanged += player_SongChanged;
            player.PlayingChanged += player_PlayingChanged;
            RestoreState();
            hashell = new HaShell(this, console);
        }

        private void CreateLogger()
        {
            Stream fs = Stream.Synchronized(File.Open(defaultLogPath, FileMode.OpenOrCreate, FileAccess.Write, FileShare.Read));
            fs.Seek(0, SeekOrigin.End);
            sw = new StreamWriter(fs);
            log = delegate (string x)
            {
                lock (sw)
                {
                    sw.Write(x + "\r\n");
                }
            };
        }

        public void FlushLog()
        {
            lock (sw)
            {
                sw.Flush();
            }
        }

        private void RestoreState()
        {
            if (File.Exists(defaultBanlistPath))
            {
                foreach (string ip in File.ReadAllLines(defaultBanlistPath))
                {
                    IPAddress addr = null;
                    if (IPAddress.TryParse(ip, out addr))
                    {
                        banlist.Add(addr);
                    }
                }
            }
        }

        public void LoadSourceState(string path)
        {
            int pos, vol;
            lock (DataSource.Lock)
            {
                using (FileStream fs = File.OpenRead(path))
                {
                    Playlist.DeserializeCounters(fs);
                    PlaylistItem.DeserializeCounters(fs);
                    DataSource = ServerDataSource.Deserialize(fs);
                }
                pos = DataSource.Position;
                vol = DataSource.Volume;
                BroadcastMessage(HaProtoImpl.Opcode.SETDB, new HaProtoImpl.SETDB() { dataSource = DataSource });
            }
            mover.OnSetDataSource();
            player.OnSongChanged();
            player.Position = pos;
            player.Volume = vol;
        }

        public void SaveSourceState(string path)
        {
            lock (DataSource.Lock)
            {
                using (FileStream fs = File.Create(path))
                {
                    Playlist.SerializeCounters(fs);
                    PlaylistItem.SerializeCounters(fs);
                    DataSource.Serialize(fs);
                }
            }
        }

        public void FlushBanlist()
        {
            try
            {
                lock (banlist)
                {
                    File.WriteAllLines(defaultBanlistPath, banlist.Select(x => x.ToString()));
                }
            }
            catch (Exception e)
            {
                log("Banlist flush failed: " + e.Message);
            }
        }

        void player_SongChanged(object sender, PlaylistItem item)
        {
            BroadcastMessage(HaProtoImpl.Opcode.SETSONG, new HaProtoImpl.SETSONG() { uid = item == null ? -1 : item.UID });
        }

        void player_PlayingChanged(object sender, bool playing)
        {
            lock (dataSource)
            {
                if (dataSource.Playing != playing)
                {
                    dataSource.Playing = playing;
                    BroadcastMessage(HaProtoImpl.Opcode.SETPLAYING, new HaProtoImpl.SETPLAYING() { playing = playing });
                }
            }
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
            while (true)
            {
                Socket newSock = listener.Accept();
                lock (banlist)
                {
                    if (banlist.Any(x => x.Equals(((IPEndPoint)newSock.RemoteEndPoint).Address)))
                    {
                        newSock.Close();
                        continue;
                    }
                }
                Client c = new Client(this, newSock, log);
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

        public void BroadcastMessage(HaProtoImpl.Opcode type, HaProtoImpl.HaProtoPacket packet, Client exempt = null)
        {
            byte[] data = packet.Build();
            lock (clients)
            {
                for (int i = 0; i < clients.Count; i++)
                {
                    Client c = clients[i];
                    if (c == exempt)
                        continue;
                    HaProtoImpl.Send(c.Socket, type, data);
                }
            }
        }

        public void BroadcastPosition()
        {
            int pos = player.Position;
            int max = player.Maximum;
            lock (dataSource.Lock)
            {
                if (dataSource.Position == pos && dataSource.Maximum == max)
                    return;
                dataSource.Position = pos;
                dataSource.Maximum = max;
            }
            BroadcastMessage(HaProtoImpl.Opcode.SEEK, new HaProtoImpl.SEEK() { pos = pos, max = max });
        }

        private void broadcastTimer_Tick(object sender, EventArgs e)
        {
            BroadcastPosition();
        }

        public void AnnounceIndexChange()
        {
            player.OnSongChanged();
        }
        
        public void SetPlaying(bool p)
        {
            player.Playing = p;
        }

        public void SetVolume(int vol)
        {
            player.Volume = vol;
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
            player.Position = pos;
        }
        
        private void MainForm_KeyDown(object sender, KeyEventArgs e)
        {
        }

        private void MainForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            player.Dispose();
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

        private void saveDbTimer_Tick(object sender, EventArgs e)
        {
            new Thread(new ThreadStart(delegate ()
            {
                try
                {
                    SaveSourceState(defaultSourcePath);
                }
                catch (Exception ex)
                {
                    log("DataSource flush failed: " + ex.Message);
                }
            })).Start();
        }
    }
}
