/* Copyright (C) 2015 haha01haha01

* This Source Code Form is subject to the terms of the Mozilla Public
* License, v. 2.0. If a copy of the MPL was not distributed with this
* file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using GongSolutions.Wpf.DragDrop;
using HaMusicLib;
using System.Windows;

namespace HaMusic.DragDrop
{
    public class TabHeaderDropHandler : IDropTarget
    {
        Data data;

        public TabHeaderDropHandler(Data data)
        {
            this.data = data;
        }

        public void DragOver(IDropInfo dropInfo)
        {
            data.SelectedPlaylist = (Playlist)((FrameworkElement)dropInfo.VisualTarget).DataContext;
        }

        public void Drop(IDropInfo dropInfo)
        {
        }
    }
}
