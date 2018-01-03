using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HaMusicLib.Pluginability
{
    public interface IPlaylistItemTypeManager
    {
        Task<IEnumerable<PlaylistItem>> ParseItems(List<string> paths);
        Task ItemRemoved(PlaylistItem item);
    }
}
