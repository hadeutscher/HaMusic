/* Copyright (C) 2015 haha01haha01

* This Source Code Form is subject to the terms of the Mozilla Public
* License, v. 2.0. If a copy of the MPL was not distributed with this
* file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Linq;
using System.Windows.Data;

namespace HaMusic.Wpf
{
    public class SeekTimeConverter : IMultiValueConverter
    {
        private string TranslateTime(int time)
        {
            if (time == -1)
                time = 0;
            int secs = time % 60;
            time = time / 60;
            int mins = time % 60;
            time = time / 60;
            return time == 0 ? mins.ToString() + ":" + secs.ToString("D2") : time.ToString() + ":" + mins.ToString("D2") + ":" + secs.ToString("D2");
        }

        public object Convert(object[] values, Type targetType, object parameter,
            System.Globalization.CultureInfo culture)
        {
            return values.Select(x => TranslateTime((int)x)).Aggregate((x, y) => x + " / " + y);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter,
            System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
