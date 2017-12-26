/* Copyright (C) 2017 Yuval Deutscher

* This Source Code Form is subject to the terms of the Mozilla Public
* License, v. 2.0. If a copy of the MPL was not distributed with this
* file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using HaMusicLib;
using System;

namespace HaMusicServer
{
    public class MediaPlayer : IDisposable
    {
        private IMediaPlayerImplementation playerImpl;
        private int volume = 0;
        private int length = -1;

        public MediaPlayer(IMediaPlayerImplementation playerImpl, int volume)
        {
            this.playerImpl = playerImpl;
            this.volume = volume;
            this.playerImpl.SongEnded += Player_SongEnded;
        }

        private PlaylistItem AdvancePlaylist()
        {
            PlaylistItem item = Program.Core.DataSource.CurrentItem = Program.Mover.Next();
            SongChanged?.Invoke(this, item);
            return item;
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
                playerImpl.SetVolume(volume);
            }
        }

        public int Position
        {
            get
            {
                return playerImpl.GetPos();
            }
            set
            {
                playerImpl.SetPos(value < 0 ? 0 : value);
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
                playerImpl.SetPlaying(value);
            }
        }

        private string FetchNextSong()
        {
            if (Program.Core.DataSource.CurrentItem == null)
                return null;
            Program.Core.DataSource.CurrentItem.Played = true;
            return Program.Core.DataSource.CurrentItem.Item;
        }

        public void OnSongChanged()
        {
            while (true)
            {
                string path = FetchNextSong();
                length = playerImpl.PlaySong(path);
                if (length != -1 || string.IsNullOrEmpty(path))
                {
                    Program.Mover.ResetErrors();
                    if (length != -1)
                        playerImpl.SetVolume(volume);
                    PlayingChanged(this, length != -1);
                    break;
                }
                Program.Mover.IncreaseErrors();
                AdvancePlaylist();
            }
        }

        public void Dispose()
        {
            playerImpl.Dispose();
        }

        public event EventHandler<PlaylistItem> SongChanged;
        public event EventHandler<bool> PlayingChanged;
    }
}
