/* Copyright (C) 2015 haha01haha01

* This Source Code Form is subject to the terms of the Mozilla Public
* License, v. 2.0. If a copy of the MPL was not distributed with this
* file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;

namespace HaMusicServer
{
    public interface IHaMusicPlayer : IDisposable
    {
        void SetVolume(int vol);
        void SetPos(int time);
        int GetPos();
        void SetPlaying(bool playing);
        int PlaySong(string path);

        event EventHandler SongEnded;
    }
}
