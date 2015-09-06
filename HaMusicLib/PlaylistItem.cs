/* Copyright (C) 2015 haha01haha01

* This Source Code Form is subject to the terms of the Mozilla Public
* License, v. 2.0. If a copy of the MPL was not distributed with this
* file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using ProtoBuf;
using System.Threading;

namespace HaMusicLib
{
    [ProtoContract]
    public class PlaylistItem : PropertyNotifierBase
    {
        private static long nextUid = 0;

        private long uid;
        private string item;
        private bool played = false;

        public PlaylistItem()
        {
            this.uid = Interlocked.Increment(ref nextUid);
        }

        [ProtoMember(1)]
        public long UID
        {
            get
            {
                return uid;
            }

            set
            {
                SetField(ref uid, value);
            }
        }

        [ProtoMember(2)]
        public string Item
        {
            get
            {
                return item;
            }

            set
            {
                SetField(ref item, value);
            }
        }

        public bool Played
        {
            get
            {
                return played;
            }

            set
            {
                played = value;
            }
        }
    }
}