/* Copyright (C) 2015 haha01haha01

* This Source Code Form is subject to the terms of the Mozilla Public
* License, v. 2.0. If a copy of the MPL was not distributed with this
* file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using HaMusicLib;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
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

        public MainWindow()
        {
            HaProtoImpl.Entity = HaProtoImpl.HaMusicEntity.Client;
            InitializeComponent();
            data = new Controls(this);
            DataContext = data;
            SetEnabled(false);
        }

        private void SetEnabled(bool b)
        {
            data.Enabled = b;
        }

        public void OpenExecuted()
        {
            OpenFileDialog ofd = new OpenFileDialog() { Filter = "All Files|*.*", Multiselect = true };
            if (ofd.ShowDialog() != true)
                return;
            addSongs(ofd.FileNames);
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

        public void ConnectExecuted()
        {
            AddressSelector selector = new AddressSelector();
            if (selector.ShowDialog() != true)
                return;
            new Thread(new ThreadStart(delegate() { ConnectThreadProc(selector.Result); })).Start();
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
                Dispatcher.Invoke(delegate()
                {
                    SetEnabled(false);
                });
            }
        }

        private void items_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            lock (data)
            {
                HaProtoImpl.Send(globalSocket, HaProtoImpl.Opcode.SETSONG, new HaProtoImpl.SETSONG() { uid = ((PlaylistItem)((TextBlock)e.OriginalSource).DataContext).UID });
            }
        }

        private void items_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                /*case Key.Delete:
                    lock (data)
                    {
                        List<long> uids = new List<long>();
                        foreach (object item in items.SelectedContent)
                            uids.Add(((PlaylistItem)item).UID);
                        HaProtoImpl.Send(globalSocket, HaProtoImpl.Opcode.REMOVE, new HaProtoImpl.REMOVE() { uid = 0, items = uids });
                    }
                    break;*/
                /*case Key.OemPlus:
                    lock (data)
                    {
                        if (items.SelectedIndex == -1 || items.SelectedIndex == items.Items.Count - 1)
                            return;
                        HaProtoImpl.C2SSend(globalSocket, items.SelectedIndex++.ToString(), HaProtoImpl.ClientToServer.DOWN);
                    }
                    break;
                case Key.OemMinus:
                    lock (data)
                    {
                        if (items.SelectedIndex == -1 || items.SelectedIndex == 0)
                            return;
                        HaProtoImpl.C2SSend(globalSocket, items.SelectedIndex--.ToString(), HaProtoImpl.ClientToServer.UP);
                    }
                    break;*/
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

        public void ClearExecuted()
        {
            if (MessageBox.Show("Are you sure?", "Clear", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                HaProtoImpl.Send(globalSocket, HaProtoImpl.Opcode.CLEAR, new HaProtoImpl.CLEAR() { uid = GetSelectedPlaylist() });
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
            Playlist pl = (Playlist)((Control)e.OriginalSource).DataContext;
            HaProtoImpl.Send(globalSocket, HaProtoImpl.Opcode.RENPL, new HaProtoImpl.RENPL() { uid = pl.UID, name = pl.Name });
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            Playlist pl = (Playlist)((Control)sender).DataContext;
            HaProtoImpl.Send(globalSocket, HaProtoImpl.Opcode.DELPL, new HaProtoImpl.DELPL() { uid = pl.UID });
        }
    }
}
