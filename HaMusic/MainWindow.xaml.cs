/* Copyright (C) 2015 haha01haha01

* This Source Code Form is subject to the terms of the Mozilla Public
* License, v. 2.0. If a copy of the MPL was not distributed with this
* file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using GongSolutions.Wpf.DragDrop;
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
using System.Windows.Controls.Ribbon;
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
        private Controls data;
        private Thread connThread = null;
        private Object connectLock = new Object();
        private bool internalChanging = false;
        public static string defaultIndexPath = Path.Combine(GetLocalSettingsFolder(), "index.txt");

        public MainWindow()
        {
            HaProtoImpl.Entity = HaProtoImpl.HaMusicEntity.Client;
            InitializeComponent();
            data = new Controls(this);
            DataContext = data;
            SetEnabled(false);
            TryReloadMediaIndex();
        }

        private void TryReloadMediaIndex()
        {
            if (File.Exists(defaultIndexPath))
            {
                mediaBrowser.SourceData = File.ReadAllLines(defaultIndexPath).ToList();
            }
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

        /// <summary>
        /// Finds a Child of a given item in the visual tree. 
        /// </summary>
        /// <param name="parent">A direct parent of the queried item.</param>
        /// <typeparam name="T">The type of the queried item.</typeparam>
        /// <param name="childName">x:Name or Name of child. </param>
        /// <returns>The first parent item that matches the submitted type parameter. 
        /// If not matching item can be found, 
        /// a null parent is being returned.</returns>
        public static T FindChild<T>(DependencyObject parent, string childName=null, object dataContext = null)
           where T : DependencyObject
        {
            // Confirm parent and childName are valid. 
            if (parent == null) return null;

            T foundChild = null;

            int childrenCount = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < childrenCount; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                // If the child is not of the request child type child
                T childType = child as T;
                if (childType == null)
                {
                    // recursively drill down the tree
                    foundChild = FindChild<T>(child, childName, dataContext);

                    // If the child is found, break so we do not overwrite the found child. 
                    if (foundChild != null) break;
                }
                else if (!string.IsNullOrEmpty(childName))
                {
                    var frameworkElement = child as FrameworkElement;
                    // If the child's name is set for search
                    if (frameworkElement != null && frameworkElement.Name == childName)
                    {
                        // if the child's name is of the request name
                        foundChild = (T)child;
                        break;
                    }
                }
                else if (dataContext != null)
                {
                    var frameworkElement = child as FrameworkElement;
                    if (frameworkElement != null && frameworkElement.DataContext == dataContext)
                    {
                        foundChild = (T)child;
                        break;
                    }
                }
                else
                {
                    // child element found.
                    foundChild = (T)child;
                    break;
                }
            }

            return foundChild;
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
                                    // Why would anyone try to get the client's DB?
                                    throw new NotSupportedException();
                                case HaProtoImpl.Opcode.SETDB:
                                    HaProtoImpl.SETDB setdb = HaProtoImpl.SETDB.Parse(buf);
                                    data.ServerDataSource = setdb.dataSource;
                                    if (data.ServerDataSource.CurrentItem != null)
                                    {
                                        data.SelectedPlaylist = data.ServerDataSource.GetPlaylistForItem(data.ServerDataSource.CurrentItem.UID);
                                    }
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

        public void OpenExecuted()
        {
            OpenFileDialog ofd = new OpenFileDialog() { Filter = "All Files|*.*", Multiselect = true };
            if (ofd.ShowDialog() != true)
                return;
            AddSongs(ofd.FileNames);
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

        public void IndexerSettingsExecuted()
        {
            IndexerSettings isWnd = new IndexerSettings();
            if (isWnd.ShowDialog() == true)
            {
                TryReloadMediaIndex();
            }
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

        public void SelectItemExecuted(ListView lv)
        {
            HaProtoImpl.Send(globalSocket, HaProtoImpl.Opcode.SETSONG, new HaProtoImpl.SETSONG() { uid = ((PlaylistItem)lv.SelectedValue).UID });
        }

        private Dictionary<PlaylistItem, int> GetPlaylistIndices(Playlist pl, List<PlaylistItem> plItems)
        {
            Dictionary<PlaylistItem, int> result = new Dictionary<PlaylistItem, int>();
            int i = 0;
            foreach (PlaylistItem item in pl.PlaylistItems)
            {
                result.Add(item, i++);
            }
            return result;
        }

        public void DragMoveItems(IDropInfo dropInfo)
        {
            List<long> items;
            if (dropInfo.Data is PlaylistItem)
                items = new List<long> { ((PlaylistItem)dropInfo.Data).UID };
            else
            {
                List<PlaylistItem> plItems = ((IEnumerable<PlaylistItem>)dropInfo.Data).ToList();
                Dictionary<PlaylistItem, int> indices = GetPlaylistIndices(data.SelectedPlaylist, plItems);
                plItems.Sort((x, y) => indices[x].CompareTo(indices[y]));
                items = plItems.Select(x => x.UID).ToList();
            }
            long after;
            if ((dropInfo.InsertPosition & RelativeInsertPosition.AfterTargetItem) != 0)
            {
                after = ((PlaylistItem)dropInfo.TargetItem).UID;
            }
            else if ((dropInfo.InsertPosition & RelativeInsertPosition.BeforeTargetItem) != 0)
            {
                int index = data.SelectedPlaylist.PlaylistItems.IndexOf((PlaylistItem)dropInfo.TargetItem);
                after = index == 0 ? -1 : data.SelectedPlaylist.PlaylistItems[index - 1].UID;
            }
            else
            {
                return;
            }
            HaProtoImpl.Send(globalSocket, HaProtoImpl.Opcode.REORDER, new HaProtoImpl.REORDER() { pid = data.SelectedPlaylist.UID, after = after, items = items });
        }

        private void items_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            SelectItemExecuted((ListView)sender);
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
        
        public void AddSongs(IEnumerable<string> paths, long playlist = -1)
        {
            if (playlist == -1)
                playlist = data.SelectedPlaylist.UID;
            HaProtoImpl.Send(globalSocket, HaProtoImpl.Opcode.ADD, new HaProtoImpl.ADD() { uid = playlist, paths = paths.ToList() });
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

            AddSongs(File.ReadLines(ofd.FileName), ((Playlist)((MenuItem)sender).DataContext).UID);
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

        private void mediaBrowser_ItemDoubleClicked(string item)
        {
            AddSongs(new List<string> { item });
        }
    }
}
