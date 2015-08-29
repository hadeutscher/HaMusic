using System.Collections.Generic;

namespace HaMusicServer
{
    public class ServerDataSource
    {
        private List<Playlist> playlists = new List<Playlist>();
        private Playlist currentPlaylist = null;

        public ServerDataSource()
        {
            Playlist defaultPlaylist = new Playlist();
            playlists.Add(defaultPlaylist);
            currentPlaylist = defaultPlaylist;
        }

        public List<Playlist> Playlists
        {
            get
            {
                return playlists;
            }

            set
            {
                playlists = value;
            }
        }

        public Playlist CurrentPlaylist
        {
            get
            {
                return currentPlaylist;
            }

            set
            {
                currentPlaylist = value;
            }
        }
    }
}
