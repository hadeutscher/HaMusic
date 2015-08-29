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
            openBtn.IsEnabled = b;
            clearBtn.IsEnabled = b;
            ppBtn.IsEnabled = b;
            stopBtn.IsEnabled = b;
            volumeSlider.IsEnabled = b;
            songSlider.IsEnabled = b;
            items.IsEnabled = b;
            nextBtn.IsEnabled = b;
            moveType.IsEnabled = b;
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
                                internalMoveChanging = true;
                                HaProtoImpl.ApplyPacketToDatabase(type, buf, data.ServerDataSource, out foo);
                                internalMoveChanging = false;
                                break;
                            case HaProtoImpl.Opcode.SKIP:
                                // We should not be receiving SKIP packets, the server should translate them to SETSONG
                                throw new NotSupportedException();
                            case HaProtoImpl.Opcode.SETVOL:
                                internalSongChanging = true;
                                data.Volume = HaProtoImpl.SETVOL.Parse(buf).volume;
                                internalSongChanging = false;
                                break;
                            case HaProtoImpl.Opcode.SEEK:
                                HaProtoImpl.SEEK seek = HaProtoImpl.SEEK.Parse(buf);
                                internalSongChanging = true;
                                data.Position = seek.pos;
                                data.Maximum = seek.max;
                                internalSongChanging = false;
                                break;
                            case HaProtoImpl.Opcode.SETPLAYING:
                                data.Playing = HaProtoImpl.SETPLAYING.Parse(buf).playing;
                                break;
                            default:
                                throw new NotSupportedException();
                        }
                    });
                    /*switch (type)
                    {



                        case HaProtoImpl.ServerToClient.PL_INFO:
                            Dispatcher.Invoke(delegate()
                            {
                                lock (data)
                                {
                                    int selectedIndex = items.SelectedIndex;
                                    data.Songs.Clear();
                                    buf.Split("\r\n".ToCharArray(), StringSplitOptions.RemoveEmptyEntries).ToList().ForEach(x => data.Songs.Add(x));
                                    if (index >= items.Items.Count)
                                        index = -1;
                                    else if (index != -1)
                                        data.Songs[index] = "[PLAYING] " + data.Songs[index];
                                    items.SelectedIndex = selectedIndex < items.Items.Count ? selectedIndex : (items.Items.Count - 1);
                                }
                            });
                            break;
                        case HaProtoImpl.ServerToClient.IDX_INFO:
                            Dispatcher.Invoke(delegate()
                            {
                                int selectedIndex = items.SelectedIndex;
                                if (index >= 0 && index < data.Songs.Count)
                                    data.Songs[index] = data.Songs[index].Substring("[PLAYING] ".Length);
                                index = int.Parse(buf);
                                if (index >= 0 && index < data.Songs.Count)
                                    data.Songs[index] = "[PLAYING] " + data.Songs[index];
                                items.SelectedIndex = selectedIndex;
                            });
                            break;
                        case HaProtoImpl.ServerToClient.MEDIA_SEEK_INFO:
                            Dispatcher.Invoke(delegate()
                            {
                                internalSongChanging = true;
                                string[] args = buf.Split(",".ToCharArray());
                                int pos = int.Parse(args[0]), max = int.Parse(args[1]);
                                if (songSlider.Maximum != max)
                                    songSlider.Maximum = max;
                                songSlider.Value = pos;
                                internalSongChanging = false;
                            });
                            break;
                        case HaProtoImpl.ServerToClient.PLAY_PAUSE_INFO:
                            data.Playing = buf == "1";
                            break;
                        case HaProtoImpl.ServerToClient.VOL_INFO:
                            Dispatcher.Invoke(delegate()
                            {
                                internalVolumeChanging = true;
                                volumeSlider.Value = int.Parse(buf);
                                internalVolumeChanging = false;
                            });
                            break;
                        case HaProtoImpl.ServerToClient.MOVE_INFO:
                            Dispatcher.Invoke(delegate ()
                            {
                                internalMoveChanging = true;
                                data.SelectedMove = int.Parse(buf);
                                internalMoveChanging = false;
                            });
                            break;
                    }*/
                }
            }
            catch (Exception e)
            {
                try
                {
                    // Try to close the socket, if it's already closed than w/e
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
                globalSocket.Close();
            }
            catch { }
        }

        private long GetSelectedPlaylist()
        {
            return ((Playlist)items.SelectedContent).UID;
        }

        public void ClearExecuted()
        {
            if (MessageBox.Show("Are you sure?", "Clear", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                HaProtoImpl.Send(globalSocket, HaProtoImpl.Opcode.CLEAR, new HaProtoImpl.CLEAR() { uid = GetSelectedPlaylist() });
        }

        public void PlayPauseExecuted()
        {
            HaProtoImpl.Send(globalSocket, HaProtoImpl.Opcode.SETPLAYING, new HaProtoImpl.SETPLAYING() { playing = data.Playing });
        }

        public void StopExecuted()
        {
            HaProtoImpl.Send(globalSocket, HaProtoImpl.Opcode.SETSONG, new HaProtoImpl.SETSONG() { uid = -1 });
        }

        private bool internalVolumeChanging = false;

        private void volumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (internalVolumeChanging)
                return;
            HaProtoImpl.Send(globalSocket, HaProtoImpl.Opcode.SETVOL, new HaProtoImpl.SETVOL() { volume = data.Volume });
        }

        private bool internalSongChanging = false;
        private void songSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (internalSongChanging)
                return;
            HaProtoImpl.Send(globalSocket, HaProtoImpl.Opcode.SEEK, new HaProtoImpl.SEEK() { pos = data.Position });
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

        private bool internalMoveChanging = false;
        private void moveType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (internalMoveChanging)
                return;
            HaProtoImpl.Send(globalSocket, HaProtoImpl.Opcode.SETMOVE, new HaProtoImpl.SETMOVE() { move = data.ServerDataSource.Mode });
        }
    }
}
