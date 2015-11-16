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
    public static class AnimatedCurrentItemBehavior
    {
        public static DependencyProperty AnimatedCurrentItemProperty =
            DependencyProperty.RegisterAttached("AnimatedCurrentItem", typeof(PlaylistItem), typeof(AnimatedCurrentItemBehavior), new PropertyMetadata(null, PropertyChanged));

        public static PlaylistItem GetAnimatedCurrentItem(DependencyObject obj)
        {
            return (PlaylistItem)obj.GetValue(AnimatedCurrentItemProperty);
        }

        public static void SetAnimatedCurrentItem(DependencyObject obj,
                                          PlaylistItem value)
        {
            obj.SetValue(AnimatedCurrentItemProperty, value);
        }

        public static readonly Color Color1 = Color.FromArgb(0xFF, 0x40, 0x95, 0xFF);
        public static readonly Color Color2 = Colors.White;
        public static readonly Duration duration = new Duration(TimeSpan.FromSeconds(4));

        public static void PropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            TextBlock element = (TextBlock)d;
            PlaylistItem curr = (PlaylistItem)element.GetValue(AnimatedCurrentItemProperty);
            PlaylistItem item = element.DataContext as PlaylistItem;
            if (curr != null && curr.UID == item.UID)
            {
                GradientStop stop1 = new GradientStop(Color1, -2d);
                GradientStop stop2 = new GradientStop(Color2, -1d);
                GradientStop stop3 = new GradientStop(Color1, 0d);
                GradientStop stop4 = new GradientStop(Color2, 1d);
                element.Background = new LinearGradientBrush(new GradientStopCollection(new List<GradientStop> { stop1, stop2, stop3, stop4 }));
                stop1.BeginAnimation(GradientStop.OffsetProperty, new DoubleAnimation(-2, 0, duration) { RepeatBehavior = RepeatBehavior.Forever });
                stop2.BeginAnimation(GradientStop.OffsetProperty, new DoubleAnimation(-1, 1, duration) { RepeatBehavior = RepeatBehavior.Forever });
                stop3.BeginAnimation(GradientStop.OffsetProperty, new DoubleAnimation(0, 2, duration) { RepeatBehavior = RepeatBehavior.Forever });
                stop4.BeginAnimation(GradientStop.OffsetProperty, new DoubleAnimation(1, 3, duration) { RepeatBehavior = RepeatBehavior.Forever });
            }
            else
            {
                element.Background = Brushes.Transparent;
            }
        }
    }
}
