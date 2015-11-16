/* Copyright (C) 2015 haha01haha01

* This Source Code Form is subject to the terms of the Mozilla Public
* License, v. 2.0. If a copy of the MPL was not distributed with this
* file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using HaMusicLib;
using System.Windows;
using System.Windows.Controls;

namespace HaMusic.Wpf
{
    public static class ListViewBehavior
    {
        public static DependencyProperty ItemInViewProperty =
            DependencyProperty.RegisterAttached("ItemInView", typeof(PlaylistItem), typeof(ListViewBehavior), new PropertyMetadata(null, ItemInViewChanged));

        public static PlaylistItem GetItemInView(DependencyObject obj)
        {
            return (PlaylistItem)obj.GetValue(ItemInViewProperty);
        }

        public static void SetItemInView(DependencyObject obj,
                                          PlaylistItem value)
        {
            obj.SetValue(ItemInViewProperty, value);
        }

        public static void ItemInViewChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ListView element = (ListView)d;
            PlaylistItem item = (PlaylistItem)element.GetValue(ItemInViewProperty);
            Playlist pl = element.DataContext as Playlist;
            if (item != null && pl != null && pl.PlaylistItems.Contains(item))
            {
                element.ScrollIntoView(item);
            }
        }
    }
}
