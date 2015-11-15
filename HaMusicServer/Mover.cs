/* Copyright (C) 2015 haha01haha01

* This Source Code Form is subject to the terms of the Mozilla Public
* License, v. 2.0. If a copy of the MPL was not distributed with this
* file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using HaMusicLib;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Security.Cryptography;

namespace HaMusicServer
{
    // All mover actions may lock the data source
    public class Mover
    {
        private MainForm mainForm;
        private static RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider();
        private int consecErrors = 0;

        public Mover(MainForm mainForm)
        {
            this.mainForm = mainForm;
            OnSetDataSource();
        }

        public void OnSetDataSource()
        {
            mainForm.DataSource.PropertyChanged += dataSource_PropertyChanged;
        }

        private void dataSource_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Mode")
            {
                resetShuffle();
            }
        }

        private void resetShuffle()
        {
            lock (mainForm.DataSource.Lock)
            {
                foreach (Playlist pl in mainForm.DataSource.Playlists)
                {
                    resetShuffle(pl);
                }
            }
        }

        private void resetShuffle(Playlist pl)
        {
            foreach (PlaylistItem pi in pl.PlaylistItems)
            {
                pi.Played = false;
            }
        }

        public void IncreaseErrors()
        {
            consecErrors += 1;
        }

        public void ResetErrors()
        {
            consecErrors = 0;
        }

        public static long GetRandom(long loBound, long hiBound)
        {
            byte[] data = new byte[8];
            rng.GetBytes(data);
            ulong randLong = BitConverter.ToUInt64(data, 0);
            return loBound + (long)(randLong % (ulong)(hiBound - loBound));
        }

        private int getRandomMove(Playlist pl)
        {
            List<int> candidates = new List<int>();
            int currIdx = pl.PlaylistItems.IndexOf(mainForm.DataSource.CurrentItem);
            for (int i = 0; i < pl.PlaylistItems.Count; i++)
            {
                if (i != currIdx)
                    candidates.Add(i);
            }
            if (candidates.Count > 0)
            {
                return candidates[(int)GetRandom(0, candidates.Count)];
            }
            else
            {
                return currIdx;
            }
        }

        private PlaylistItem indexToItem(int index, Playlist pl)
        {
            return index < pl.PlaylistItems.Count ? pl.PlaylistItems[index] : null;
        }

        private void ChangeNextItemOverride(PlaylistItem newOverride, HaProtoImpl.InjectionType? newType=null)
        {
            if (newType.HasValue)
                mainForm.DataSource.NextItemOverrideAction = newType.Value;
            mainForm.DataSource.NextItemOverride = newOverride;
            mainForm.BroadcastMessage(HaProtoImpl.Opcode.INJECT, new HaProtoImpl.INJECT() {
                uid = newOverride != null ? newOverride.UID : -1,
                type =newType.HasValue ? newType.Value : HaProtoImpl.InjectionType.INJECT_SONG
            });
        }

        public PlaylistItem Next()
        {
            lock (mainForm.DataSource.Lock)
            {
                try
                {
                    if (mainForm.DataSource.NextItemOverride != null)
                    {
                        PlaylistItem result;
                        switch (mainForm.DataSource.NextItemOverrideAction)
                        {
                            case HaProtoImpl.InjectionType.INJECT_SONG:
                                // Normal injection, set the song and disable nextItemOverride
                                result = mainForm.DataSource.NextItemOverride;
                                ChangeNextItemOverride(null);
                                return result;
                            case HaProtoImpl.InjectionType.INJECT_AS_IF_SONG_ENDED:
                                mainForm.DataSource.CurrentItem = mainForm.DataSource.NextItemOverride;
                                ChangeNextItemOverride(null);
                                // Purposely break and not return, this will cause the rest of the routine to advance CurrentItem
                                break;
                            case HaProtoImpl.InjectionType.INJECT_AND_RETURN:
                                PlaylistItem curr = mainForm.DataSource.CurrentItem;
                                result = mainForm.DataSource.NextItemOverride;
                                ChangeNextItemOverride(curr, HaProtoImpl.InjectionType.INJECT_AS_IF_SONG_ENDED);
                                return result;
                        }
                        
                    }
                    if (mainForm.DataSource.CurrentItem == null)
                    {
                        return null;
                    }
                    Playlist pl = mainForm.DataSource.GetPlaylistForItem(mainForm.DataSource.CurrentItem.UID, true);
                    if (consecErrors > pl.PlaylistItems.Count || pl.PlaylistItems.Count == 0)
                    {
                        // Too many errors in a row, or nothing to play, just stop
                        return null;
                    }
                    switch (mainForm.DataSource.Mode)
                    {
                        case HaProtoImpl.MoveType.NEXT:
                            return indexToItem(pl.PlaylistItems.IndexOf(mainForm.DataSource.CurrentItem) + 1, pl);
                        case HaProtoImpl.MoveType.RANDOM:
                            return indexToItem(getRandomMove(pl), pl);
                        case HaProtoImpl.MoveType.SHUFFLE:
                            List<int> candidates = new List<int>();
                            for (int i = 0; i < pl.PlaylistItems.Count; i++)
                            {
                                if (!pl.PlaylistItems[i].Played)
                                {
                                    candidates.Add(i);
                                }
                            }
                            int winner;
                            if (candidates.Count > 0)
                            {
                                winner = candidates[(int)GetRandom(0, candidates.Count)];
                            }
                            else
                            {
                                resetShuffle(pl);
                                winner = getRandomMove(pl); // Fallback to random when we just cleared the shuffle set
                            }
                            // Winner will be marked as playing when it gets pulled from the playlist by the media player
                            return indexToItem(winner, pl);
                        default:
                            return null;
                    }
                }
                catch (Exception e)
                {
                    mainForm.log(Program.GetErrorException(e));
                    return null;
                }
            }
        }
    }
}
