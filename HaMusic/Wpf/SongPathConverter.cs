/* Copyright (C) 2015 haha01haha01

* This Source Code Form is subject to the terms of the Mozilla Public
* License, v. 2.0. If a copy of the MPL was not distributed with this
* file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Windows.Data;

namespace HaMusic.Wpf
{
    public class SongPathConverter : IValueConverter
    {
        private const int MaxSongNameLength = 60;

        public object Convert(object value, Type targetType, object parameter,
                          System.Globalization.CultureInfo culture)
        {
            string result;
            if (value is string && !string.IsNullOrEmpty((string)value))
            {
                string strVal = (string)value;
                
                // Remove folder heirarchy
                int lastSep = strVal.LastIndexOfAny(new char[] { '/', '\\' });
                if (lastSep != -1 && lastSep + 1 < strVal.Length)
                    strVal = strVal.Substring(lastSep + 1);

                // Remove extension
                int lastDot = strVal.LastIndexOf('.');
                if (lastDot != -1)
                    strVal = strVal.Substring(0, lastDot);

                // Trim song name
                if (strVal.Length > MaxSongNameLength)
                {
                    strVal = strVal.Substring(0, MaxSongNameLength - 3);
                    if (strVal.EndsWith("."))
                        strVal = strVal.TrimEnd('.');
                    strVal += "...";
                }
                result = strVal;
            }
            else
            {
                result = "None";
            }
            return "Now Playing:\r\n" + result;
        }

        public object ConvertBack(object value, Type targetType, object parameter,
                              System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
