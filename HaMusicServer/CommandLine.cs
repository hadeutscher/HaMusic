/* Copyright (C) 2017 Yuval Deutscher

* This Source Code Form is subject to the terms of the Mozilla Public
* License, v. 2.0. If a copy of the MPL was not distributed with this
* file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.IO;

namespace HaMusicServer
{
    public class CommandLineArgs
    {
        private string player = "";
        private bool load = true;
        private List<string> commands = new List<string>();

        private static void ShowHelpAndExit()
        {
            Console.WriteLine("usage: HaMusicServer [OPTIONS]");
            Console.WriteLine("");
            Console.WriteLine("Options:");
            Console.WriteLine("  -c, --command      run command at boot");
            Console.WriteLine("  -C, --clean        do not load previous database");
            Console.WriteLine("  -p, --player       enforce a specific media player");
            Console.WriteLine("  -h, --help         show this help");
            Environment.Exit(0);
        }

        public CommandLineArgs()
        {
            string[] args = Environment.GetCommandLineArgs();
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
                    case "-p":
                    case "--player":
                        i++;
                        if (i >= args.Length)
                        {
                            ShowHelpAndExit();
                        }
                        player = args[i];
                        break;
                    default:
                        ShowHelpAndExit();
                        break;
                }
            }
        }

        public string Player { get => player; set => player = value; }

        public void Run()
        {
            if (load)
            {
                try
                {
                    Program.Core.LoadSourceState(Consts.defaultSourcePath);
                }
                catch (FileNotFoundException)
                {
                }
                catch (Exception e)
                {
                    Program.Logger.Log(Utils.GetErrorException(e));
                }
            }
            foreach (string command in commands)
            {
                Program.Shell.Exec(command);
            }
        }
    }
}