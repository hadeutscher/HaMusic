/* Copyright (C) 2015 haha01haha01

* This Source Code Form is subject to the terms of the Mozilla Public
* License, v. 2.0. If a copy of the MPL was not distributed with this
* file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows.Forms;

namespace HaMusicServer
{
    static class Program
    {
        public static string GetErrorException(Exception e, [CallerMemberName] string name = "Unknown")
        {
            return string.Format("{0}: {1}\r\n\r\n{2}", name, e.Message, e.StackTrace);
        }

        public static string GetLocalSettingsFolder()
        {
            string appdata = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string our_folder = Path.Combine(appdata, "HaMusic");
            if (!Directory.Exists(our_folder))
                Directory.CreateDirectory(our_folder);
            return our_folder;
        }

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
            Environment.Exit(0);
        }
    }
}
