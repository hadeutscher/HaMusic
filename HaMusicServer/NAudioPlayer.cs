/* Copyright (C) 2015 haha01haha01

* This Source Code Form is subject to the terms of the Mozilla Public
* License, v. 2.0. If a copy of the MPL was not distributed with this
* file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using NAudio.Wave;
using System;
using System.IO;

namespace HaMusicServer
{
    public class NAudioPlayer : IHaMusicPlayer
    {
        private IWavePlayer player;
        private WaveStream stream;
        private MainForm mainForm;
        private int vol;

        public NAudioPlayer(MainForm mainForm, int vol)
        {
            this.vol = vol;
            this.player = null;
            this.stream = null;
            this.mainForm = mainForm;
        }

        private void MakePlayerInternal()
        {
            player = new WaveOutEvent();
            player.PlaybackStopped += player_PlaybackStopped;
            SetVolumeInternal(vol);
        }

        void player_PlaybackStopped(object sender, StoppedEventArgs e)
        {
            lock (mainForm.DataSource.Lock)
            {
                mainForm.DataSource.CurrentItem = mainForm.Mover.Next();
            }
            mainForm.AnnounceIndexChange();
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
            mainForm.InvokeIfRequired((Action)delegate
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
            mainForm.InvokeIfRequired((Action)delegate
            {
                if (stream == null)
                    return;
                stream.Seek(time * stream.WaveFormat.AverageBytesPerSecond, SeekOrigin.Begin);
            });
        }

        public Tuple<int, int> GetPos()
        {
            return (Tuple<int, int>)mainForm.InvokeIfRequired((Func<Tuple<int, int>>)delegate
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
            mainForm.InvokeIfRequired((Action)delegate
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
            return (bool)mainForm.Invoke((Func<bool>)delegate
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
            lock (mainForm.DataSource.Lock)
            {
                if (mainForm.DataSource.CurrentItem != null)
                {
                    mainForm.DataSource.CurrentItem.Played = true;
                    path = mainForm.DataSource.CurrentItem.Item;
                }
            }
            mainForm.InvokeIfRequired((Action)delegate
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
                UpdatePausePlay();
            });
            if (advance_idx)
            {
                lock (mainForm.DataSource.Lock)
                {
                    mainForm.Mover.IncreaseErrors();
                    mainForm.DataSource.CurrentItem = mainForm.Mover.Next();
                }
                this.OnIndexChanged(); // Try again with next item
            }
            else
            {
                mainForm.Mover.ResetErrors();
            }
        }

        public event EventHandler<bool> PausePlayChanged;

        private void UpdatePausePlay()
        {
            if (PausePlayChanged != null)
                PausePlayChanged.Invoke(this, IsPlayingInternal());
        }

        public void Close()
        {
            CleanPlayerAndStream(false);
        }
    }
}
