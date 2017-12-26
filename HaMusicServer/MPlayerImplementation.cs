/* Copyright (C) 2017 Yuval Deutscher

* This Source Code Form is subject to the terms of the Mozilla Public
* License, v. 2.0. If a copy of the MPL was not distributed with this
* file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using LibMPlayerCommon;
using System;

namespace HaMusicServer
{
    class MPlayerImplementation : IMediaPlayerImplementation
    {
        private MPlayer player;
        private bool playing = false;

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
            player.VideoExited += Player_VideoExited;
        }

        private void Player_VideoExited(object sender, MplayerEvent e)
        {
            SongEnded?.Invoke(sender, e);
        }

        public event EventHandler SongEnded;

        public int GetPos()
        {
            return (int)player.GetCurrentPosition();
        }

        public int PlaySong(string path)
        {
            player.Play(path);
            return player.CurrentPlayingFileLength();
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
            player.Seek(time, Seek.Relative);
        }

        public void SetVolume(int vol)
        {
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
