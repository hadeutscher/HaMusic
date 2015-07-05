/* Copyright (C) 2015 haha01haha01

* This Source Code Form is subject to the terms of the Mozilla Public
* License, v. 2.0. If a copy of the MPL was not distributed with this
* file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace HaMusicServer
{
    public class NAudioPlayer : IHaMusicPlayer
    {
        private IWavePlayer player;
        private WaveStream stream;
        private string currPath = "";
        private bool internalStop = false;
        private MainForm mf;
        private int vol;

        public NAudioPlayer(MainForm mf, int vol)
        {
            this.vol = vol;
            this.player = null;
            this.stream = null;
            this.mf = mf;
        }

        private void MakePlayerInternal()
        {
            player = new WaveOutEvent();
            player.PlaybackStopped += player_PlaybackStopped;
            SetVolumeInternal(vol);
        }

        void player_PlaybackStopped(object sender, StoppedEventArgs e)
        {
            if (internalStop)
                return;
            lock (mf.playlist)
                mf.Index++;
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
            this.vol = vol;
            mf.Invoke((Action)delegate
            {
                if (player != null)
                {
                    SetVolumeInternal(vol);
                }
            });
        }

        public int GetVolume()
        {
            return vol;
        }

        public void Seek(int time)
        {
            mf.Invoke((Action)delegate
            {
                if (stream == null)
                    return;
                stream.Seek(time * stream.WaveFormat.AverageBytesPerSecond, SeekOrigin.Begin);
            });
        }

        public Tuple<int, int> GetPos()
        {
            return (Tuple<int, int>)mf.Invoke((Func<Tuple<int, int>>)delegate
            {
                int pos, max;
                if (player == null || player.PlaybackState == PlaybackState.Stopped || stream == null)
                {
                    pos = max = -1;
                }
                else
                {
                    pos = (int)(stream.Position / stream.WaveFormat.AverageBytesPerSecond);
                    max = (int)(stream.Length / stream.WaveFormat.AverageBytesPerSecond);
                }
                return new Tuple<int, int>(pos, max);
            });
        }

        public void SetPlaying(bool playing)
        {
            mf.Invoke((Action)delegate
            {
                if (player == null)
                    return;
                if (!playing)
                    player.Pause();
                else
                    player.Play();
            });
        }

        private bool IsPlayingInternal()
        {
            return player != null && player.PlaybackState == PlaybackState.Playing;
        }

        public bool IsPlaying()
        {
            return (bool)mf.Invoke((Func<bool>)delegate
            {
                return IsPlayingInternal();
            });
        }

        private WaveStream CreateStream(string path)
        {
            return new MediaFoundationReader(path);
        }

        private void CleanPlayerAndStream(bool full)
        {
            if (player != null)
            {
                if (full)
                {
                    player.PlaybackStopped -= player_PlaybackStopped;
                    player.Stop();
                }
                player.Dispose();
                player = null;
            }
            if (stream != null)
            {
                stream.Dispose();
                stream = null;
            }
        }

        public void OnIndexChanged()
        {
            string path = "";
            bool advance_idx = false;
            lock (mf.playlist)
            {
                if (mf.Index < mf.playlist.Count && mf.Index >= 0)
                {
                    path = mf.playlist[mf.Index];
                }
            }
            mf.Invoke((Action)delegate
            {
                if (path != currPath)
                {
                    CleanPlayerAndStream(true);
                    if (path != "")
                    {
                        try
                        {
                            MakePlayerInternal();
                            stream = CreateStream(path);
                            player.Init(stream);
                            player.Play();
                        }
                        catch (Exception)
                        {
                            CleanPlayerAndStream(false);
                            advance_idx = true;
                        }
                    }
                }
                currPath = path;
                UpdatePausePlay();
            });
            if (advance_idx)
            {
                lock (mf.playlist)
                {
                    mf.SetIndexInternal(mf.Index + 1);
                }
                this.OnIndexChanged();
            }
        }

        public event EventHandler<bool> PausePlayChanged;

        private void UpdatePausePlay()
        {
            if (PausePlayChanged != null)
                PausePlayChanged.Invoke(this, IsPlayingInternal());
        }
    }
}
