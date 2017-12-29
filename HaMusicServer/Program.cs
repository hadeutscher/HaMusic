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
        public static CommandLineArgs Args;

        static void Main(string[] args)
        {
            HaProtoImpl.Entity = HaProtoImpl.HaMusicEntity.Server;
            Args = new CommandLineArgs();
            Logger = new Logger();
            Core = new ServerCore();
            Mover = new Mover();
            Server = new ServerAsync();
            Shell = new Shell();

            Args.Run();
            Core.Run();
            Server.Run();
            Shell.Run(); // Does not return
        }
    }
}
