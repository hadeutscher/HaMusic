/* Copyright (C) 2017 Yuval Deutscher

* This Source Code Form is subject to the terms of the Mozilla Public
* License, v. 2.0. If a copy of the MPL was not distributed with this
* file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;

namespace HaMusicServer
{
    public class SilentImplementation : IMediaPlayerImplementation
    {
        public event EventHandler SongEnded;

        public void Dispose()
        {
        }

        public int GetPos()
        {
            return 0;
        }

        public int PlaySong(string path)
        {
            return 1;
        }

        public void SetPlaying(bool playing)
        {
        }

        public void SetPos(int time)
        {
        }

        public void SetVolume(int vol)
        {
        }
    }
}
