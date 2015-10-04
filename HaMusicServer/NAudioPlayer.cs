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
        private IWavePlayer player = null;
        private WaveStream stream = null;
        private MainForm sync;

        public event EventHandler SongEnded;

        public NAudioPlayer(MainForm sync)
        {
            this.sync = sync;
        }

        private void MakePlayerInternal()
        {
            player = new WaveOutEvent();
            player.PlaybackStopped += player_PlaybackStopped;
        }

        void player_PlaybackStopped(object sender, StoppedEventArgs e)
        {
            if (SongEnded != null)
                SongEnded(this, new EventArgs());
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
            sync.InvokeIfRequired((Action)delegate
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
            });
        }

        public void SetPos(int time)
        {
            sync.InvokeIfRequired((Action)delegate
            {
                if (stream == null)
                    return;
                stream.Seek(time * stream.WaveFormat.AverageBytesPerSecond, SeekOrigin.Begin);
            });
        }

        public int GetPos()
        {
            int result = 0; // To satisfy VS, this is actually always assigned inside InvokeIfRequired
            sync.InvokeIfRequired((Action)delegate
            {
                if (player == null || player.PlaybackState == PlaybackState.Stopped || stream == null)
                {
                    result = -1;
                }
                else
                {
                    result = (int)(stream.Position / stream.WaveFormat.AverageBytesPerSecond);
                }
            });
            return result;
        }

        public void SetPlaying(bool playing)
        {
            sync.InvokeIfRequired((Action)delegate
            {
                if (player == null)
                    return;
                if (!playing)
                    player.Pause();
                else
                    player.Play();
            });
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
            int result = 0;
            sync.InvokeIfRequired((Action)delegate
            {
                CleanPlayerAndStream();
                result = -1;
                if (!string.IsNullOrEmpty(path))
                {
                    try
                    {
                        MakePlayerInternal();
                        stream = CreateStream(path);
                        player.Init(stream);
                        player.Play();
                        result = (int)(stream.Length / stream.WaveFormat.AverageBytesPerSecond);
                    }
                    catch (Exception)
                    {
                        CleanPlayerAndStream();
                    }
                }
            });
            return result;
        }
       
        public void Dispose()
        {
            CleanPlayerAndStream();
        }
    }
}
