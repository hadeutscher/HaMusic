/* Copyright (C) 2017 Yuval Deutscher

* This Source Code Form is subject to the terms of the Mozilla Public
* License, v. 2.0. If a copy of the MPL was not distributed with this
* file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using HaMusicLib;
using System;
using System.Collections.Generic;
using System.IO;

namespace HaMusicServer
{
    class Program
    {
        public static ServerAsync Server;
        public static ServerCore Core;
        public static Shell Shell;
        public static Logger Logger;
        public static Mover Mover;

        private static void ShowHelpAndExit()
        {
            Console.WriteLine("usage: HaMusicServer [OPTIONS]");
            Console.WriteLine("");
            Console.WriteLine("Options:");
            Console.WriteLine("  -c, --command      run command at boot");
            Console.WriteLine("  -C, --clean        do not load previous database");
            Console.WriteLine("  -h, --help         show this help");
            Environment.Exit(0);
        }

        private static void ParseCommandLine()
        {
            string[] args = Environment.GetCommandLineArgs();
            List<string> commands = new List<string>();
            bool load = true;
            for (int i = 1; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "-c":
                    case "--command":
                        i++;
                        if (i >= args.Length)
                        {
                            ShowHelpAndExit();
                        }
                        commands.Add(args[i]);
                        break;
                    case "-C":
                    case "--clean":
                        load = false;
                        break;
                    case "-h":
                    case "--help":
                        ShowHelpAndExit();
                        break;
                    default:
                        ShowHelpAndExit();
                        break;
                }
            }
            if (load)
            {
                try
                {
                    Core.LoadSourceState(Consts.defaultSourcePath);
                }
                catch (FileNotFoundException)
                {
                }
                catch (Exception e)
                {
                    Logger.Log(Utils.GetErrorException(e));
                }
            }
            foreach (string command in commands)
            {
                Shell.Exec(command);
            }
        }

        static void Main(string[] args)
        {
            HaProtoImpl.Entity = HaProtoImpl.HaMusicEntity.Server;
            Logger = new Logger();
            Core = new ServerCore();
            Mover = new Mover();
            Server = new ServerAsync();
            Shell = new Shell();
            ParseCommandLine();

            Core.Run();
            Server.Run();
            Shell.Run(); // Does not return
        }
    }
}
