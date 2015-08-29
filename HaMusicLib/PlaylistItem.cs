using ProtoBuf;
using System.Threading;

namespace HaMusicLib
{
    [ProtoContract]
    public class PlaylistItem
    {
        private static long nextUid = 0;

        private long uid;
        private string item;
        private bool played = false;

        public PlaylistItem(string item)
        {
            this.uid = Interlocked.Increment(ref nextUid);
            this.item = item;
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
        public string Item
        {
            get
            {
                return item;
            }

            set
            {
                item = value;
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