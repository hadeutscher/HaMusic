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
        public const string LocalVersion = "3.0";
        public object Lock = new object();
        private FastAccessList<long, Playlist> playlists = new FastAccessList<long, Playlist>(x => x.UID);
        private PlaylistItem currentItem = null;
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
                SetField(ref playlists, value, "Playlists");
            }
        }

        [ProtoMember(2, AsReference = true)]
        public PlaylistItem CurrentItem
        {
            get
            {
                return currentItem;
            }

            set
            {
                SetField(ref currentItem, value, "CurrentItem");
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
                SetField(ref mode, value, "Mode");
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
                SetField(ref volume, value, "Volume");
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
                SetField(ref position, value, "Position");
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
                SetField(ref maximum, value, "Maximum");
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
                SetField(ref playing, value, "Playing");
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
                remoteVersion = value;
            }
        }

        public Playlist GetPlaylistForItem(long uid)
        {
            foreach (Playlist pl in playlists)
            {
                if (pl.PlaylistItems.ContainsKey(uid))
                {
                    return pl;
                }
            }
            throw new KeyNotFoundException();
        }

        public PlaylistItem GetItem(long uid)
        {
            PlaylistItem result;
            foreach (Playlist pl in playlists)
            {
                if (pl.PlaylistItems.FastTryGet(uid, out result))
                {
                    return result;
                }
            }
            throw new KeyNotFoundException();
        }
    }
}
