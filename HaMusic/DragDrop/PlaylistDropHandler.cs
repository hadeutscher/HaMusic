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
            if (dropInfo.Data is IEnumerable<PlaylistItem> &&
                dropInfo.DragInfo.VisualSource is FrameworkElement &&
                ((FrameworkElement)dropInfo.DragInfo.VisualSource).DataContext == data.ServerDataSource.LibraryPlaylist)
            {
                dropInfo.DropTargetAdorner = DropTargetAdorners.Insert;
                dropInfo.Effects = DragDropEffects.Copy;
            }
            else if (dropInfo.Data is IEnumerable<PlaylistItem>)
            {
                dropInfo.DropTargetAdorner = DropTargetAdorners.Insert;
                dropInfo.Effects = DragDropEffects.Move;
            }
            else if (dropInfo.Data is DataObject && ((DataObject)dropInfo.Data).GetDataPresent(DataFormats.FileDrop))
            {
                dropInfo.DropTargetAdorner = DropTargetAdorners.Insert;
                dropInfo.Effects = DragDropEffects.Copy;
            }
        }

        public long GetAfterFromDropInfo(IDropInfo dropInfo)
        {
            if ((dropInfo.InsertPosition & RelativeInsertPosition.AfterTargetItem) != 0)
            {
                if (dropInfo.TargetItem == null)
                    return data.SelectedPlaylist.PlaylistItems[data.SelectedPlaylist.PlaylistItems.Count - 1].UID;
                else
                    return ((PlaylistItem)dropInfo.TargetItem).UID;
            }
            else if ((dropInfo.InsertPosition & RelativeInsertPosition.BeforeTargetItem) != 0)
            {
                if (dropInfo.TargetItem == null)
                    return -1;
                else
                {
                    int index = data.SelectedPlaylist.PlaylistItems.IndexOf((PlaylistItem)dropInfo.TargetItem);
                    return index == 0 ? -1 : data.SelectedPlaylist.PlaylistItems[index - 1].UID;
                }
            }
            else
            {
                return -1;
            }
        }

        public void Drop(IDropInfo dropInfo)
        {
            if (dropInfo.Data is IEnumerable<PlaylistItem> &&
                dropInfo.DragInfo.VisualSource is FrameworkElement &&
                ((FrameworkElement)dropInfo.DragInfo.VisualSource).DataContext == data.ServerDataSource.LibraryPlaylist)
            {
                parent.AddSongs(((IEnumerable<PlaylistItem>)dropInfo.Data).Select(x => x.Item), GetAfterFromDropInfo(dropInfo));
            }
            else if (dropInfo.Data is IEnumerable<PlaylistItem>)
            {
                parent.DragMoveItems((IEnumerable<PlaylistItem>)dropInfo.Data, GetAfterFromDropInfo(dropInfo));
            }
            else if (dropInfo.Data is DataObject && ((DataObject)dropInfo.Data).GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])((DataObject)dropInfo.Data).GetData(DataFormats.FileDrop);
                parent.AddSongs(files, GetAfterFromDropInfo(dropInfo));
            }
        }
    }
}
