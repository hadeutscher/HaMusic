/* Copyright (C) 2015 haha01haha01

* This Source Code Form is subject to the terms of the Mozilla Public
* License, v. 2.0. If a copy of the MPL was not distributed with this
* file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using HaMusicLib;
using System;

namespace HaMusicServer
{
    public class HaMusicPlayerManager : IDisposable
    {
        private IHaMusicPlayer player;
        private MainForm mainForm;
        private int volume = 0;
        private int length = -1;

        public HaMusicPlayerManager(IHaMusicPlayer player, MainForm mainForm, int volume)
        {
            this.player = player;
            this.mainForm = mainForm;
            this.volume = volume;
            this.player.SongEnded += Player_SongEnded;
        }

        private PlaylistItem AdvancePlaylist()
        {
            lock (mainForm.DataSource.Lock)
            {
                PlaylistItem item = mainForm.DataSource.CurrentItem = mainForm.Mover.Next();
                SongChanged?.Invoke(this, item);
                return item;
            }
        }

        private void Player_SongEnded(object sender, EventArgs e)
        {
            AdvancePlaylist();
            OnSongChanged();
        }

        public int Volume
        {
            get { return volume; }
            set
            {
                volume = value;
                player.SetVolume(volume);
            }
        }

        public int Position
        {
            get
            {
                return player.GetPos();
            }
            set
            {
                player.SetPos(value < 0 ? 0 : value);
            }
        }

        public int Maximum
        {
            get { return length; }
        }

        public bool Playing
        {
            set
            {
                player.SetPlaying(value);
            }
        }

        private string FetchNextSong()
        {
            lock (mainForm.DataSource.Lock)
            {
                if (mainForm.DataSource.CurrentItem == null)
                    return null;
                mainForm.DataSource.CurrentItem.Played = true;
                return mainForm.DataSource.CurrentItem.Item;
            }
        }

        public void OnSongChanged()
        {
            while (true)
            {
                string path = FetchNextSong();
                length = player.PlaySong(path);
                if (length != -1 || string.IsNullOrEmpty(path))
                {
                    mainForm.Mover.ResetErrors();
                    if (length != -1)
                        player.SetVolume(volume);
                    PlayingChanged(this, length != -1);
                    break;
                }
                lock (mainForm.DataSource.Lock)
                {
                    mainForm.Mover.IncreaseErrors();
                    AdvancePlaylist();
                }
            }
        }

        public void Dispose()
        {
            player.Dispose();
        }

        public event EventHandler<PlaylistItem> SongChanged;
        public event EventHandler<bool> PlayingChanged;
    }
}
