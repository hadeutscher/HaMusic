/* Copyright (C) 2015 haha01haha01

* This Source Code Form is subject to the terms of the Mozilla Public
* License, v. 2.0. If a copy of the MPL was not distributed with this
* file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System.Collections.Generic;
using ProtoBuf;
using System.IO;
using HaMusicLib;
using System.Linq;
using System;

namespace HaMusicLib
{
    [ProtoContract]
    public class ServerDataSource : PropertyNotifierBase
    {
        public const string LocalVersion = "3.5";
        public object Lock = new object();
        private FastAccessList<long, Playlist> playlists = new FastAccessList<long, Playlist>(x => x.UID);
        private Playlist library = new Playlist();
        private PlaylistItem currentItem = null;
        private PlaylistItem nextItemOverride = null;
        private HaProtoImpl.InjectionType nextItemOverrideAction = HaProtoImpl.InjectionType.INJECT_SONG;
        private HaProtoImpl.MoveType mode = HaProtoImpl.MoveType.NEXT;
        private int volume = 50;
        private int position = 0;
        private int maximum = 0;
        private bool playing = false;
        private string remoteVersion = LocalVersion;

        public ServerDataSource()
        {
        }

        public static ServerDataSource Deserialize(byte[] buf)
        {
            return Deserialize(new MemoryStream(buf));
        }

        public static ServerDataSource Deserialize(Stream data)
        {
            return Serializer.Deserialize<ServerDataSource>(data);
        }

        public void Serialize(Stream data)
        {
            Serializer.Serialize<ServerDataSource>(data, this);
        }

        public byte[] Serialize()
        {
            MemoryStream data = new MemoryStream();
            Serialize(data);
            return data.ToArray();
        }

        [ProtoMember(1)]
        public FastAccessList<long, Playlist> Playlists
        {
            get
            {
                return playlists;
            }

            set
            {
                SetField(ref playlists, value);
            }
        }

        [ProtoMember(2)]
        public PlaylistItem CurrentItem
        {
            get
            {
                return currentItem;
            }

            set
            {
                SetField(ref currentItem, value);
            }
        }

        [ProtoMember(3)]
        public HaProtoImpl.MoveType Mode
        {
            get
            {
                return mode;
            }
            set
            {
                SetField(ref mode, value);
            }
        }

        [ProtoMember(4)]
        public int Volume
        {
            get
            {
                return volume;
            }

            set
            {
                SetField(ref volume, value);
            }
        }

        [ProtoMember(5)]
        public int Position
        {
            get
            {
                return position;
            }

            set
            {
                SetField(ref position, value);
            }
        }

        [ProtoMember(6)]
        public int Maximum
        {
            get
            {
                return maximum;
            }

            set
            {
                SetField(ref maximum, value);
            }
        }

        [ProtoMember(7)]
        public bool Playing
        {
            get
            {
                return playing;
            }

            set
            {
                SetField(ref playing, value);
            }
        }

        [ProtoMember(8)]
        public string RemoteVersion
        {
            get
            {
                return remoteVersion;
            }

            set
            {
                SetField(ref remoteVersion, value);
            }
        }

        [ProtoMember(9)]
        public PlaylistItem NextItemOverride
        {
            get
            {
                return nextItemOverride;
            }

            set
            {
                SetField(ref nextItemOverride, value);
            }
        }

        [ProtoMember(10)]
        public HaProtoImpl.InjectionType NextItemOverrideAction
        {
            get
            {
                return nextItemOverrideAction;
            }

            set
            {
                SetField(ref nextItemOverrideAction, value);
            }
        }

        [ProtoMember(11)]
        public Playlist LibraryPlaylist
        {
            get
            {
                return library;
            }

            set
            {
                SetField(ref library, value);
            }
        }

        public Playlist GetPlaylistForItem(long uid, bool allowLibrarySearch = false)
        {
            foreach (Playlist pl in playlists)
            {
                if (pl.PlaylistItems.ContainsKey(uid))
                {
                    return pl;
                }
            }
            if (allowLibrarySearch && library.PlaylistItems.ContainsKey(uid))
                return library;
            throw new KeyNotFoundException();
        }

        public PlaylistItem GetItem(long uid, bool allowLibrarySearch=false)
        {
            PlaylistItem result;
            if (TryGetItem(uid, out result, allowLibrarySearch))
            {
                return result;
            }
            else
            {
                throw new KeyNotFoundException();
            }
        }

        public bool TryGetItem(long uid, out PlaylistItem result, bool allowLibrarySearch=false)
        {
            foreach (Playlist pl in playlists)
            {
                if (pl.PlaylistItems.FastTryGet(uid, out result))
                {
                    return true;
                }
            }
            if (allowLibrarySearch && library.PlaylistItems.FastTryGet(uid, out result))
                return true;
            result = null;
            return false;
        }
    }
}
