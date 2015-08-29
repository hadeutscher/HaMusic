using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace HaMusicServer
{
    public class Playlist
    {
        private static long nextNum = 0;

        private List<PlaylistItem> playlistItems = new List<PlaylistItem>();
        private PlaylistItem currentItem = null;
        private string name;

        public Playlist()
        {
            this.name = string.Format("Playlist {0}", Interlocked.Increment(ref nextNum));
        }

        public List<PlaylistItem> PlaylistItems
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

        public PlaylistItem CurrentItem
        {
            get
            {
                return currentItem;
            }

            set
            {
                currentItem = value;
            }
        }
    }
}
