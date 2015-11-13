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
using System.Text;
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
        public List<string> libraryPaths = new List<string>();
        private List<FileSystemWatcher> watchers = new List<FileSystemWatcher>();
        public List<string> extensionWhitelist = new List<string>();
        public Action<string> log = delegate(string x) { };
        private HaShell hashell = null;
        private StreamWriter sw;
        private object libraryLoaderLock = new object();
        private bool indexerFinished = false;

        public static string defaultLogPath = Path.Combine(Program.GetLocalSettingsFolder(), "hms.log");
        public static string defaultSourcePath = Path.Combine(Program.GetLocalSettingsFolder(), "hms.db");
        public static string defaultConfigPath = Path.Combine(Program.GetLocalSettingsFolder(), "config.txt");

        private const string BANLIST_KEY = "banlist";
        private const string LIBRARIES_KEY = "libraries";
        private const string EXTENSIONS_KEY = "extensions";


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
            BeginReloadLibrary();
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

        private Dictionary<string, List<string>> ReadConfig()
        {
            Dictionary<string, List<string>> result = new Dictionary<string, List<string>>();
            if (File.Exists(defaultConfigPath))
            {
                foreach (string conf in File.ReadAllLines(defaultConfigPath))
                {
                    int splitterIndex = conf.IndexOf('=');
                    if (splitterIndex < 0)
                        continue;
                    string key = conf.Substring(0, splitterIndex).Trim().ToLower();
                    string value = conf.Substring(splitterIndex + 1, conf.Length - splitterIndex - 1).Trim();
                    result[key] = string.IsNullOrWhiteSpace(value) ? new List<string>() : value.Split(',').Select(x => x.Trim()).ToList();
                }
            }
            return result;
        }

        private void RestoreState(Action<string> logger = null)
        {
            if (logger == null)
                logger = log;
            try {
                Dictionary<string, List<string>> conf = ReadConfig();
                List<string> workingSet;
                if (conf.TryGetValue(BANLIST_KEY, out workingSet))
                {
                    lock (banlist)
                    {
                        foreach (string ip in workingSet)
                        {
                            IPAddress addr = null;
                            if (IPAddress.TryParse(ip, out addr))
                            {
                                banlist.Add(addr);
                            }
                        }
                    }
                }

                if (conf.TryGetValue(LIBRARIES_KEY, out workingSet))
                {
                    lock (libraryPaths)
                    {
                        foreach (string library in workingSet)
                        {
                            libraryPaths.Add(library);
                        }
                        OnLibraryPathsChanged();
                    }
                }

                if (conf.TryGetValue(EXTENSIONS_KEY, out workingSet))
                {
                    lock(extensionWhitelist)
                    {
                        foreach (string ext in workingSet)
                        {
                            extensionWhitelist.Add(ext);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                logger("Config read failed: " + e.Message);
            }
        }

        public void OnLibraryPathsChanged()
        {
            lock (libraryPaths)
            {
                foreach (FileSystemWatcher fsw in watchers)
                {
                    fsw.Dispose();
                }
                watchers.Clear();
                foreach (string path in libraryPaths)
                {
                    FileSystemWatcher fsw = new FileSystemWatcher(path) { NotifyFilter = NotifyFilters.FileName, IncludeSubdirectories = true };
                    fsw.Created += Fsw_Created;
                    fsw.Deleted += Fsw_Deleted;
                    fsw.Renamed += Fsw_Renamed;
                    watchers.Add(fsw);
                    fsw.EnableRaisingEvents = true;
                }
            }
        }

        private void Fsw_Created(object sender, FileSystemEventArgs e)
        {
            lock (extensionWhitelist)
            {
                if (!extensionWhitelist.Contains(Path.GetExtension(e.FullPath).ToLower()))
                    return;
            }
            HaProtoImpl.LIBRARY_ADD packet = new HaProtoImpl.LIBRARY_ADD() { paths = new List<string> { e.FullPath } };
            ExecutePacketAndBroadcast(HaProtoImpl.Opcode.LIBRARY_ADD, packet);
        }

        private void Fsw_Deleted(object sender, FileSystemEventArgs e)
        {
            lock (extensionWhitelist)
            {
                if (!extensionWhitelist.Contains(Path.GetExtension(e.FullPath).ToLower()))
                    return;
            }
            HaProtoImpl.LIBRARY_REMOVE packet = new HaProtoImpl.LIBRARY_REMOVE() { paths = new List<string> { e.FullPath } };
            ExecutePacketAndBroadcast(HaProtoImpl.Opcode.LIBRARY_REMOVE, packet);
        }

        private void Fsw_Renamed(object sender, RenamedEventArgs e)
        {
            lock (extensionWhitelist)
            {
                if (!extensionWhitelist.Contains(Path.GetExtension(e.FullPath).ToLower()))
                    return;
            }
            List<HaProtoImpl.HaProtoPacket> packets = new List<HaProtoImpl.HaProtoPacket> {
                new HaProtoImpl.LIBRARY_REMOVE() { paths = new List<string> { e.OldFullPath } },
                new HaProtoImpl.LIBRARY_ADD() { paths = new List<string> { e.FullPath } }
            };
            List<HaProtoImpl.Opcode> ops = new List<HaProtoImpl.Opcode> { HaProtoImpl.Opcode.LIBRARY_REMOVE, HaProtoImpl.Opcode.LIBRARY_ADD };
            ExecutePacketsAndBroadcast(ops, packets);
        }

        public void LoadSourceState(string path)
        {
            int pos, vol;
            bool playing;
            lock (DataSource.Lock)
            {
                Playlist library = null;
                if (indexerFinished)
                {
                    library = DataSource.LibraryPlaylist;
                }
                ServerDataSource newSource;
                using (FileStream fs = File.OpenRead(path))
                {
                    Playlist.DeserializeCounters(fs);
                    PlaylistItem.DeserializeCounters(fs);
                    newSource = ServerDataSource.Deserialize(fs);
                }
                lock (newSource.Lock)
                {
                    // Make sure that Current and Next still exist; they might not if the DB was saved when playing from the Library
                    PlaylistItem foo;
                    if (newSource.CurrentItem != null && !newSource.TryGetItem(newSource.CurrentItem.UID, out foo))
                        newSource.CurrentItem = null;
                    if (newSource.NextItemOverride != null && !newSource.TryGetItem(newSource.NextItemOverride.UID, out foo))
                        newSource.NextItemOverride = null;

                    if (indexerFinished)
                    {
                        newSource.LibraryPlaylist = library;
                    }
                    DataSource = newSource;
                    pos = DataSource.Position;
                    vol = DataSource.Volume;
                    playing = DataSource.Playing;
                    BroadcastMessage(HaProtoImpl.Opcode.SETDB, new HaProtoImpl.SETDB() { dataSource = DataSource });
                }
            }
            mover.OnSetDataSource();
            player.OnSongChanged();
            player.Playing = playing;
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

        private void WriteConfigInternal(Dictionary<string, List<string>> conf)
        {
            File.WriteAllLines(defaultConfigPath, conf.Keys.Select(x => x + "=" + (conf[x].Count > 0 ? conf[x].Aggregate((a, b) => a + "," + b) : "")));
        }

        public void WriteConfig(Action<string> logger=null)
        {
            if (logger == null)
                logger = log;
            try
            {
                Dictionary<string, List<string>> conf = new Dictionary<string, List<string>>();
                lock (banlist)
                {
                    conf[BANLIST_KEY] = banlist.Select(x => x.ToString()).ToList();
                }
                lock (libraryPaths)
                {
                    conf[LIBRARIES_KEY] = libraryPaths;
                }
                lock (extensionWhitelist)
                {
                    conf[EXTENSIONS_KEY] = extensionWhitelist;
                }
                WriteConfigInternal(conf);
            }
            catch (Exception e)
            {
                logger("Config flush failed: " + e.Message);
            }
        }

        public void BeginReloadLibrary(Action<string> logger = null)
        {
            if (logger == null)
                logger = log;
            Thread libraryLoader = new Thread(new ThreadStart(() => ReloadLibrary(logger)));
            libraryLoader.Start();
        }

        private void ReloadLibrary(Action<string> logger = null)
        {
            if (logger == null)
                logger = log;
            HaProtoImpl.LIBRARY_RESET result = null;
            if (Monitor.TryEnter(libraryLoaderLock))
            {
                try
                {
                    List<string> paths = new List<string>(), exts = new List<string>();
                    lock (libraryPaths)
                    {
                        paths = libraryPaths.ToList();
                    }
                    lock (extensionWhitelist)
                    {
                        exts = extensionWhitelist.ToList();
                    }
                    List<string> index = Reindex(paths, exts, logger);
                    if (index == null)
                    {
                        return;
                    }
                    lock (dataSource.Lock)
                    {
                        result = new HaProtoImpl.LIBRARY_RESET() { paths = index };
                        indexerFinished = true;
                    }
                }
                finally
                {
                    Monitor.Exit(libraryLoaderLock);
                }
            }
            else
            {
                logger("ReloadLibrary: Someone else already reloading, skipping");
            }
            if (result != null)
            {
                ExecutePacketAndBroadcast(HaProtoImpl.Opcode.LIBRARY_RESET, result);
            }
        }

        private void ExecutePacketAndBroadcast(HaProtoImpl.Opcode op, HaProtoImpl.HaProtoPacket packet)
        {
            bool announceIndexChange;
            lock (dataSource.Lock)
            {
                announceIndexChange = packet.ApplyToDatabase(dataSource);
            }
            if (announceIndexChange)
            {
                AnnounceIndexChange();
            }
            BroadcastMessage(op, packet);
        }

        private void ExecutePacketsAndBroadcast(List<HaProtoImpl.Opcode> ops, List<HaProtoImpl.HaProtoPacket> packets)
        {
            bool announceIndexChange = false;
            lock (dataSource.Lock)
            {
                foreach (HaProtoImpl.HaProtoPacket packet in packets)
                {
                    announceIndexChange |= packet.ApplyToDatabase(dataSource);
                }
            }
            if (announceIndexChange)
            {
                AnnounceIndexChange();
            }
            for (int i = 0; i < packets.Count; i++)
            {
                BroadcastMessage(ops[i], packets[i]);
            }
        }

        private List<string> Reindex(List<string> sources, List<string> exts, Action<string> logger = null)
        {
            if (logger == null)
                logger = log;
            Exception error = null;
            List<string> result = new List<string>();
            try
            {
                foreach (string source in sources)
                {
                    IndexRecursive(result, new DirectoryInfo(source), exts);
                }
            }
            catch (Exception e)
            {
                error = e;
            }
            if (error != null)
            {
                logger("Reindex: " + error.Message);
                return null;
            }
            else
            {
                return result;
            }
        }

        private void IndexRecursive(List<string> result, DirectoryInfo dir, List<string> exts)
        {
            foreach (DirectoryInfo subdir in dir.EnumerateDirectories().OrderBy(x => x.Name))
            {
                IndexRecursive(result, subdir, exts);
            }
            foreach (FileInfo file in dir.EnumerateFiles().OrderBy(x => x.Name))
            {
                if (exts.Contains(file.Extension.ToLower()))
                    result.Add(file.FullName);
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
            FlushLog();
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
