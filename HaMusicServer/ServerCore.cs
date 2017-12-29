/* Copyright (C) 2017 Yuval Deutscher

* This Source Code Form is subject to the terms of the Mozilla Public
* License, v. 2.0. If a copy of the MPL was not distributed with this
* file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using HaMusicLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;

namespace HaMusicServer
{
    public class ServerCore
    {
        private static readonly Dictionary<PlatformID, string> defaultPlayer = new Dictionary<PlatformID, string> {
            { PlatformID.Win32Windows, "naudio" },
            { PlatformID.Win32NT, "naudio" },
            { PlatformID.Unix, "mplayer" },
        };

        public MediaPlayer Player;

        private ServerDataSource dataSource = new ServerDataSource();
        private List<IPAddress> banlist = new List<IPAddress>();
        private List<string> libraryPaths = new List<string>();
        private List<FileSystemWatcher> watchers = new List<FileSystemWatcher>();
        private List<string> extensionWhitelist = new List<string>();
        private string playerName = null;
        private TimerAsync positionUpdater = new TimerAsync(() => Utils.BroadcastPosition(), 100);
        private TimerAsync databaseSaver = new TimerAsync(() => Utils.SaveSourceStateSafe(), 60000);
        private bool indexerFinished = false;

        public ServerCore()
        {
            DataSource.Playlists.Add(new Playlist());
            ReadConfig();
            LoadPlayer();
            Player.SongChanged += player_SongChanged;
            Player.PlayingChanged += player_PlayingChanged;
        }

        public void Run()
        {
            BeginReloadLibrary();
            positionUpdater.Run();
            databaseSaver.Run();
        }

        private static Dictionary<string, List<string>> ReadConfigInternal()
        {
            Dictionary<string, List<string>> result = new Dictionary<string, List<string>>();
            if (File.Exists(Consts.defaultConfigPath))
            {
                foreach (string conf in File.ReadAllLines(Consts.defaultConfigPath))
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

        private void ReadConfig(Logger logger = null)
        {
            if (logger == null)
                logger = Program.Logger;
            try
            {
                Dictionary<string, List<string>> conf = ReadConfigInternal();
                List<string> workingSet;
                if (conf.TryGetValue(Consts.BANLIST_KEY, out workingSet))
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

                if (conf.TryGetValue(Consts.LIBRARIES_KEY, out workingSet))
                {
                    foreach (string library in workingSet)
                    {
                        libraryPaths.Add(library);
                    }
                    OnLibraryPathsChanged();
                }

                if (conf.TryGetValue(Consts.EXTENSIONS_KEY, out workingSet))
                {
                    foreach (string ext in workingSet)
                    {
                        extensionWhitelist.Add(ext);
                    }
                }

                if (defaultPlayer.ContainsKey(Environment.OSVersion.Platform))
                {
                    playerName = defaultPlayer[Environment.OSVersion.Platform];
                }
            }
            catch (Exception e)
            {
                Program.Logger.Log(Utils.GetErrorException(e));
            }
        }

        private void LoadPlayer()
        {
            IMediaPlayerImplementation impl = null;

            if (!string.IsNullOrEmpty(Program.Args.Player))
            {
                playerName = Program.Args.Player;
            }

            if (string.IsNullOrEmpty(playerName))
            {
                Console.WriteLine("There was an error finding an appropriate player to load - loading in silent mode");
            }
            else
            {
                switch (playerName)
                {
                    case "naudio":
                        impl = new NAudioImplementation();
                        break;
                    case "mplayer":
                        impl = new MPlayerImplementation();
                        break;
                    default:
                        Console.WriteLine("There was an error finding an appropriate player to load - loading in silent mode");
                        break;
                }
            }

            if (impl == null)
            {
                impl = new SilentImplementation();
            }

            Player = new MediaPlayer(impl, 50);
        }

        private void WriteConfigInternal(Dictionary<string, List<string>> conf)
        {
            File.WriteAllLines(Consts.defaultConfigPath, conf.Keys.Select(x => x + "=" + (conf[x].Count > 0 ? conf[x].Aggregate((a, b) => a + "," + b) : "")));
        }

        public void WriteConfig(Logger logger = null)
        {
            if (logger == null)
                logger = Program.Logger;
            try
            {
                Dictionary<string, List<string>> conf = new Dictionary<string, List<string>>();
                conf[Consts.BANLIST_KEY] = banlist.Select(x => x.ToString()).ToList();
                conf[Consts.LIBRARIES_KEY] = libraryPaths;
                conf[Consts.EXTENSIONS_KEY] = extensionWhitelist;
                WriteConfigInternal(conf);
            }
            catch (Exception e)
            {
                logger.Log(Utils.GetErrorException(e));
            }
        }

        // TODO: Make Load/Save functions async
        public void LoadSourceState(string path)
        {
            int pos, vol;
            bool playing;
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
            Program.Server.BroadcastMessage(HaProtoImpl.Opcode.SETDB, new HaProtoImpl.SETDB() { dataSource = DataSource });
            Program.Mover.OnSetDataSource();
            Player.OnSongChanged();
            Player.Playing = playing;
            Player.Position = pos;
            Player.Volume = vol;
        }

        public void SaveSourceState(string path)
        {
            using (FileStream fs = File.Create(path + "$TMP"))
            {
                Playlist.SerializeCounters(fs);
                PlaylistItem.SerializeCounters(fs);
                DataSource.Serialize(fs);
            }
            if (File.Exists(path))
            {
                File.Delete(path);
            }
            // Racy...
            File.Move(path + "$TMP", path);
        }

        public void OnLibraryPathsChanged()
        {
            foreach (FileSystemWatcher fsw in watchers)
            {
                fsw.Dispose();
            }
            watchers.Clear();
            // TODO: Implement async watchers
            /*foreach (string path in libraryPaths)
            {
                FileSystemWatcher fsw = new FileSystemWatcher(path) { NotifyFilter = NotifyFilters.FileName, IncludeSubdirectories = true };
                fsw.Created += Fsw_Created;
                fsw.Deleted += Fsw_Deleted;
                fsw.Renamed += Fsw_Renamed;
                watchers.Add(fsw);
                fsw.EnableRaisingEvents = true;
            }*/
        }
        /*
        private void Fsw_Created(object sender, FileSystemEventArgs e)
        {
            lock (extensionWhitelist)
            {
                if (!extensionWhitelist.Contains(Path.GetExtension(e.FullPath).ToLower()))
                    return;
            }
            HaProtoImpl.LIBRARY_ADD packet = new HaProtoImpl.LIBRARY_ADD() { paths = new List<string> { e.FullPath } };
            Utils.ExecutePacketAndBroadcast(HaProtoImpl.Opcode.LIBRARY_ADD, packet);
        }

        private void Fsw_Deleted(object sender, FileSystemEventArgs e)
        {
            lock (extensionWhitelist)
            {
                if (!extensionWhitelist.Contains(Path.GetExtension(e.FullPath).ToLower()))
                    return;
            }
            HaProtoImpl.LIBRARY_REMOVE packet = new HaProtoImpl.LIBRARY_REMOVE() { paths = new List<string> { e.FullPath } };
            Utils.ExecutePacketAndBroadcast(HaProtoImpl.Opcode.LIBRARY_REMOVE, packet);
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
            Utils.ExecutePacketsAndBroadcast(ops, packets);
        }*/

        public void BeginReloadLibrary(Logger logger = null)
        {
            if (logger == null)
                logger = Program.Logger;
            // TODO: make this async
            ReloadLibrary(logger);
        }

        private void ReloadLibrary(Logger logger = null)
        {
            if (logger == null)
                logger = Program.Logger;
            HaProtoImpl.LIBRARY_RESET result = null;
            List<string> paths = new List<string>(), exts = new List<string>();
            paths = libraryPaths.ToList();
            exts = extensionWhitelist.ToList();
            List<string> index = Reindex(paths, exts, logger);
            if (index == null)
            {
                return;
            }
            result = new HaProtoImpl.LIBRARY_RESET() { paths = index };
            indexerFinished = true;
            if (result != null)
            {
                Utils.ExecutePacketAndBroadcast(HaProtoImpl.Opcode.LIBRARY_RESET, result);
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

        private List<string> Reindex(List<string> sources, List<string> exts, Logger logger = null)
        {
            if (logger == null)
                logger = Program.Logger;
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
                logger.Log(Utils.GetErrorException(error));
                return null;
            }
            else
            {
                return result;
            }
        }

        void player_SongChanged(object sender, PlaylistItem item)
        {
            Program.Server.BroadcastMessage(HaProtoImpl.Opcode.SETSONG, new HaProtoImpl.SETSONG() { uid = item == null ? -1 : item.UID });
        }

        void player_PlayingChanged(object sender, bool playing)
        {
            if (dataSource.Playing != playing)
            {
                dataSource.Playing = playing;
                Program.Server.BroadcastMessage(HaProtoImpl.Opcode.SETPLAYING, new HaProtoImpl.SETPLAYING() { playing = playing });
            }
        }

        public void AnnounceIndexChange()
        {
            Player.OnSongChanged();
        }

        public void SetPlaying(bool p)
        {
            Player.Playing = p;
        }

        public void SetVolume(int vol)
        {
            Player.Volume = vol;
        }

        public void SetPosition(int pos)
        {
            Player.Position = pos;
        }

        public ServerDataSource DataSource { get => dataSource; set => dataSource = value; }
        public List<IPAddress> Banlist { get => banlist; }
        public List<string> LibraryPaths { get => libraryPaths; }
        public List<string> ExtensionWhitelist { get => extensionWhitelist; }
        public string PlayerName { get => playerName; set => playerName = value; }
    }
}
