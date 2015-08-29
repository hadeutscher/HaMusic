using System.Threading;

namespace HaMusicServer
{
    public class PlaylistItem
    {
        private static long nextUid = 0;

        private long uid;
        private string item;

        public PlaylistItem(string item)
        {
            this.uid = Interlocked.Increment(ref nextUid);
            this.item = item;
        }

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
    }
}