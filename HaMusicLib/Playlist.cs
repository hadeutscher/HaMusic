using HaMusicLib;
using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace HaMusicLib
{
    [ProtoContract]
    public class Playlist
    {
        private static long nextUid = 0;

        private FastAccessList<long, PlaylistItem> playlistItems = new FastAccessList<long, PlaylistItem>(x => x.UID);
        private string name;
        private long uid;

        public Playlist()
        {
            this.name = string.Format("Playlist {0}", Interlocked.Increment(ref nextUid));
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
                uid = value;
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
                playlistItems = value;
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
                name = value;
            }
        }
    }
}
