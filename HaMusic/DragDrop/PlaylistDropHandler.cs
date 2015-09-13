/* Copyright (C) 2015 haha01haha01

* This Source Code Form is subject to the terms of the Mozilla Public
* License, v. 2.0. If a copy of the MPL was not distributed with this
* file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using GongSolutions.Wpf.DragDrop;
using HaMusicLib;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace HaMusic.DragDrop
{
    public class PlaylistDropHandler : IDropTarget
    {
        MainWindow parent;
        Data data;

        public PlaylistDropHandler(MainWindow parent, Data data)
        {
            this.parent = parent;
            this.data = data;
        }

        public void DragOver(IDropInfo dropInfo)
        {
            if (dropInfo.Data is PlaylistItem || dropInfo.Data is IEnumerable<PlaylistItem>)
            {
                dropInfo.DropTargetAdorner = DropTargetAdorners.Insert;
                dropInfo.Effects = DragDropEffects.Move;
            }
            else if (dropInfo.Data is DataObject && ((DataObject)dropInfo.Data).GetDataPresent(DataFormats.FileDrop))
            {
                dropInfo.DropTargetAdorner = DropTargetAdorners.Insert;
                dropInfo.Effects = DragDropEffects.Copy;
            }
            else if (dropInfo.Data is string || dropInfo.Data is IEnumerable<string>)
            {
                dropInfo.DropTargetAdorner = DropTargetAdorners.Insert;
                dropInfo.Effects = DragDropEffects.Copy;
            }
        }

        public long GetAfterFromDropInfo(IDropInfo dropInfo)
        {
            long after;
            if ((dropInfo.InsertPosition & RelativeInsertPosition.AfterTargetItem) != 0)
            {
                after = dropInfo.TargetItem == null ? data.SelectedPlaylist.PlaylistItems[data.SelectedPlaylist.PlaylistItems.Count - 1].UID : ((PlaylistItem)dropInfo.TargetItem).UID;
            }
            else if ((dropInfo.InsertPosition & RelativeInsertPosition.BeforeTargetItem) != 0)
            {
                int index = data.SelectedPlaylist.PlaylistItems.IndexOf((PlaylistItem)dropInfo.TargetItem);
                after = index == 0 ? -1 : data.SelectedPlaylist.PlaylistItems[index - 1].UID;
            }
            else
            {
                return -1;
            }
            return after;
        }

        public void Drop(IDropInfo dropInfo)
        {
            if ((dropInfo.Data is PlaylistItem || dropInfo.Data is IEnumerable<PlaylistItem>) && dropInfo.TargetItem != null)
            {
                PlaylistItem[] items = dropInfo.Data is PlaylistItem ? new PlaylistItem[] { (PlaylistItem)dropInfo.Data } : ((IEnumerable<PlaylistItem>)dropInfo.Data).ToArray();
                parent.DragMoveItems(items, GetAfterFromDropInfo(dropInfo));
            }
            else if (dropInfo.Data is DataObject && ((DataObject)dropInfo.Data).GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])((DataObject)dropInfo.Data).GetData(DataFormats.FileDrop);
                parent.AddSongs(files, GetAfterFromDropInfo(dropInfo));
            }
            else if (dropInfo.Data is string || dropInfo.Data is IEnumerable<string>)
            {
                string[] files = dropInfo.Data is string ? new string[] { (string)dropInfo.Data } : ((IEnumerable<string>)dropInfo.Data).ToArray();
                parent.AddSongs(files, GetAfterFromDropInfo(dropInfo));
            }
        }
    }
}
