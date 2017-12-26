/* Copyright (C) 2017 Yuval Deutscher

* This Source Code Form is subject to the terms of the Mozilla Public
* License, v. 2.0. If a copy of the MPL was not distributed with this
* file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.IO;

namespace HaMusicServer
{
    public static class Consts
    {
        public static string GetLocalSettingsFolder()
        {
            string appdata = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string our_folder = Path.Combine(appdata, "HaMusic");
            if (!Directory.Exists(our_folder))
                Directory.CreateDirectory(our_folder);
            return our_folder;
        }

        public static string defaultLogPath = Path.Combine(GetLocalSettingsFolder(), "hms.log");
        public static string defaultSourcePath = Path.Combine(GetLocalSettingsFolder(), "hms.db");
        public static string defaultConfigPath = Path.Combine(GetLocalSettingsFolder(), "config.txt");

        public const string BANLIST_KEY = "banlist";
        public const string LIBRARIES_KEY = "libraries";
        public const string EXTENSIONS_KEY = "extensions";
    }
}
