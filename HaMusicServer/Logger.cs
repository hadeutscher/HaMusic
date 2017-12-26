/* Copyright (C) 2017 Yuval Deutscher

* This Source Code Form is subject to the terms of the Mozilla Public
* License, v. 2.0. If a copy of the MPL was not distributed with this
* file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.IO;

namespace HaMusicServer
{
    public class Logger
    {
        private StreamWriter logger;

        public Logger()
        {
            Stream fs = File.Open(Consts.defaultLogPath, FileMode.OpenOrCreate, FileAccess.Write, FileShare.Read);
            fs.Seek(0, SeekOrigin.End);
            logger = new StreamWriter(fs);
        }

        public void Log(string x)
        {
            try
            {
                logger.Write(x + "\r\n");
            }
            catch (Exception e)
            {
                Shell.WriteLines(new List<string> { "Exception when trying to log:\n", x, "Exception was: " + e.Message, e.StackTrace });
            }
        }

        public void FlushLog() => logger.Flush();
    }
}
