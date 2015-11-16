/* Copyright (C) 2015 haha01haha01

* This Source Code Form is subject to the terms of the Mozilla Public
* License, v. 2.0. If a copy of the MPL was not distributed with this
* file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using ProtoBuf;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Linq;

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
        public string Item
        {
            get
            {
                return item;
            }

            set
            {
                _cachedItemLower = null; // Must do this before setting field, since SetField may invoke external code depending on us
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

        public bool MatchKeywords(IEnumerable<string> keywords)
        {
            return keywords.Any(x => ItemLower.Contains(x));
        }

        private string _cachedItemLower = null;
        private string ItemLower
        {
            get
            {
                return _cachedItemLower == null ? (_cachedItemLower = Item.ToLower()) : _cachedItemLower;
            }
        }

        public override bool Equals(object obj)
        {
            return obj is PlaylistItem && ((PlaylistItem)obj).UID == UID;
        }

        public override int GetHashCode()
        {
            return uid.GetHashCode();
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