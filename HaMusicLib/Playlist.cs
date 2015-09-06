/* Copyright (C) 2015 haha01haha01

* This Source Code Form is subject to the terms of the Mozilla Public
* License, v. 2.0. If a copy of the MPL was not distributed with this
* file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using HaMusicLib;
using ProtoBuf;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace HaMusicLib
{
    [ProtoContract]
    public class Playlist : PropertyNotifierBase
    {
        private static long nextUid = 0;

        private FastAccessList<long, PlaylistItem> playlistItems = new FastAccessList<long, PlaylistItem>(x => x.UID);
        private string name;
        private long uid;

        public Playlist()
        {
            this.name = string.Format("Playlist {0}", uid = Interlocked.Increment(ref nextUid));
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
        public FastAccessList<long, PlaylistItem> PlaylistItems
        {
            get
            {
                return playlistItems;
            }

            set
            {
                SetField(ref playlistItems, value);
            }
        }

        [ProtoMember(3)]
        public string Name
        {
            get
            {
                return name;
            }

            set
            {
                SetField(ref name, value);
            }
        }
    }
}
