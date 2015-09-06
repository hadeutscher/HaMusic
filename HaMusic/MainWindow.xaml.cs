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
using System.Windows.Controls.Ribbon;
using System.Windows.Input;

namespace HaMusic
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : RibbonWindow
    {
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
                                    HaProtoImpl.ApplyPacketToDatabase(type, buf, data.ServerDataSource, out foo);
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
            addSongs(ofd.FileNames);
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
            HaProtoImpl.Send(globalSocket, HaProtoImpl.Opcode.REMOVE, new HaProtoImpl.REMOVE() { uid = pl.UID, items = uids });
        }

        public void SelectItemExecuted(ListView lv)
        {
            HaProtoImpl.Send(globalSocket, HaProtoImpl.Opcode.SETSONG, new HaProtoImpl.SETSONG() { uid = ((PlaylistItem)lv.SelectedValue).UID });
        }

        private void items_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            SelectItemExecuted((ListView)sender);
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
            HaProtoImpl.Send(globalSocket, HaProtoImpl.Opcode.SETPLAYING, new HaProtoImpl.SETPLAYING() { playing = data.ServerDataSource.Playing = !data.ServerDataSource.Playing });
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

        private void items_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effects = DragDropEffects.Copy;
            else
                e.Effects = DragDropEffects.None;
        }

        private void items_Drop(object sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            addSongs(files);
        }

        public void addSongs(IEnumerable<string> paths)
        {
            HaProtoImpl.Send(globalSocket, HaProtoImpl.Opcode.ADD, new HaProtoImpl.ADD() { uid = GetSelectedPlaylist(), paths = paths.ToList() });
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
            DeleteItemsExecuted((ListView)sender);
        }
    }
}
