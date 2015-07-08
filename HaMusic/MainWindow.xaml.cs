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
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Ribbon;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

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
        private int index = -1;
        private Object connectLock = new Object();

        public MainWindow()
        {
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
        }

        public void OpenExecuted()
        {
            OpenFileDialog ofd = new OpenFileDialog() { Filter = "All Files|*.*", Multiselect = true };
            if (ofd.ShowDialog() != true)
                return;
            ofd.FileNames.ToList().ForEach(x => HaProtoImpl.C2SSend(globalSocket, x, HaProtoImpl.ClientToServer.ADD));
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

        private void SockProc(Socket sock)
        {
            try
            {
                HaProtoImpl.C2SSend(sock, "", HaProtoImpl.ClientToServer.GETPL);
                HaProtoImpl.C2SSend(sock, "", HaProtoImpl.ClientToServer.GETIDX);
                HaProtoImpl.C2SSend(sock, "", HaProtoImpl.ClientToServer.GETVOL);
                HaProtoImpl.C2SSend(sock, "", HaProtoImpl.ClientToServer.GETSTATE);
                while (true)
                {
                    string buf;
                    HaProtoImpl.ServerToClient type = HaProtoImpl.S2CReceive(sock, out buf);
                    switch (type)
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
                    }
                }
            }
            catch (Exception)
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
            HaProtoImpl.C2SSend(globalSocket, items.SelectedIndex.ToString(), HaProtoImpl.ClientToServer.SETIDX);
        }

        private void items_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Delete:
                    lock (data)
                    {
                        List<int> indexes = new List<int>();
                        foreach (object item in items.SelectedItems)
                            indexes.Add(items.Items.IndexOf(items));
                        indexes.Sort();
                        indexes.Reverse();
                        foreach (int index in indexes)
                            HaProtoImpl.C2SSend(globalSocket, index.ToString(), HaProtoImpl.ClientToServer.REMOVE);
                    }
                    break;
                case Key.OemPlus:
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
                    break;
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

        public void ClearExecuted()
        {
            HaProtoImpl.C2SSend(globalSocket, "", HaProtoImpl.ClientToServer.CLEAR);
        }

        public void PlayPauseExecuted()
        {
            HaProtoImpl.C2SSend(globalSocket, "", data.Playing ? HaProtoImpl.ClientToServer.PAUSE : HaProtoImpl.ClientToServer.PLAY);
        }

        public void StopExecuted()
        {
            HaProtoImpl.C2SSend(globalSocket, "-1", HaProtoImpl.ClientToServer.SETIDX);
        }

        private bool internalVolumeChanging = false;

        private void volumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (internalVolumeChanging)
                return;
            HaProtoImpl.C2SSend(globalSocket, ((int)volumeSlider.Value).ToString(), HaProtoImpl.ClientToServer.SETVOL);
        }

        private bool internalSongChanging = false;
        private void songSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (internalSongChanging)
                return;
            HaProtoImpl.C2SSend(globalSocket, ((int)songSlider.Value).ToString(), HaProtoImpl.ClientToServer.SEEK);
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
            files.ToList().ForEach(x => HaProtoImpl.C2SSend(globalSocket, x, HaProtoImpl.ClientToServer.ADD));
        }

        private void RibbonWindow_Loaded(object sender, RoutedEventArgs e)
        {
            ConnectExecuted();
        }
    }
}
