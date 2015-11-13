/* Copyright (C) 2015 haha01haha01

* This Source Code Form is subject to the terms of the Mozilla Public
* License, v. 2.0. If a copy of the MPL was not distributed with this
* file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using HaMusicLib;
using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows.Data;

namespace HaMusic.Wpf
{
    public class MediaBrowserFilterConverter : IMultiValueConverter
    {
        private MediaBrowser parent;

        public MediaBrowserFilterConverter(MediaBrowser parent)
        {
            this.parent = parent;
        }

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length != 2)
                return null;
            ObservableCollection<PlaylistItem> items = null;
            string filter = null;
            foreach (object value in values)
            {
                if (value is ObservableCollection<PlaylistItem>)
                    items = (ObservableCollection<PlaylistItem>)value;
                else if (value is string)
                    filter = (string)value;
            }
            if (items == null || filter == null)
                return null;
            return parent.FilterData(items, filter);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
