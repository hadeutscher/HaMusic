/* Copyright (C) 2015 haha01haha01

* This Source Code Form is subject to the terms of the Mozilla Public
* License, v. 2.0. If a copy of the MPL was not distributed with this
* file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using HaMusicLib;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace HaMusic.Wpf
{
    public static class FoundItemBehavior
    {
        public static DependencyProperty FoundItemProperty =
            DependencyProperty.RegisterAttached("FoundItem", typeof(PlaylistItem), typeof(FoundItemBehavior), new PropertyMetadata(null, PropertyChanged));

        public static PlaylistItem GetFoundItem(DependencyObject obj)
        {
            return (PlaylistItem)obj.GetValue(FoundItemProperty);
        }

        public static void SetFoundItem(DependencyObject obj,
                                          PlaylistItem value)
        {
            obj.SetValue(FoundItemProperty, value);
        }
        public static void PropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            TextBlock element = (TextBlock)d;
            PlaylistItem curr = (PlaylistItem)element.GetValue(FoundItemProperty);
            PlaylistItem item = element.DataContext as PlaylistItem;
            if (curr != null && item != null && curr.UID == item.UID)
                element.Background = Brushes.Yellow;
            else
                element.Background = Brushes.Transparent;
        }
    }
}
