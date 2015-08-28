/* Copyright (C) 2015 haha01haha01

* This Source Code Form is subject to the terms of the Mozilla Public
* License, v. 2.0. If a copy of the MPL was not distributed with this
* file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using HaMusicLib;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;

namespace HaMusicServer
{
    public class Mover
    {
        private MainForm mf;
        private HashSet<string> shuffleSet = new HashSet<string>();
        private HaProtoImpl.MoveType mode = HaProtoImpl.MoveType.NEXT;
        private static RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider();
        private int consecErrors = 0;

        public Mover(MainForm mf)
        {
            this.mf = mf;
        }

        public HaProtoImpl.MoveType Mode
        {
            get { return mode; }
            set { mode = value; shuffleSet.Clear(); }
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

        private int getRandomMove()
        {
            List<int> candidates = new List<int>();
            int currIdx = mf.Index;
            for (int i = 0; i < mf.playlist.Count; i++)
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

        public int AdvanceIndex()
        {
            lock (mf.playlist)
            {
                if (consecErrors > mf.playlist.Count || mf.playlist.Count == 0)
                {
                    // Too many errors in a row, or nothing to play, just stop
                    return -1;
                }
                switch (mode)
                {
                    case HaProtoImpl.MoveType.NEXT:
                        return mf.Index + 1;
                    case HaProtoImpl.MoveType.RANDOM:
                        return getRandomMove();
                    case HaProtoImpl.MoveType.SHUFFLE:
                        List<int> candidates = new List<int>();
                        for (int i = 0; i < mf.playlist.Count; i++)
                        {
                            if (!shuffleSet.Contains(mf.playlist[i]))
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
                            shuffleSet.Clear();
                            winner = getRandomMove(); // Fallback to random when we just cleared the shuffle set
                        }
                        // Winner will be marked as playing when it gets pulled from the playlist by the media player
                        return winner;
                    default:
                        return -1;
                }
            }
        }

        public void MarkPlayed(string file)
        {
            lock (mf.playlist)
            {
                shuffleSet.Add(file);
            }
        }
    }
}
