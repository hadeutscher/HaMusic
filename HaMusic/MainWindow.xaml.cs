/* Copyright (C) 2015 haha01haha01

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
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;

namespace HaMusic
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private const int EyeCandyDisableThreshold = 1000;
        private Socket globalSocket = null;
        private Data data;
        private Thread connThread = null;
        private Object connectLock = new Object();
        private bool internalChanging = false;

        public static string defaultIndexPath = Path.Combine(GetLocalSettingsFolder(), "index.txt");
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

        private void ConnectThreadProc(string address)
        {
            // Lock to make sure we don't connect twice simultaneously
            lock (connectLock)
            {
                try
                {
                    // Try to close globalSocket, if it doesn't exist or was already closed it's w/e
                    globalSocket.Close();
                }
                catch { }
                if (connThread != null)
                {
                    // If we have a previous thread, make sure it died already
                    connThread.Join();
                }
                try
                {
                    // Create a socket and a handler thread
                    globalSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    globalSocket.Connect(address, 5151);
                    connThread = new Thread(new ThreadStart(delegate() { SockProc(globalSocket); }));
                    connThread.Start();
                    Dispatcher.Invoke(delegate() { SetEnabled(true); });
                }
                catch (Exception)
                {
                    MessageBox.Show("Could not connect", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
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
                    data.ItemInView = null;
                    data.ItemInView = data.ServerDataSource.CurrentItem;
                }
                catch (Exception e)
                {
                    MessageBox.Show(string.Format("Could not bring item to view, error: {0}", e.Message), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        
        private void SockProc(Socket sock)
        {
            try
            {
                HaProtoImpl.Send(sock, HaProtoImpl.Opcode.GETDB, new HaProtoImpl.GETDB());
                while (true)
                {
                    byte[] buf;
                    HaProtoImpl.Opcode type = HaProtoImpl.Receive(sock, out buf);
                    bool foo;
                    Dispatcher.Invoke(delegate ()
                    {
                        try
                        {
                            internalChanging = true;
                            switch (type)
                            {
                                case HaProtoImpl.Opcode.GETDB:
                                    throw new NotSupportedException();
                                case HaProtoImpl.Opcode.SETDB:
                                    HaProtoImpl.SETDB setdb = HaProtoImpl.SETDB.Parse(buf);
                                    data.ServerDataSource = setdb.dataSource;
                                    BringSelectedItemIntoView();
                                    break;
                                case HaProtoImpl.Opcode.LIBRARY_ADD:
                                case HaProtoImpl.Opcode.LIBRARY_REMOVE:
                                case HaProtoImpl.Opcode.LIBRARY_RESET:
                                    // No selection eye candy for library because it's not worth the time it will take to implement
                                    HaProtoImpl.ApplyPacketToDatabase(type, buf, data.ServerDataSource, out foo);

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

                                    HaProtoImpl.ApplyPacketToDatabase(type, buf, data.ServerDataSource, out foo);

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
                                            data.FocusedItem = null;
                                            data.FocusedItem = focusItem;
                                        }
                                    }
                                    break;
                                case HaProtoImpl.Opcode.SETMOVE:
                                    HaProtoImpl.ApplyPacketToDatabase(type, buf, data.ServerDataSource, out foo);
                                    break;
                                case HaProtoImpl.Opcode.SKIP:
                                    // We should not be receiving SKIP packets, the server should translate them to SETSONG
                                    throw new NotSupportedException();
                                case HaProtoImpl.Opcode.SETVOL:
                                    data.ServerDataSource.Volume = HaProtoImpl.SETVOL.Parse(buf).volume;
                                    break;
                                case HaProtoImpl.Opcode.SEEK:
                                    HaProtoImpl.SEEK seek = HaProtoImpl.SEEK.Parse(buf);
                                    data.ServerDataSource.Position = seek.pos;
                                    data.ServerDataSource.Maximum = seek.max;
                                    break;
                                case HaProtoImpl.Opcode.SETPLAYING:
                                    data.ServerDataSource.Playing = HaProtoImpl.SETPLAYING.Parse(buf).playing;
                                    break;
                                default:
                                    throw new NotSupportedException();
                            }
                        }
                        finally
                        {
                            internalChanging = false;
                        }
                    });
                }
            }
            catch (Exception e)
            {
                try
                {
                    // Try to close the socket, if it's already closed than w/e
                    sock.Shutdown(SocketShutdown.Both);
                    sock.Close();
                }
                catch { }
                try
                {
                    Dispatcher.Invoke(delegate ()
                    {
                        SetEnabled(false);
                    });
                }
                catch { }
            }
        }

        public void ConnectExecuted()
        {
            AddressSelector selector = new AddressSelector();
            if (selector.ShowDialog() != true)
                return;
            new Thread(new ThreadStart(delegate () { ConnectThreadProc(selector.Result); })).Start();
        }

        public void NewPlaylistExecuted()
        {
            HaProtoImpl.Send(globalSocket, HaProtoImpl.Opcode.ADDPL, new HaProtoImpl.ADDPL());
        }

        public void NextExecuted()
        {
            HaProtoImpl.Send(globalSocket, HaProtoImpl.Opcode.SKIP, new HaProtoImpl.SKIP());
        }

        public void DeletePlaylistExecuted()
        {
            HaProtoImpl.Send(globalSocket, HaProtoImpl.Opcode.DELPL, new HaProtoImpl.DELPL() { uid = data.SelectedPlaylist.UID });
        }

        public void DeleteItemsExecuted(ListView lv)
        {
            Playlist pl = (Playlist)lv.DataContext;
            List<long> uids = new List<long>();
            foreach (object item in lv.SelectedItems)
                uids.Add(((PlaylistItem)item).UID);
            if (uids.Count > 0)
                HaProtoImpl.Send(globalSocket, HaProtoImpl.Opcode.REMOVE, new HaProtoImpl.REMOVE() { uid = pl.UID, items = uids });
        }

        public void SelectItemExecuted(PlaylistItem item)
        {
            HaProtoImpl.Send(globalSocket, HaProtoImpl.Opcode.SETSONG, new HaProtoImpl.SETSONG() { uid = item.UID });
        }

        public void DragMoveItems(IEnumerable<PlaylistItem> items, long after)
        {
            HaProtoImpl.Send(globalSocket, HaProtoImpl.Opcode.REORDER, new HaProtoImpl.REORDER() { pid = data.SelectedPlaylist.UID, after = after, items = items.Select(x => x.UID).ToList() });
        }

        private void items_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            PlaylistItem item = ((FrameworkElement)e.OriginalSource).DataContext as PlaylistItem;
            if (item != null)
            {
                SelectItemExecuted(item);
            }
            e.Handled = true;
        }

        private void items_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Delete:
                    DeleteItemsExecuted((ListView)sender);
                    e.Handled = true;
                    break;
            }
        }

        private void RibbonWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            try
            {
                globalSocket.Shutdown(SocketShutdown.Both);
                globalSocket.Close();
            }
            catch { }
        }

        private long GetSelectedPlaylist()
        {
            return data.SelectedPlaylist.UID;
        }

        public void PlayPauseExecuted()
        {
            if (!data.ServerDataSource.Playing && data.ServerDataSource.CurrentItem == null && data.SelectedPlaylistItem != null)
            {
                HaProtoImpl.Send(globalSocket, HaProtoImpl.Opcode.SETSONG, new HaProtoImpl.SETSONG() { uid = data.SelectedPlaylistItem.UID });
            }
            else if (!data.ServerDataSource.Playing && data.ServerDataSource.CurrentItem == null && mediaBrowser.listView.SelectedItem is PlaylistItem)
            {
                HaProtoImpl.Send(globalSocket, HaProtoImpl.Opcode.SETSONG, new HaProtoImpl.SETSONG() { uid = ((PlaylistItem)mediaBrowser.listView.SelectedItem).UID });
            }
            else if (data.ServerDataSource.Playing || data.ServerDataSource.CurrentItem != null)
            {
                HaProtoImpl.Send(globalSocket, HaProtoImpl.Opcode.SETPLAYING, new HaProtoImpl.SETPLAYING() { playing = !data.ServerDataSource.Playing });
            }
        }

        public void StopExecuted()
        {
            HaProtoImpl.Send(globalSocket, HaProtoImpl.Opcode.SETSONG, new HaProtoImpl.SETSONG() { uid = -1 });
        }


        private void volumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (internalChanging)
                return;
            HaProtoImpl.Send(globalSocket, HaProtoImpl.Opcode.SETVOL, new HaProtoImpl.SETVOL() { volume = data.ServerDataSource.Volume });
        }

        private void songSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (internalChanging)
                return;
            HaProtoImpl.Send(globalSocket, HaProtoImpl.Opcode.SEEK, new HaProtoImpl.SEEK() { pos = data.ServerDataSource.Position });
        }
        
        public void AddSongs(IEnumerable<string> paths, long after, long playlist = -1)
        {
            if (playlist == -1)
                playlist = data.SelectedPlaylist.UID;
            HaProtoImpl.Send(globalSocket, HaProtoImpl.Opcode.ADD, new HaProtoImpl.ADD() { uid = playlist, paths = paths.ToList(), after = after });
        }

        private void RibbonWindow_Loaded(object sender, RoutedEventArgs e)
        {
            ConnectExecuted();
        }

        private void moveType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (internalChanging)
                return;
            HaProtoImpl.Send(globalSocket, HaProtoImpl.Opcode.SETMOVE, new HaProtoImpl.SETMOVE() { move = data.ServerDataSource.Mode });
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            Playlist pl = (Playlist)((Control)sender).DataContext;
            HaProtoImpl.Send(globalSocket, HaProtoImpl.Opcode.RENPL, new HaProtoImpl.RENPL() { uid = pl.UID, name = pl.Name });
        }

        private void MenuItem_DeletePlaylist(object sender, RoutedEventArgs e)
        {
            Playlist pl = (Playlist)((Control)sender).DataContext;
            HaProtoImpl.Send(globalSocket, HaProtoImpl.Opcode.DELPL, new HaProtoImpl.DELPL() { uid = pl.UID });
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
                HaProtoImpl.Send(globalSocket, HaProtoImpl.Opcode.CLEAR, new HaProtoImpl.CLEAR() { uid = ((Playlist)((MenuItem)sender).DataContext).UID });
            }
        }

        private void MenuItem_PlayItemNext(object sender, RoutedEventArgs e)
        {
            HaProtoImpl.Send(globalSocket, HaProtoImpl.Opcode.INJECT, new HaProtoImpl.INJECT() { uid = ((PlaylistItem)((MenuItem)sender).DataContext).UID });
        }

        private void MenuItem_DeleteItem(object sender, RoutedEventArgs e)
        {

            HaProtoImpl.Send(globalSocket, HaProtoImpl.Opcode.REMOVE, new HaProtoImpl.REMOVE() { uid = GetSelectedPlaylist(), items = new List<long> { ((PlaylistItem)((MenuItem)sender).DataContext).UID } });
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
    }
}
