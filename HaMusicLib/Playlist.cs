/* Copyright (C) 2015 haha01haha01

* This Source Code Form is subject to the terms of the Mozilla Public
* License, v. 2.0. If a copy of the MPL was not distributed with this
* file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using ProtoBuf;
using System;
using System.IO;
using System.Threading;

namespace HaMusicLib
{
    [ProtoContract]
    public class Playlist : PropertyNotifierBase
    {
        private static long nextUid = 0;

        private FastAccessList<long, PlaylistItem> playlistItems = new FastAccessList<long, PlaylistItem>(x => x.UID);
        private string name = "Playlist";
        private long uid;

        public Playlist()
        {
            uid = Interlocked.Increment(ref nextUid);
        }

        [ProtoMember(1, IsRequired = true)]
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

        [ProtoMember(2, IsRequired = true)]
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

        [ProtoMember(3, IsRequired = true)]
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

        public static void DeserializeCounters(Stream ifs)
        {
            byte[] buf = new byte[8];
            ifs.Read(buf, 0, 8);
            nextUid = BitConverter.ToInt64(buf, 0);
        }

        public static void SerializeCounters(Stream ofs)
        {
            ofs.Write(BitConverter.GetBytes(nextUid), 0, 8);
        }
    }
}
