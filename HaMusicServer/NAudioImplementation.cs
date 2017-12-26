/* Copyright (C) 2017 Yuval Deutscher

* This Source Code Form is subject to the terms of the Mozilla Public
* License, v. 2.0. If a copy of the MPL was not distributed with this
* file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using NAudio.Wave;
using System;
using System.IO;

namespace HaMusicServer
{
    public class NAudioImplementation : IMediaPlayerImplementation
    {
        private IWavePlayer player = null;
        private WaveStream stream = null;

        public event EventHandler SongEnded;

        private void MakePlayerInternal()
        {
            player = new WaveOutEvent();
            player.PlaybackStopped += player_PlaybackStopped;
        }

        void player_PlaybackStopped(object sender, StoppedEventArgs e)
        {
            SongEnded?.Invoke(this, new EventArgs());
        }

        private void SetVolumeInternal(int vol)
        {
            try
            {
                player.Volume = vol / 100f;
            }
            catch (Exception)
            {
            }
        }

        public void SetVolume(int vol)
        {
            if (player != null)
            {
                SetVolumeInternal(vol);
            }
            else
            {
                MakePlayerInternal();
                SetVolumeInternal(vol);
                CleanPlayerAndStream();
            }
        }

        public void SetPos(int time)
        {
            if (stream == null)
                return;
            stream.Seek(time * stream.WaveFormat.AverageBytesPerSecond, SeekOrigin.Begin);
        }

        public int GetPos()
        {
            if (player == null || player.PlaybackState == PlaybackState.Stopped || stream == null)
            {
                return -1;
            }
            else
            {
                return (int)(stream.Position / stream.WaveFormat.AverageBytesPerSecond);
            }
        }

        public void SetPlaying(bool playing)
        {
            if (player == null)
                return;
            if (!playing)
                player.Pause();
            else
                player.Play();
        }

        private WaveStream CreateStream(string path)
        {
            return new MediaFoundationReader(path);
        }

        private void CleanPlayerAndStream()
        {
            if (player != null)
            {
                player.PlaybackStopped -= player_PlaybackStopped;
                player.Stop();
                player.Dispose();
                player = null;
            }
            if (stream != null)
            {
                stream.Dispose();
                stream = null;
            }
        }

        public int PlaySong(string path)
        {
            CleanPlayerAndStream();
            if (!string.IsNullOrEmpty(path))
            {
                try
                {
                    MakePlayerInternal();
                    stream = CreateStream(path);
                    player.Init(stream);
                    player.Play();
                    return (int)(stream.Length / stream.WaveFormat.AverageBytesPerSecond);
                }
                catch (Exception)
                {
                    CleanPlayerAndStream();
                }
            }
            return -1;
        }

        public void Dispose()
        {
            CleanPlayerAndStream();
        }
    }

}
