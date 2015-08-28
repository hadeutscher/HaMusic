/* Copyright (C) 2015 haha01haha01

* This Source Code Form is subject to the terms of the Mozilla Public
* License, v. 2.0. If a copy of the MPL was not distributed with this
* file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HaMusicServer
{
    public interface IHaMusicPlayer
    {
        void SetVolume(int vol);
        int GetVolume();
        void Seek(int time);
        Tuple<int, int> GetPos();
        void SetPlaying(bool playing);
        bool IsPlaying();
        void OnIndexChanged(bool forceReplay);
        event EventHandler<bool> PausePlayChanged;
        void Close();
    }
}
