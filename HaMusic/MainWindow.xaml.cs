﻿/* Copyright (C) 2015 haha01haha01

* This Source Code Form is subject to the terms of the Mozilla Public
* License, v. 2.0. If a copy of the MPL was not distributed with this
* file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using HaMusicLib;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

namespace HaMusic
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private const int EyeCandyDisableThreshold = 1000;
        private TcpClient client = null;
        private NetworkStream stream = null;
        private Data data;
        private bool internalChanging = false;
        private Task connectionTask = null;

        public static readonly DependencyProperty SelectedPlaylistItemsProperty =
            DependencyProperty.Register("SelectedPlaylistItems", typeof(IEnumerable<PlaylistItem>), typeof(MainWindow), new PropertyMetadata());

        public MainWindow()
        {
            HaProtoImpl.Entity = HaProtoImpl.HaMusicEntity.Client;
            InitializeComponent();
            data = new Data(this);
            DataContext = data;
            SetEnabled(false);
        }

        public static string GetLocalSettingsFolder()
        {
            string appdata = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string our_folder = Path.Combine(appdata, "HaMusic");
            if (!Directory.Exists(our_folder))
                Directory.CreateDirectory(our_folder);
            return our_folder;
        }

        private void SetEnabled(bool b)
        {
            data.Enabled = b;
        }

        private static bool IsSelectionEqual(ICollection<PlaylistItem> a, ICollection<PlaylistItem> b)
        {
            if (a.Count != b.Count)
                return false;
            for (int i = 0; i < a.Count; i++)
            {
                if (a.ElementAt(i) != b.ElementAt(i))
                    return false;
            }
            return true;
        }

        public void BringSelectedItemIntoView()
        {
            if (data.ServerDataSource.CurrentItem != null)
            {
                try
                {
                    data.SelectedPlaylist = data.ServerDataSource.GetPlaylistForItem(data.ServerDataSource.CurrentItem.UID, true);
                    SetItemInView(data.ServerDataSource.CurrentItem);
                }
                catch (Exception e)
                {
                    MessageBox.Show(string.Format("Could not bring item to view, error: {0}", e.Message), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        public void SetItemInView(PlaylistItem item)
        {
            data.ItemInView = null;
            data.ItemInView = item;
        }

        public void SetFocusItem(PlaylistItem item)
        {
            data.FocusedItem = null;
            data.FocusedItem = item;
        }

        public void Send(HaProtoImpl.Opcode type, HaProtoImpl.HaProtoPacket packet)
        {
            Send(type, packet.Build());
        }

        public async void Send(HaProtoImpl.Opcode type, byte[] data)
        {
            try
            {
                await HaProtoImpl.SendAsync(stream, type, data);
            }
            catch (Exception)
            {
                Kill();
            }
        }

        public void Kill()
        {
            try
            {
                // Try to close the socket, if it's already closed then w/e
                client.Close();
            }
            catch { }
        }

        private async Task Proc()
        {
            try
            {
                Send(HaProtoImpl.Opcode.GETDB, new HaProtoImpl.GETDB());
                while (true)
                {
                    var result = await HaProtoImpl.ReceiveAsync(stream);
                    HaProtoImpl.Opcode type = result.Item1;
                    byte[] packet = result.Item2;
                    bool foo;

                    try
                    {
                        internalChanging = true;
                        switch (type)
                        {
                            case HaProtoImpl.Opcode.GETDB:
                                throw new NotSupportedException();
                            case HaProtoImpl.Opcode.SETDB:
                                HaProtoImpl.SETDB setdb = HaProtoImpl.SETDB.Parse(packet);
                                data.ServerDataSource = setdb.dataSource;
                                BringSelectedItemIntoView();
                                break;
                            case HaProtoImpl.Opcode.LIBRARY_ADD:
                            case HaProtoImpl.Opcode.LIBRARY_REMOVE:
                            case HaProtoImpl.Opcode.LIBRARY_RESET:
                                // No selection eye candy for library because it's not worth the time it will take to implement
                                HaProtoImpl.ApplyPacketToDatabase(type, packet, data.ServerDataSource, out foo);

                                // For some reason this doesn't happen automatically and I'm too tired to search why
                                // mediaBrowser.InvalidateProperty(MediaBrowser.SelectedDataProperty);
                                // For some reason, .NET sucks and InvalidateProperty doesn't do what it says it does...
                                // BindingOperations.GetBindingExpression(mediaBrowser, MediaBrowser.SelectedDataProperty).UpdateTarget();
                                // For some reason, .NET sucks and GetBindingExpression only works with single bindings...
                                BindingOperations.GetMultiBindingExpression(mediaBrowser, MediaBrowser.SelectedDataProperty).UpdateTarget();
                                break;
                            case HaProtoImpl.Opcode.ADD:
                            case HaProtoImpl.Opcode.REMOVE:
                            case HaProtoImpl.Opcode.CLEAR:
                            case HaProtoImpl.Opcode.SETSONG:
                            case HaProtoImpl.Opcode.ADDPL:
                            case HaProtoImpl.Opcode.DELPL:
                            case HaProtoImpl.Opcode.RENPL:
                            case HaProtoImpl.Opcode.REORDER:
                            case HaProtoImpl.Opcode.INJECT:
                                // These opcodes might change the list selection, we need to back it up
                                // However, only do this for small selections - if the selection is large, we would rather take the UI
                                // inconsistency over multiple seconds of delay
                                List<PlaylistItem> selectedItems = data.SelectedPlaylistItems.ToList();
                                IInputElement element = FocusManager.GetFocusedElement(this);
                                PlaylistItem focusItem = null;
                                if (element is FrameworkElement && ((FrameworkElement)element).DataContext is PlaylistItem)
                                {
                                    focusItem = (PlaylistItem)((FrameworkElement)element).DataContext;
                                }
                                int firstIndex;
                                if (selectedItems.Count == 0)
                                {
                                    firstIndex = 0;
                                }
                                else if (selectedItems.Count < EyeCandyDisableThreshold)
                                {
                                    firstIndex = selectedItems.Select(x => data.SelectedPlaylist.PlaylistItems.IndexOf(x)).Min();
                                }
                                else
                                {
                                    firstIndex = data.SelectedPlaylist.PlaylistItems.IndexOf(selectedItems[0]);
                                }

                                HaProtoImpl.ApplyPacketToDatabase(type, packet, data.ServerDataSource, out foo);

                                // Check which items still exist
                                List<PlaylistItem> newSelectedItems = new List<PlaylistItem>();
                                foreach (PlaylistItem item in selectedItems)
                                {
                                    PlaylistItem newItem;
                                    if (data.SelectedPlaylist.PlaylistItems.FastTryGet(item.UID, out newItem))
                                    {
                                        // We could have added item and it would work, but for extra safety lets add the new item
                                        newSelectedItems.Add(newItem);
                                    }
                                }

                                // Special case for deletion
                                if (selectedItems.Count > 0 && newSelectedItems.Count == 0)
                                {
                                    int selectedIndex;
                                    if (firstIndex < data.SelectedPlaylist.PlaylistItems.Count)
                                    {
                                        selectedIndex = firstIndex;
                                    }
                                    else if (data.SelectedPlaylist.PlaylistItems.Count > 0)
                                    {
                                        selectedIndex = data.SelectedPlaylist.PlaylistItems.Count - 1;
                                    }
                                    else
                                    {
                                        selectedIndex = -1;
                                    }

                                    if (selectedIndex == -1)
                                    {
                                        focusItem = null;
                                    }
                                    else
                                    {
                                        newSelectedItems.Add(focusItem = data.SelectedPlaylist.PlaylistItems[selectedIndex]);
                                    }
                                }
                                if (newSelectedItems.Count < EyeCandyDisableThreshold && !IsSelectionEqual(data.SelectedPlaylistItems, newSelectedItems))
                                {
                                    data.SelectedPlaylistItems.Clear();
                                    newSelectedItems.ForEach(x => data.SelectedPlaylistItems.Add(x));

                                    if (focusItem != null)
                                    {
                                        SetFocusItem(focusItem);
                                    }
                                }
                                break;
                            case HaProtoImpl.Opcode.SETMOVE:
                                HaProtoImpl.ApplyPacketToDatabase(type, packet, data.ServerDataSource, out foo);
                                break;
                            case HaProtoImpl.Opcode.SKIP:
                                // We should not be receiving SKIP packets, the server should translate them to SETSONG
                                throw new NotSupportedException();
                            case HaProtoImpl.Opcode.SETVOL:
                                data.ServerDataSource.Volume = HaProtoImpl.SETVOL.Parse(packet).volume;
                                break;
                            case HaProtoImpl.Opcode.SEEK:
                                HaProtoImpl.SEEK seek = HaProtoImpl.SEEK.Parse(packet);
                                data.ServerDataSource.Position = seek.pos;
                                data.ServerDataSource.Maximum = seek.max;
                                break;
                            case HaProtoImpl.Opcode.SETPLAYING:
                                data.ServerDataSource.Playing = HaProtoImpl.SETPLAYING.Parse(packet).playing;
                                break;
                            default:
                                throw new NotSupportedException();
                        }
                    }
                    finally
                    {
                        internalChanging = false;
                    }
                }
            }
            catch (Exception)
            {
                Kill();
            }
        }

        private async Task ConnectionProc()
        {
            SetEnabled(true);
            await Proc();
            SetEnabled(false);
        }

        public async void ConnectExecuted()
        {
            AddressSelector selector = new AddressSelector();
            if (selector.ShowDialog() != true)
                return;
            Kill();
            if (connectionTask != null)
                await connectionTask;
            client = selector.Result;
            stream = client.GetStream();
            connectionTask = ConnectionProc();
        }

        public void NewPlaylistExecuted()
        {
            Send(HaProtoImpl.Opcode.ADDPL, new HaProtoImpl.ADDPL());
        }

        public void NextExecuted()
        {
            Send(HaProtoImpl.Opcode.SKIP, new HaProtoImpl.SKIP());
        }

        public void DeletePlaylistExecuted()
        {
            Send(HaProtoImpl.Opcode.DELPL, new HaProtoImpl.DELPL() { uid = data.SelectedPlaylist.UID });
        }

        public void DeleteItemsExecuted(IEnumerable<PlaylistItem> items)
        {
            List<long> uids = items.Select(x => x.UID).ToList();
            if (uids.Count > 0)
                Send(HaProtoImpl.Opcode.REMOVE, new HaProtoImpl.REMOVE() { uid = GetSelectedPlaylist(), items = uids });
        }

        public void SelectItemExecuted(PlaylistItem item)
        {
            Send(HaProtoImpl.Opcode.SETSONG, new HaProtoImpl.SETSONG() { uid = item.UID });
        }

        public void InjectionExecuted(PlaylistItem item, HaProtoImpl.InjectionType type)
        {
            switch (type)
            {
                case HaProtoImpl.InjectionType.INJECT_SONG:
                    Send(HaProtoImpl.Opcode.INJECT, new HaProtoImpl.INJECT() { uid = item.UID, type = HaProtoImpl.InjectionType.INJECT_SONG });
                    break;
                case HaProtoImpl.InjectionType.INJECT_AS_IF_SONG_ENDED:
                    PlaylistItem curr = data.ServerDataSource.CurrentItem;
                    SelectItemExecuted(item);
                    if (curr != null && !data.ServerDataSource.LibraryPlaylist.PlaylistItems.ContainsKey(curr.UID))
                        Send(HaProtoImpl.Opcode.INJECT, new HaProtoImpl.INJECT() { uid = curr.UID, type = HaProtoImpl.InjectionType.INJECT_AS_IF_SONG_ENDED });
                    break;
                case HaProtoImpl.InjectionType.INJECT_AND_RETURN:
                    Send(HaProtoImpl.Opcode.INJECT, new HaProtoImpl.INJECT() { uid = item.UID, type = HaProtoImpl.InjectionType.INJECT_AND_RETURN });
                    break;
            }
        }

        public void DragMoveItems(IEnumerable<PlaylistItem> items, long after)
        {
            Send(HaProtoImpl.Opcode.REORDER, new HaProtoImpl.REORDER() { pid = data.SelectedPlaylist.UID, after = after, items = items.Select(x => x.UID).ToList() });
        }

        private T GetEventArgsItem<T>(RoutedEventArgs e)
        {
            return (T)((FrameworkElement)e.OriginalSource).DataContext;
        }

        private void items_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            try
            {
                SelectItemExecuted(GetEventArgsItem<PlaylistItem>(e));
                e.Handled = true;
            }
            catch (InvalidCastException) { }
        }

        private void mediaBrowser_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            try
            {
                InjectionExecuted(GetEventArgsItem<PlaylistItem>(e), HaProtoImpl.InjectionType.INJECT_AS_IF_SONG_ENDED);
            }
            catch (InvalidCastException) { }
        }

        private void items_KeyDown(object sender, KeyEventArgs e)
        {
            e.Handled = true;
            switch (e.Key)
            {
                case Key.Delete:
                    DeleteItemsExecuted(((ListView)sender).SelectedItems.Cast<PlaylistItem>());
                    break;
                case Key.F:
                    if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
                    {
                        findBox.SelectAll();
                        data.IsFinding = true;
                        findBox.Focus();
                    }
                    goto default; // FALLTHROUGH
                default:
                    e.Handled = false;
                    break;
            }
        }

        private void RibbonWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Kill();
        }

        private long GetSelectedPlaylist()
        {
            return data.SelectedPlaylist.UID;
        }

        public void PlayPauseExecuted()
        {
            if (!data.ServerDataSource.Playing && data.ServerDataSource.CurrentItem == null && data.SelectedPlaylistItem != null)
            {
                Send(HaProtoImpl.Opcode.SETSONG, new HaProtoImpl.SETSONG() { uid = data.SelectedPlaylistItem.UID });
            }
            else if (!data.ServerDataSource.Playing && data.ServerDataSource.CurrentItem == null && mediaBrowser.listView.SelectedItem is PlaylistItem)
            {
                Send(HaProtoImpl.Opcode.SETSONG, new HaProtoImpl.SETSONG() { uid = ((PlaylistItem)mediaBrowser.listView.SelectedItem).UID });
            }
            else if (data.ServerDataSource.Playing || data.ServerDataSource.CurrentItem != null)
            {
                Send(HaProtoImpl.Opcode.SETPLAYING, new HaProtoImpl.SETPLAYING() { playing = !data.ServerDataSource.Playing });
            }
        }

        public void StopExecuted()
        {
            Send(HaProtoImpl.Opcode.SETSONG, new HaProtoImpl.SETSONG() { uid = -1 });
        }

        private void volumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (internalChanging)
                return;
            Send(HaProtoImpl.Opcode.SETVOL, new HaProtoImpl.SETVOL() { volume = data.ServerDataSource.Volume });
        }

        private void songSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (internalChanging)
                return;
            Send(HaProtoImpl.Opcode.SEEK, new HaProtoImpl.SEEK() { pos = data.ServerDataSource.Position });
        }
        
        public void AddSongs(IEnumerable<string> paths, long after, long playlist = -1)
        {
            if (playlist == -1)
                playlist = data.SelectedPlaylist.UID;
            Send(HaProtoImpl.Opcode.ADD, new HaProtoImpl.ADD() { uid = playlist, paths = paths.ToList(), after = after });
        }

        private void RibbonWindow_Loaded(object sender, RoutedEventArgs e)
        {
            ConnectExecuted();
        }

        private void moveType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (internalChanging)
                return;
            Send(HaProtoImpl.Opcode.SETMOVE, new HaProtoImpl.SETMOVE() { move = data.ServerDataSource.Mode });
        }


        private T GetSenderItem<T>(object sender)
        {
            return (T)((FrameworkElement)sender).DataContext;
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            Playlist pl = GetSenderItem<Playlist>(sender);
            Send(HaProtoImpl.Opcode.RENPL, new HaProtoImpl.RENPL() { uid = pl.UID, name = pl.Name });
        }

        private void MenuItem_DeletePlaylist(object sender, RoutedEventArgs e)
        {
            Playlist pl = GetSenderItem<Playlist>(sender);
            Send(HaProtoImpl.Opcode.DELPL, new HaProtoImpl.DELPL() { uid = pl.UID });
        }

        private void MenuItem_RenamePlaylist(object sender, RoutedEventArgs e)
        {
            EditableLabel el = (EditableLabel)((ContextMenu)((MenuItem)sender).Parent).PlacementTarget;
            if (!el.Open)
            {
                el.SetOpen();
            }
        }

        private void MenuItem_ClearPlaylist(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Are you sure?", "Clear", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                Send(HaProtoImpl.Opcode.CLEAR, new HaProtoImpl.CLEAR() { uid = GetSenderItem<Playlist>(sender).UID });
            }
        }

        private void MenuItem_PlayItemNext(object sender, RoutedEventArgs e)
        {
            InjectionExecuted(GetSenderItem<PlaylistItem>(sender), HaProtoImpl.InjectionType.INJECT_SONG);
        }


        private void MenuItem_PlayItem(object sender, RoutedEventArgs e)
        {
            SelectItemExecuted(GetSenderItem<PlaylistItem>(sender));
        }

        private void MenuItem_PlayItemAndReturn(object sender, RoutedEventArgs e)
        {
            InjectionExecuted(GetSenderItem<PlaylistItem>(sender), HaProtoImpl.InjectionType.INJECT_AS_IF_SONG_ENDED);
        }

        private void MenuItem_PlayItemNextAndReturn(object sender, RoutedEventArgs e)
        {
            InjectionExecuted(GetSenderItem<PlaylistItem>(sender), HaProtoImpl.InjectionType.INJECT_AND_RETURN);
        }

        private void MenuItem_DeleteItem(object sender, RoutedEventArgs e)
        {
            DeleteItemsExecuted(new List<PlaylistItem> { GetSenderItem<PlaylistItem>(sender) });
        }

        private void MenuItem_ImportPlaylist(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog() { Title = "Select File", Filter = "HaMusic Playlist (*.hmp)|*.hmp" };
            if (ofd.ShowDialog() != true)
                return;

            Playlist pl = (Playlist)((MenuItem)sender).DataContext;
            AddSongs(File.ReadLines(ofd.FileName), GetAfterFromIndex(pl, pl.PlaylistItems.Count), pl.UID);
        }

        private void MenuItem_ExportPlaylist(object sender, RoutedEventArgs e)
        {
            SaveFileDialog sfd = new SaveFileDialog() { Title = "Select File", Filter = "HaMusic Playlist (*.hmp)|*.hmp" };
            if (sfd.ShowDialog() != true)
                return;
            using (StreamWriter sw = new StreamWriter(File.Create(sfd.FileName)))
            {
                foreach (PlaylistItem pi in ((Playlist)((MenuItem)sender).DataContext).PlaylistItems)
                {
                    sw.WriteLine(pi.Item);
                }
            }
        }

        private void MenuItem_NewPlaylist(object sender, RoutedEventArgs e)
        {
            NewPlaylistExecuted();
        }

        public static long GetAfterFromIndex(Playlist pl, int index)
        {
            if (index == 0)
                return -1;
            else
                return pl.PlaylistItems[index - 1].UID;
        }
        
        private void aboutBtn_Click(object sender, RoutedEventArgs e)
        {
            new AboutForm().ShowDialog();
        }

        private void CloseFinder()
        {
            data.FindResult = null;
            data.IsFinding = false;
            SetFocusItem(data.SelectedPlaylistItem);
        }

        private void findBox_KeyDown(object sender, KeyEventArgs e)
        {
            e.Handled = true;
            switch (e.Key)
            {
                case Key.Escape:
                    CloseFinder();
                    break;
                case Key.Enter:
                case Key.Down:
                    FindPlaylistItem(data.FindResult, true);
                    break;
                case Key.Up:
                    FindPlaylistItem(data.FindResult, false);
                    break;
                default:
                    e.Handled = false;
                    break;
            }
        }

        private PlaylistItem FindPlaylistItem(PlaylistItem startFrom, bool searchDown)
        {
            if (!string.IsNullOrWhiteSpace(findBox.Text))
            {
                // Get required info
                Playlist pl = data.SelectedPlaylist;
                string[] terms = findBox.Text.ToLower().Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                // Build search pool
                IEnumerable<PlaylistItem> searchPool = pl.PlaylistItems;
                if (startFrom != null)
                {
                    int index = pl.PlaylistItems.IndexOf(startFrom);
                    if (index >= 0)
                    {
                        // Treat PlaylistItems as circular buffer, and map 1 round of it, with our start in index + 1, or index, depending on search direction
                        int circleFirstIndex = searchDown ?
                            (index + 1) % pl.PlaylistItems.Count : // If searching down, start of list is one after startFrom (circularly, of course)
                            index; // If searching up, start of list is startFrom (since we search from bottom to top)
                        searchPool = pl.PlaylistItems.Skip(circleFirstIndex).Concat(pl.PlaylistItems.Take(circleFirstIndex));
                    }
                }

                // Do search
                foreach (PlaylistItem item in searchDown ? searchPool : searchPool.Reverse())
                {
                    if (item.MatchKeywords(terms))
                    {
                        data.FindResult = item;
                        data.SelectedPlaylistItems.Clear();
                        data.SelectedPlaylistItems.Add(item);
                        SetItemInView(item);
                        return item;
                    }
                }
            }
            data.FindResult = null;
            return null;
        }

        private void findBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            FindPlaylistItem(null, true);
        }

        private void findBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Up:
                case Key.Down:
                    // WPF is shit so it doesn't fire KeyDown on arrow keys
                    // No probs, we'll do it ourselves
                    findBox_KeyDown(sender, e);
                    e.Handled = true;
                    break;
            }
        }
        
        private void closeFindButton_Click(object sender, RoutedEventArgs e)
        {
            CloseFinder();
        }
    }
}
