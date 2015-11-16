/* Copyright (C) 2015 haha01haha01

* This Source Code Form is subject to the terms of the Mozilla Public
* License, v. 2.0. If a copy of the MPL was not distributed with this
* file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using HaMusicLib;
using System.Windows;
using System.Windows.Controls;

namespace HaMusic.Wpf
{
    public static class NextItemImageBehavior
    {
        public static DependencyProperty NextItemImageProperty =
            DependencyProperty.RegisterAttached("NextItemImage", typeof(PlaylistItem), typeof(NextItemImageBehavior), new PropertyMetadata(null, PropertyChanged));

        public static PlaylistItem GetNextItemImage(DependencyObject obj)
        {
            return (PlaylistItem)obj.GetValue(NextItemImageProperty);
        }

        public static void SetNextItemImage(DependencyObject obj,
                                          PlaylistItem value)
        {
            obj.SetValue(NextItemImageProperty, value);
        }

        public static void PropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            Image element = (Image)d;
            PlaylistItem next = (PlaylistItem)element.GetValue(NextItemImageProperty);
            PlaylistItem item = element.DataContext as PlaylistItem;
            if (next != null && next.UID == item.UID && 
                element.Tag is ServerDataSource &&
                ((ServerDataSource)element.Tag).NextItemOverrideAction != HaProtoImpl.InjectionType.INJECT_AS_IF_SONG_ENDED)
                element.Visibility = Visibility.Visible;
            else
                element.Visibility = Visibility.Collapsed;
        }
    }
}
