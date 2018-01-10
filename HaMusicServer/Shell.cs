/* Copyright (C) 2017 Yuval Deutscher

* This Source Code Form is subject to the terms of the Mozilla Public
* License, v. 2.0. If a copy of the MPL was not distributed with this
* file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using HaMusicLib;
using Mono.Terminal;
using ProtoBuf;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace HaMusicServer
{
    public class Shell
    {
        private static Regex commandLineParser = new Regex("([^\" ][^ ]*)|(\"[^\"]*\")");

        public static string[] CommandLineToArgs(string commandLine)
        {
            return commandLineParser.Matches(commandLine).OfType<Match>().Select(m => m.Value).ToArray();
        }

        public static void WriteLines(IList<string> lines)
        {
            if (lines.Count > 0)
            {
                Console.WriteLine(lines.Aggregate((x, y) => x + "\r\n" + y));
            }
        }

        public async Task Exec(string s)
        {
            if (string.IsNullOrWhiteSpace(s))
                return;
            try
            {
                string[] cmd = CommandLineToArgs(s);
                if (cmd.Length < 0)
                    return;
                string command = cmd[0];
                cmd = Enumerable.Skip(cmd, 1).ToArray();
                MethodInfo mi = null;
                try
                {
                    mi = GetType().GetMethod("command_" + command);
                }
                catch { }
                if (mi == null)
                {
                    Console.WriteLine(string.Format("Couldn't find command {0}, try `help`", command));
                    return;
                }
                await (Task)mi.Invoke(this, new object[] { cmd });
            }
            catch (Exception e)
            {
                if (e is TargetInvocationException)
                    e = e.InnerException;
                Console.WriteLine("Unhandled exception occurred: " + e.Message + "\r\n" + e.StackTrace);
            }
        }

        public Task command_help(string[] args)
        {
            WriteLines(new List<string> {
                        string.Format("HaShell [Version {0}]", ServerDataSource.LocalVersion),
                        "",
                        "\thelp - show this help",
                        "\tclients - print all clients",
                        "\tkick [--ban] <ip> - disconnect client, optionally banning it",
                        "\tunban <ip> - unban client",
                        "\tbanlist - print banlist",
                        "\tsource [add|remove|reload|addext|removeext|list] <path|ext>",
                        "\ttail [n] - print last n lines from the log, default 10",
                        "\tflush [path] - force DataSource flush, optionally into a specific path",
                        "\tload [path] - load DataSource, optionally from a specific path",
                        "\tbackup [path] - backup playlists and songs in cross-version format",
                        "\trestore [path] - restore playlists and songs from cross-version format",
                        "\tplayer - see which player is currently in use",
                        "\texit - exit server"
                    });
            return Task.FromResult(false);
        }

        public Task command_clients(string[] args)
        {
            List<string> output = new List<string>();
            foreach (ClientAsync c in Program.Server.Clients)
            {
                output.Add(c.ID);
            }
            WriteLines(output);
            return Task.FromResult(false);
        }

        public Task command_kick(string[] args)
        {
            bool ban = false;
            IPAddress ipaddr = null;
            foreach (string arg in args)
            {
                if (arg == "--ban")
                    ban = true;
                else
                {
                    IPAddress.TryParse(arg, out ipaddr);
                }
            }
            if (ipaddr == null)
            {
                Console.WriteLine("kick: must supply an IP address");
                return Task.FromResult(false);
            }
            foreach (ClientAsync c in Program.Server.Clients)
            {
                if (c.IP.Equals(ipaddr))
                {
                    c.Kill();
                }
            }
            if (ban)
            {
                Program.Core.Banlist.Add(ipaddr);
                Program.Core.WriteConfig();
            }
            return Task.FromResult(false);
        }

        public Task command_unban(string[] args)
        {
            IPAddress addr = null;
            if (args.Length < 1 || !IPAddress.TryParse(args[0], out addr))
            {
                Console.WriteLine("unban: must supply an IP address");
                return Task.FromResult(false);
            }
            Program.Core.Banlist.RemoveAll(x => x.Equals(addr));
            Program.Core.WriteConfig();
            return Task.FromResult(false);
        }

        public Task command_banlist(string[] args)
        {
            List<string> output;
            output = Program.Core.Banlist.Select(x => x.ToString()).ToList();
            WriteLines(output);
            return Task.FromResult(false);
        }

        public Task command_player(string[] args)
        {
            Console.WriteLine(Program.Core.PlayerName);
            return Task.FromResult(false);
        }

        public static async Task<string[]> ReadEndLines(string path, int numberOfTokens)
        {
            byte[] buffer = new byte[1];

            using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                int tokenCount = 0;

                for (int position = 1; position < fs.Length; position++)
                {
                    fs.Seek(-position, SeekOrigin.End);
                    await fs.ReadAsync(buffer, 0, buffer.Length);

                    if (buffer[0] == 0x0A)
                    {
                        tokenCount++;
                        if (tokenCount == numberOfTokens)
                        {
                            byte[] returnBuffer = new byte[fs.Length - fs.Position];
                            await fs.ReadAsync(returnBuffer, 0, returnBuffer.Length);
                            return Encoding.ASCII.GetString(returnBuffer).Split(new string[] { "\r\n" }, StringSplitOptions.None);
                        }
                    }
                }

                // handle case where number of tokens in file is less than numberOfTokens
                fs.Seek(0, SeekOrigin.Begin);
                buffer = new byte[fs.Length];
                await fs.ReadAsync(buffer, 0, buffer.Length);
                return Encoding.ASCII.GetString(buffer).Split(new string[] { "\r\n" }, StringSplitOptions.None);
            }
        }

        public async Task command_tail(string[] args)
        {
            int n;
            if (args.Length == 0 || !int.TryParse(args[0], out n))
                n = 10;
            Program.Logger.FlushLog();
            WriteLines((await ReadEndLines(Consts.defaultLogPath, n)).ToList());
        }

        public Task command_flush(string[] args)
        {
            string path = args.Length > 0 ? args[0] : Consts.defaultSourcePath;
            Program.Core.SaveSourceState(path);
            return Task.FromResult(false);
        }

        public Task command_load(string[] args)
        {
            string path = args.Length > 0 ? args[0] : Consts.defaultSourcePath;
            Program.Core.LoadSourceState(path);
            return Task.FromResult(false);
        }

        public Task command_exit(string[] args)
        {
            Program.Logger.FlushLog();
            Environment.Exit(0);
            return Task.FromResult(false);
        }

        public Task command_source(string[] args)
        {
            if (args.Length < 1)
            {
                Console.WriteLine("source: not enough arguments, try `help`");
                return Task.FromResult(false);
            }
            switch (args[0])
            {
                case "add":
                case "remove":
                case "addext":
                case "removeext":
                    if (args.Length < 2)
                    {
                        Console.WriteLine("source: not enough arguments, try `help`");
                        return Task.FromResult(false);
                    }
                    break;
                case "reload":
                case "list":
                    break;
                default:
                    Console.WriteLine(string.Format("source: unknown operation {0}, try `help`", args[0]));
                    return Task.FromResult(false);
            }
            switch (args[0])
            {
                case "add":
                    Program.Core.LibraryPaths.Add(args[1]);
                    Program.Core.OnLibraryPathsChanged();
                    break;
                case "remove":
                    Program.Core.LibraryPaths.RemoveAll(x => x.ToLower().Contains(args[1].ToLower()));
                    Program.Core.OnLibraryPathsChanged();
                    break;
                case "addext":
                    Program.Core.ExtensionWhitelist.Add(args[1]);
                    break;
                case "removeext":
                    Program.Core.ExtensionWhitelist.RemoveAll(x => x.ToLower() == args[1].ToLower());
                    break;
                case "list":
                    Console.WriteLine("Sources:");
                    foreach (string path in Program.Core.LibraryPaths)
                    {
                        Console.WriteLine(path);
                    }
                    Console.WriteLine("Extensions:");
                    foreach (string ext in Program.Core.ExtensionWhitelist)
                    {
                        Console.WriteLine(ext);
                    }
                    break;
                case "reload":
                    Program.Core.BeginReloadLibrary();
                    break;
            }
            switch (args[0])
            {
                case "add":
                case "remove":
                case "addext":
                case "removeext":
                    Program.Core.WriteConfig();
                    break;
            }
            return Task.FromResult(false);
        }

        // Protobuf-net is shit so it cant deal with nested list, so we nest LIST CONTAINERS!
        [ProtoContract]
        private class ListContainer<T>
        {
            [ProtoMember(1, IsRequired = true)]
            public List<T> List = new List<T>();

            public ListContainer()
            {
            }
        }

        public Task command_backup(string[] args)
        {
            if (args.Length < 1)
            {
                Console.WriteLine("backup: not enough arguments, try `help`");
                return Task.FromResult(false);
            }

            ListContainer<ListContainer<string>> backup = new ListContainer<ListContainer<string>>();
            foreach (Playlist pl in Program.Core.DataSource.Playlists)
            {
                ListContainer<string> plBackup = new ListContainer<string>();
                plBackup.List.Add(pl.Name);
                foreach (PlaylistItem item in pl.PlaylistItems)
                {
                    plBackup.List.Add(item.Item);
                }
                backup.List.Add(plBackup);
            }

            using (FileStream fs = File.Create(args[0]))
            {
                Serializer.Serialize(fs, backup);
            }

            Console.WriteLine(string.Format("Backup written to {0}", args[0]));
            return Task.FromResult(false);
        }

        public Task command_restore(string[] args)
        {
            if (args.Length < 1)
            {
                Console.WriteLine("restore: not enough arguments, try `help`");
                return Task.FromResult(false);
            }

            ListContainer<ListContainer<string>> backup;
            using (FileStream fs = File.OpenRead(args[0]))
            {
                backup = Serializer.Deserialize<ListContainer<ListContainer<string>>>(fs);
            }
            Program.Core.DataSource.CurrentItem = null;
            Program.Core.DataSource.NextItemOverride = null;
            Program.Core.DataSource.Playlists.Clear();
            foreach (ListContainer<string> plBackup in backup.List)
            {
                Playlist pl = new Playlist() { Name = plBackup.List[0] };
                foreach (string item in plBackup.List.Skip(1))
                {
                    pl.PlaylistItems.Add(new PlaylistItem() { Item = item });
                }
                Program.Core.DataSource.Playlists.Add(pl);
            }

            Console.WriteLine("Loaded backup, please run `flush` and `load` to propagate changes");
            return Task.FromResult(false);
        }

        public void Run()
        {
            LineEditor le = new LineEditor("foo");

            // Prompts the user for input
            while (true)
            {
                string s = le.Edit("# ", "");
                Exec(s).Wait();
            }
        }
    }
}
