/* Copyright (C) 2015 haha01haha01

* This Source Code Form is subject to the terms of the Mozilla Public
* License, v. 2.0. If a copy of the MPL was not distributed with this
* file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using GongSolutions.Wpf.DragDrop;
using HaMusicLib;
using System;
using System.Collections.Generic;
using System.Windows;

namespace HaMusic.DragDrop
{
    public class TabHeaderDropHandler : IDropTarget
    {
        Data data;

        private class HoverDelayData
        {
            public int hoverStartTime;
            public IDragInfo hoverDragInfo = null;
        }

        private Dictionary<UIElement, HoverDelayData> delayData = new Dictionary<UIElement, HoverDelayData>();

        public TabHeaderDropHandler(Data data)
        {
            this.data = data;
        }

        public void DragOver(IDropInfo dropInfo)
        {
            lock (delayData)
            {
                FrameworkElement element = (FrameworkElement)dropInfo.VisualTarget;
                HoverDelayData hdd;
                if (!delayData.TryGetValue(element, out hdd))
                {
                    hdd = new HoverDelayData();
                    delayData.Add(element, hdd);
                    element.Unloaded += Element_Unloaded;
                }
                if (hdd.hoverDragInfo != null && hdd.hoverDragInfo == dropInfo.DragInfo)
                {
                    int timeDiff = Environment.TickCount - hdd.hoverStartTime;
                    if (timeDiff > 500)
                    {
                        if (timeDiff < 1000)
                        {
                            if (dropInfo.VisualTarget is FrameworkElement && ((FrameworkElement)dropInfo.VisualTarget).DataContext is Playlist)
                                data.SelectedPlaylist = (Playlist)((FrameworkElement)dropInfo.VisualTarget).DataContext;
                        }
                        hdd.hoverDragInfo = null;
                    }
                }
                else
                {
                    hdd.hoverStartTime = Environment.TickCount;
                    hdd.hoverDragInfo = dropInfo.DragInfo;
                }
            }
            
        }

        private void Element_Unloaded(object sender, RoutedEventArgs e)
        {
            lock (delayData)
            {
                if (sender is UIElement && delayData.ContainsKey((UIElement)sender))
                    delayData.Remove((UIElement)sender);
            }
        }

        public void Drop(IDropInfo dropInfo)
        {
        }
    }
}
