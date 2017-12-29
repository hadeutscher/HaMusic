/* Copyright (C) 2017 Yuval Deutscher

* This Source Code Form is subject to the terms of the Mozilla Public
* License, v. 2.0. If a copy of the MPL was not distributed with this
* file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using LibMPlayerCommon;
using System;
using System.Diagnostics;
using System.IO;

namespace HaMusicServer
{
    class MPlayerImplementation : IMediaPlayerImplementation
    {
        private MPlayer player;
        private int vol = 0;
        private int pos = 0;

        public MPlayerImplementation()
        {
            MplayerBackends backend;
            System.PlatformID runningPlatform = System.Environment.OSVersion.Platform;
            if (runningPlatform == System.PlatformID.Unix)
            {
                backend = MplayerBackends.GL2;
            }
            else if (runningPlatform == PlatformID.MacOSX)
            {
                backend = MplayerBackends.OpenGL;
            }
            else
            {
                backend = MplayerBackends.Direct3D;
            }
            player = new MPlayer(0, backend);
            player.Finalfile += Player_Finalfile;
            player.MplayerError += Player_MplayerError;
            player.CurrentPosition += Player_CurrentPosition;
        }

        private void Player_CurrentPosition(object sender, MplayerEvent e)
        {
            pos = (int)e.Value;
        }

        private void Player_Finalfile(object sender, MplayerEvent e)
        {
            if (e.Message == " 1  ")
            {
                SongEnded?.Invoke(sender, e);
            }
        }

        private void Player_MplayerError(object sender, DataReceivedEventArgs e)
        {
            Program.Logger.Log(e.Data);
        }

        public event EventHandler SongEnded;

        public int GetPos()
        {
            return pos;
        }

        public int PlaySong(string path)
        {
            player.Stop();
            pos = 0;
            if (string.IsNullOrEmpty(path))
                return -1;
            try
            {
                player.Play(path);
                return player.CurrentPlayingFileLength();
            }
            catch (Exception)
            {
                return -1;
            }
        }

        public void SetPlaying(bool playing)
        {
            if (playing && player.CurrentStatus == MediaStatus.Paused)
            {
                player.Pause();
            }
            else if (!playing && player.CurrentStatus == MediaStatus.Playing)
            {
                player.Pause();
            }
        }

        public void SetPos(int time)
        {
            player.Seek(time, Seek.Absolute);
            player.Volume(vol);
        }

        public void SetVolume(int vol)
        {
            this.vol = vol;
            player.Volume(vol);
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    player.Stop();
                }

                disposedValue = true;
            }
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
        }
        #endregion
    }
}
