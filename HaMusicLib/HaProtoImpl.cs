/* Copyright (C) 2015 haha01haha01

* This Source Code Form is subject to the terms of the Mozilla Public
* License, v. 2.0. If a copy of the MPL was not distributed with this
* file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using ProtoBuf;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;

namespace HaMusicLib
{
    public static class HaProtoImpl
    {
        public enum HaMusicEntity
        {
            Server,
            Client
        }

        public static HaMusicEntity Entity = HaMusicEntity.Server;

        public static bool IsServer()
        {
            return Entity == HaMusicEntity.Server;
        }

        public static bool IsClient()
        {
            return Entity == HaMusicEntity.Client;
        }

        public enum Opcode
        {
            // Special packets
            GETDB,
            SETDB,

            // Database updaters
            ADD,
            REMOVE,
            CLEAR,
            SETSONG,
            ADDPL,
            DELPL,
            RENPL,
            SETMOVE,
            REORDER,
            INJECT,
            LIBRARY_ADD,
            LIBRARY_REMOVE,
            LIBRARY_RESET,

            // Skip is special
            SKIP,

            // Other
            SETVOL,
            SEEK,
            SETPLAYING
        }

        public enum MoveType
        {
            NEXT,
            RANDOM,
            SHUFFLE
        }

        public interface HaProtoPacket
        {
            byte[] Build();
            bool ApplyToDatabase(ServerDataSource dataSource);
        }

        [ProtoContract]
        public class GETDB : HaProtoImpl.HaProtoPacket
        {
            public GETDB()
            {
            }

            public static GETDB Parse(byte[] buf)
            {
                using (MemoryStream ms = new MemoryStream(buf))
                    return Serializer.Deserialize<GETDB>(ms);
            }

            public byte[] Build()
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    Serializer.Serialize<GETDB>(ms, this);
                    return ms.ToArray();
                }
            }

            public bool ApplyToDatabase(ServerDataSource dataSource)
            {
                throw new NotSupportedException();
            }
        }

        [ProtoContract]
        public class SETDB : HaProtoImpl.HaProtoPacket
        {
            [ProtoMember(1)]
            public ServerDataSource dataSource { get; set; }

            public SETDB()
            {
            }

            public static SETDB Parse(byte[] buf)
            {
                using (MemoryStream ms = new MemoryStream(buf))
                    return Serializer.Deserialize<SETDB>(ms);
            }

            public byte[] Build()
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    Serializer.Serialize<SETDB>(ms, this);
                    return ms.ToArray();
                }
            }

            public bool ApplyToDatabase(ServerDataSource dataSource)
            {
                throw new NotSupportedException();
            }
        }

        [ProtoContract]
        public class ADD : HaProtoImpl.HaProtoPacket
        {
            [ProtoMember(1)]
            public long uid { get; set; }

            [ProtoMember(2)]
            public List<string> paths { get; set; }

            [ProtoMember(3)]
            public List<long> pathUids { get; set; }

            [ProtoMember(4)]
            public long after { get; set; }

            public ADD()
            {
            }

            public static ADD Parse(byte[] buf)
            {
                using (MemoryStream ms = new MemoryStream(buf))
                    return Serializer.Deserialize<ADD>(ms);
            }

            public byte[] Build()
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    Serializer.Serialize<ADD>(ms, this);
                    return ms.ToArray();
                }
            }

            public bool ApplyToDatabase(ServerDataSource dataSource)
            {
                if (IsServer())
                    pathUids = new List<long>();
                lock (dataSource.Lock)
                {
                    Playlist pl = dataSource.Playlists.FastGet(uid);
                    int i = 0;
                    int index = after < 0 ? 0 : pl.PlaylistItems.IndexOf(pl.PlaylistItems.FastGet(after)) + 1;
                    foreach (PlaylistItem pi in IsServer() ? paths.Select(x => new PlaylistItem() { Item = x })
                                                           : paths.Select(x => new PlaylistItem() { Item = x, UID = pathUids[i++] }))
                    {
                        pl.PlaylistItems.Insert(index++, pi);
                        if (IsServer())
                            pathUids.Add(pi.UID);
                    }
                }
                return false;
            }
        }

        [ProtoContract]
        public class REMOVE : HaProtoImpl.HaProtoPacket
        {
            [ProtoMember(1)]
            public long uid { get; set; }

            [ProtoMember(2)]
            public List<long> items { get; set; }

            public REMOVE()
            {
            }

            public static REMOVE Parse(byte[] buf)
            {
                using (MemoryStream ms = new MemoryStream(buf))
                    return Serializer.Deserialize<REMOVE>(ms);
            }

            public byte[] Build()
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    Serializer.Serialize<REMOVE>(ms, this);
                    return ms.ToArray();
                }
            }

            public bool ApplyToDatabase(ServerDataSource dataSource)
            {
                bool result = false;
                lock (dataSource.Lock)
                {
                    Playlist pl = dataSource.Playlists.FastGet(uid);
                    if (dataSource.CurrentItem != null && items.Contains(dataSource.CurrentItem.UID))
                    {
                        dataSource.CurrentItem = null;
                        result = true;
                    }
                    if (dataSource.NextItemOverride != null && items.Contains(dataSource.NextItemOverride.UID))
                    {
                        dataSource.NextItemOverride = null;
                    }

                    // This is a performance-critical area in the client, because Remove is a costly operation on INotifyCollectionChanged objects
                    if (items.Count > 1)
                    {
                        HashSet<long> fastItems = new HashSet<long>(items);
                        pl.PlaylistItems.RemoveAll(x => fastItems.Contains(x.UID));
                    }
                    else if (items.Count > 0)
                    {
                        pl.PlaylistItems.Remove(pl.PlaylistItems.FastGet(items[0]));
                    }
                }
                return result;
            }
        }

        [ProtoContract]
        public class CLEAR : HaProtoImpl.HaProtoPacket
        {
            [ProtoMember(1)]
            public long uid { get; set; }

            public CLEAR()
            {
            }

            public static CLEAR Parse(byte[] buf)
            {
                using (MemoryStream ms = new MemoryStream(buf))
                    return Serializer.Deserialize<CLEAR>(ms);
            }

            public byte[] Build()
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    Serializer.Serialize<CLEAR>(ms, this);
                    return ms.ToArray();
                }
            }

            public bool ApplyToDatabase(ServerDataSource dataSource)
            {
                bool result = false;
                lock (dataSource.Lock)
                {
                    Playlist pl = dataSource.Playlists.FastGet(uid);
                    if (dataSource.CurrentItem != null && pl.PlaylistItems.ContainsKey(dataSource.CurrentItem.UID))
                    {
                        dataSource.CurrentItem = null;
                        result = true;
                    }
                    if (dataSource.NextItemOverride != null && pl.PlaylistItems.ContainsKey(dataSource.NextItemOverride.UID))
                    {
                        dataSource.NextItemOverride = null;
                    }
                    pl.PlaylistItems.Clear();
                }
                return result;
            }
        }

        [ProtoContract]
        public class SETSONG : HaProtoImpl.HaProtoPacket
        {
            [ProtoMember(1)]
            public long uid { get; set; }

            public SETSONG()
            {
            }

            public static SETSONG Parse(byte[] buf)
            {
                using (MemoryStream ms = new MemoryStream(buf))
                    return Serializer.Deserialize<SETSONG>(ms);
            }

            public byte[] Build()
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    Serializer.Serialize<SETSONG>(ms, this);
                    return ms.ToArray();
                }
            }

            public bool ApplyToDatabase(ServerDataSource dataSource)
            {
                lock (dataSource.Lock)
                {
                    dataSource.CurrentItem = uid < 0 ? null : dataSource.GetItem(uid, true);
                    if (dataSource.NextItemOverride != null && dataSource.NextItemOverride.UID == uid)
                    {
                        dataSource.NextItemOverride = null;
                    }
                }
                return true;
            }
        }

        [ProtoContract]
        public class SKIP : HaProtoImpl.HaProtoPacket
        {
            public SKIP()
            {
            }

            public static SKIP Parse(byte[] buf)
            {
                using (MemoryStream ms = new MemoryStream(buf))
                    return Serializer.Deserialize<SKIP>(ms);
            }

            public byte[] Build()
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    Serializer.Serialize<SKIP>(ms, this);
                    return ms.ToArray();
                }
            }

            public bool ApplyToDatabase(ServerDataSource dataSource)
            {
                throw new NotSupportedException();
            }
        }

        [ProtoContract]
        public class ADDPL : HaProtoImpl.HaProtoPacket
        {
            [ProtoMember(1)]
            public long uid { get; set; }

            public ADDPL()
            {
            }

            public static ADDPL Parse(byte[] buf)
            {
                using (MemoryStream ms = new MemoryStream(buf))
                    return Serializer.Deserialize<ADDPL>(ms);
            }

            public byte[] Build()
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    Serializer.Serialize<ADDPL>(ms, this);
                    return ms.ToArray();
                }
            }

            public bool ApplyToDatabase(ServerDataSource dataSource)
            {
                lock (dataSource.Lock)
                {
                    Playlist pl = IsServer() ? new Playlist()
                                             : new Playlist() { UID = uid };
                    dataSource.Playlists.Add(pl);
                    if (IsServer())
                        uid = pl.UID;
                }
                return false;
            }
        }

        [ProtoContract]
        public class DELPL : HaProtoImpl.HaProtoPacket
        {
            [ProtoMember(1)]
            public long uid { get; set; }

            public DELPL()
            {
            }

            public static DELPL Parse(byte[] buf)
            {
                using (MemoryStream ms = new MemoryStream(buf))
                    return Serializer.Deserialize<DELPL>(ms);
            }

            public byte[] Build()
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    Serializer.Serialize<DELPL>(ms, this);
                    return ms.ToArray();
                }
            }

            public bool ApplyToDatabase(ServerDataSource dataSource)
            {
                bool result = false;
                lock (dataSource.Lock)
                {
                    if (dataSource.Playlists.Count < 2)
                    {
                        return result;
                    }
                    Playlist pl = dataSource.Playlists.FastGet(uid);
                    if (dataSource.CurrentItem != null && pl.PlaylistItems.ContainsKey(dataSource.CurrentItem.UID))
                    {
                        dataSource.CurrentItem = null;
                        result = true;
                    }
                    if (dataSource.NextItemOverride != null && pl.PlaylistItems.ContainsKey(dataSource.NextItemOverride.UID))
                    {
                        dataSource.NextItemOverride = null;
                    }
                    dataSource.Playlists.Remove(pl);
                }
                return result;
            }
        }

        [ProtoContract]
        public class RENPL : HaProtoImpl.HaProtoPacket
        {
            [ProtoMember(1)]
            public long uid { get; set; }

            [ProtoMember(2)]
            public string name { get; set; }

            public RENPL()
            {
            }

            public static RENPL Parse(byte[] buf)
            {
                using (MemoryStream ms = new MemoryStream(buf))
                    return Serializer.Deserialize<RENPL>(ms);
            }

            public byte[] Build()
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    Serializer.Serialize<RENPL>(ms, this);
                    return ms.ToArray();
                }
            }

            public bool ApplyToDatabase(ServerDataSource dataSource)
            {
                lock (dataSource.Lock)
                {
                    Playlist pl = dataSource.Playlists.FastGet(uid);
                    pl.Name = name;
                }
                return false;
            }
        }

        [ProtoContract]
        public class SETVOL : HaProtoImpl.HaProtoPacket
        {
            [ProtoMember(1)]
            public int volume { get; set; }

            public SETVOL()
            {
            }

            public static SETVOL Parse(byte[] buf)
            {
                using (MemoryStream ms = new MemoryStream(buf))
                    return Serializer.Deserialize<SETVOL>(ms);
            }

            public byte[] Build()
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    Serializer.Serialize<SETVOL>(ms, this);
                    return ms.ToArray();
                }
            }

            public bool ApplyToDatabase(ServerDataSource dataSource)
            {
                throw new NotSupportedException();
            }
        }

        [ProtoContract]
        public class SEEK : HaProtoImpl.HaProtoPacket
        {
            [ProtoMember(1)]
            public int pos { get; set; }

            [ProtoMember(2)]
            public int max { get; set; }

            public SEEK()
            {
            }

            public static SEEK Parse(byte[] buf)
            {
                using (MemoryStream ms = new MemoryStream(buf))
                    return Serializer.Deserialize<SEEK>(ms);
            }

            public byte[] Build()
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    Serializer.Serialize<SEEK>(ms, this);
                    return ms.ToArray();
                }
            }

            public bool ApplyToDatabase(ServerDataSource dataSource)
            {
                throw new NotSupportedException();
            }
        }

        [ProtoContract]
        public class SETPLAYING : HaProtoImpl.HaProtoPacket
        {
            [ProtoMember(1)]
            public bool playing { get; set; }

            public SETPLAYING()
            {
            }

            public static SETPLAYING Parse(byte[] buf)
            {
                using (MemoryStream ms = new MemoryStream(buf))
                    return Serializer.Deserialize<SETPLAYING>(ms);
            }

            public byte[] Build()
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    Serializer.Serialize<SETPLAYING>(ms, this);
                    return ms.ToArray();
                }
            }

            public bool ApplyToDatabase(ServerDataSource dataSource)
            {
                throw new NotSupportedException();
            }
        }

        [ProtoContract]
        public class SETMOVE : HaProtoImpl.HaProtoPacket
        {
            [ProtoMember(1)]
            public HaProtoImpl.MoveType move { get; set; }

            public SETMOVE()
            {
            }

            public static SETMOVE Parse(byte[] buf)
            {
                using (MemoryStream ms = new MemoryStream(buf))
                    return Serializer.Deserialize<SETMOVE>(ms);
            }

            public byte[] Build()
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    Serializer.Serialize<SETMOVE>(ms, this);
                    return ms.ToArray();
                }
            }

            public bool ApplyToDatabase(ServerDataSource dataSource)
            {
                dataSource.Mode = move;
                return false;
            }
        }

        [ProtoContract]
        public class REORDER : HaProtoImpl.HaProtoPacket
        {
            [ProtoMember(1)]
            public long pid { get; set; }

            [ProtoMember(2)]
            public long after { get; set; }

            [ProtoMember(3)]
            public List<long> items { get; set; }

            public REORDER()
            {
            }

            public static REORDER Parse(byte[] buf)
            {
                using (MemoryStream ms = new MemoryStream(buf))
                    return Serializer.Deserialize<REORDER>(ms);
            }

            public byte[] Build()
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    Serializer.Serialize<REORDER>(ms, this);
                    return ms.ToArray();
                }
            }

            public bool ApplyToDatabase(ServerDataSource dataSource)
            {
                bool abortClient = false;
                List<long> needles;
                if (items.Contains(after))
                    return false;
                lock (dataSource.Lock)
                {
                    List<PlaylistItem> foundNeedles = new List<PlaylistItem>();
                    needles = items.ToList();
                    HashSet<Playlist> relevantPlaylists = new HashSet<Playlist>();
                    for (int haystackIndex = 0; haystackIndex < dataSource.Playlists.Count && needles.Count > 0; haystackIndex++)
                    {
                        Playlist haystack = dataSource.Playlists[haystackIndex];
                        for (int needleIndex = 0; needleIndex < needles.Count; needleIndex++)
                        {
                            PlaylistItem foundNeedle;
                            long uid = needles[needleIndex];
                            if (haystack.PlaylistItems.FastTryGet(uid, out foundNeedle))
                            {
                                foundNeedles.Add(foundNeedle);
                                relevantPlaylists.Add(haystack);
                            }
                        }
                    }

                    if (needles.Count != foundNeedles.Count)
                    {
                        // This is bad, some UIDs were not found - abort the client, but only after we re-add the items we removed
                        abortClient = true;
                    }

                    HashSet<long> fastNeedles = new HashSet<long>(needles);
                    foreach (Playlist haystack in relevantPlaylists)
                    {
                        haystack.PlaylistItems.RemoveAll(x => fastNeedles.Contains(x.UID));
                    }

                    Playlist pl = dataSource.Playlists.FastGet(pid);
                    int index = after < 0 ? 0 : pl.PlaylistItems.IndexOf(pl.PlaylistItems.FastGet(after)) + 1;
                    foreach (PlaylistItem currItem in foundNeedles)
                    {
                        pl.PlaylistItems.Insert(index++, currItem);
                    }
                }

                if (abortClient)
                {
                    throw new Exception("Could not find UIDs: " + needles.Select(x => x.ToString()).Aggregate((x, y) => x + " " + y));
                }
                return false;
            }
        }

        [ProtoContract]
        public class INJECT : HaProtoImpl.HaProtoPacket
        {
            [ProtoMember(1)]
            public long uid { get; set; }

            public INJECT()
            {
            }

            public static INJECT Parse(byte[] buf)
            {
                using (MemoryStream ms = new MemoryStream(buf))
                    return Serializer.Deserialize<INJECT>(ms);
            }

            public byte[] Build()
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    Serializer.Serialize<INJECT>(ms, this);
                    return ms.ToArray();
                }
            }

            public bool ApplyToDatabase(ServerDataSource dataSource)
            {
                lock (dataSource.Lock)
                {
                    dataSource.NextItemOverride = uid < 0 ? null : dataSource.GetItem(uid, true);
                }
                return false;
            }
        }

        [ProtoContract]
        public class LIBRARY_ADD : HaProtoImpl.HaProtoPacket
        {
            [ProtoMember(1)]
            public List<string> paths { get; set; }

            [ProtoMember(2)]
            public List<long> pathUids { get; set; }

            public LIBRARY_ADD()
            {
            }

            public static LIBRARY_ADD Parse(byte[] buf)
            {
                using (MemoryStream ms = new MemoryStream(buf))
                    return Serializer.Deserialize<LIBRARY_ADD>(ms);
            }

            public byte[] Build()
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    Serializer.Serialize<LIBRARY_ADD>(ms, this);
                    return ms.ToArray();
                }
            }

            public bool ApplyToDatabase(ServerDataSource dataSource)
            {
                if (IsServer())
                    pathUids = new List<long>();
                lock (dataSource.Lock)
                {
                    int i = 0;
                    foreach (PlaylistItem pi in IsServer() ? paths.Select(x => new PlaylistItem() { Item = x })
                                                           : paths.Select(x => new PlaylistItem() { Item = x, UID = pathUids[i++] }))
                    {
                        dataSource.LibraryPlaylist.PlaylistItems.Add(pi);
                        if (IsServer())
                            pathUids.Add(pi.UID);
                    }
                }
                return false;
            }
        }

        [ProtoContract]
        public class LIBRARY_REMOVE : HaProtoImpl.HaProtoPacket
        {
            [ProtoMember(1)]
            public List<string> paths { get; set; }

            public LIBRARY_REMOVE()
            {
            }

            public static LIBRARY_REMOVE Parse(byte[] buf)
            {
                using (MemoryStream ms = new MemoryStream(buf))
                    return Serializer.Deserialize<LIBRARY_REMOVE>(ms);
            }

            public byte[] Build()
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    Serializer.Serialize<LIBRARY_REMOVE>(ms, this);
                    return ms.ToArray();
                }
            }

            public bool ApplyToDatabase(ServerDataSource dataSource)
            {
                HashSet<string> paths_set = new HashSet<string>(paths);
                bool result = false;
                lock (dataSource.Lock)
                {
                    if (dataSource.LibraryPlaylist.PlaylistItems.Contains(dataSource.CurrentItem) && paths_set.Contains(dataSource.CurrentItem.Item))
                    {
                        dataSource.CurrentItem = null;
                        result = true;
                    }
                    if (dataSource.LibraryPlaylist.PlaylistItems.Contains(dataSource.NextItemOverride) && paths_set.Contains(dataSource.NextItemOverride.Item))
                    {
                        dataSource.NextItemOverride = null;
                    }
                    foreach (string path in paths)
                    {
                        dataSource.LibraryPlaylist.PlaylistItems.RemoveAll(x => paths_set.Contains(x.Item));
                    }
                }
                return result;
            }
        }

        [ProtoContract]
        public class LIBRARY_RESET : HaProtoImpl.HaProtoPacket
        {
            [ProtoMember(1)]
            public List<string> paths { get; set; }

            [ProtoMember(2)]
            public List<long> pathUids { get; set; }

            public LIBRARY_RESET()
            {
            }

            public static LIBRARY_RESET Parse(byte[] buf)
            {
                using (MemoryStream ms = new MemoryStream(buf))
                    return Serializer.Deserialize<LIBRARY_RESET>(ms);
            }

            public byte[] Build()
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    Serializer.Serialize<LIBRARY_RESET>(ms, this);
                    return ms.ToArray();
                }
            }

            public bool ApplyToDatabase(ServerDataSource dataSource)
            {
                if (IsServer())
                    pathUids = new List<long>();
                bool result = false;
                lock (dataSource.Lock)
                {
                    if (dataSource.LibraryPlaylist.PlaylistItems.Contains(dataSource.CurrentItem))
                    {
                        dataSource.CurrentItem = null;
                        result = true;
                    }
                    if (dataSource.LibraryPlaylist.PlaylistItems.Contains(dataSource.NextItemOverride))
                    {
                        dataSource.NextItemOverride = null;
                    }
                    dataSource.LibraryPlaylist.PlaylistItems.Clear();

                    if (paths == null)
                        return result;

                    int i = 0;
                    foreach (PlaylistItem pi in IsServer() ? paths.Select(x => new PlaylistItem() { Item = x })
                                                           : paths.Select(x => new PlaylistItem() { Item = x, UID = pathUids[i++] }))
                    {
                        dataSource.LibraryPlaylist.PlaylistItems.Add(pi);
                        if (IsServer())
                            pathUids.Add(pi.UID);
                    }
                }
                return result;
            }
        }

        public static HaProtoImpl.HaProtoPacket ApplyPacketToDatabase(HaProtoImpl.Opcode op, byte[] data, ServerDataSource dataSource, out bool result)
        {
            HaProtoPacket packet;
            switch (op)
            {
                case HaProtoImpl.Opcode.ADD:
                    packet = HaProtoImpl.ADD.Parse(data);
                    break;
                case HaProtoImpl.Opcode.REMOVE:
                    packet = HaProtoImpl.REMOVE.Parse(data);
                    break;
                case HaProtoImpl.Opcode.CLEAR:
                    packet = HaProtoImpl.CLEAR.Parse(data);
                    break;
                case HaProtoImpl.Opcode.SETSONG:
                    packet = HaProtoImpl.SETSONG.Parse(data);
                    break;
                case HaProtoImpl.Opcode.SKIP:
                    packet = HaProtoImpl.SKIP.Parse(data);
                    break;
                case HaProtoImpl.Opcode.ADDPL:
                    packet = HaProtoImpl.ADDPL.Parse(data);
                    break;
                case HaProtoImpl.Opcode.DELPL:
                    packet = HaProtoImpl.DELPL.Parse(data);
                    break;
                case HaProtoImpl.Opcode.RENPL:
                    packet = HaProtoImpl.RENPL.Parse(data);
                    break;
                case HaProtoImpl.Opcode.SETMOVE:
                    packet = HaProtoImpl.SETMOVE.Parse(data);
                    break;
                case HaProtoImpl.Opcode.REORDER:
                    packet = HaProtoImpl.REORDER.Parse(data);
                    break;
                case HaProtoImpl.Opcode.INJECT:
                    packet = HaProtoImpl.INJECT.Parse(data);
                    break;
                case HaProtoImpl.Opcode.LIBRARY_ADD:
                    packet = HaProtoImpl.LIBRARY_ADD.Parse(data);
                    break;
                case HaProtoImpl.Opcode.LIBRARY_REMOVE:
                    packet = HaProtoImpl.LIBRARY_REMOVE.Parse(data);
                    break;
                case HaProtoImpl.Opcode.LIBRARY_RESET:
                    packet = HaProtoImpl.LIBRARY_RESET.Parse(data);
                    break;
                default:
                    throw new NotImplementedException();
            }
            result = packet.ApplyToDatabase(dataSource);
            return packet;
        }

        private static byte[] ReceiveAll(this Socket s, int len)
        {
            int totalRead = 0, bRead = 0;
            byte[] result = new byte[len];
            while (totalRead < len)
            {
                bRead = s.Receive(result, totalRead, len - totalRead, SocketFlags.None);
                if (bRead <= 0)
                {
                    throw new SocketException();
                }
                totalRead += bRead;
            }
            return result;
        }

        private static int ReceiveBlock(Socket s, out byte[] data)
        {
            // Type
            int type = BitConverter.ToInt32(s.ReceiveAll(4), 0);
            
            // Length
            int len = BitConverter.ToInt32(s.ReceiveAll(4), 0);

            // Value
            data = len > 0 ? s.ReceiveAll(len) : new byte[0];

            return type;
        }

        public static Opcode Receive(Socket s, out byte[] data)
        {
            return (Opcode)ReceiveBlock(s, out data);
        }

        private static void SendBlock(Socket s, byte[] x, int type)
        {
            byte[] typeBuf = BitConverter.GetBytes(type);
            byte[] dataBuf = x;
            byte[] lenBuf = BitConverter.GetBytes(dataBuf.Length);

            s.Send(new List<ArraySegment<byte>> { new ArraySegment<byte>(typeBuf), new ArraySegment<byte>(lenBuf), new ArraySegment<byte>(dataBuf) });
        }

        private static void SafeSendBlock(Socket s, byte[] x, int type)
        {
            try
            {
                SendBlock(s, x, type);
            }
            catch (Exception)
            {
                try
                {
                    s.Close();
                }
                catch { }
            }
        }

        public static void Send(Socket s, Opcode type, HaProtoImpl.HaProtoPacket x)
        {
            Send(s, type, x.Build());
        }

        public static void Send(Socket s, Opcode type, byte[] x)
        {
            SafeSendBlock(s, x, (int)type);
        }
    }
}
