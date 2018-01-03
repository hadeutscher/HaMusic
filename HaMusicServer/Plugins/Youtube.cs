using HaMusicLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Linq;
using YoutubeExplode;

namespace HaMusicServer.Plugins
{
    public class Youtube : HaMusicLib.Pluginability.IPlaylistItemTypeManager
    {
        static Youtube()
        {
            HaProtoImpl.PlaylistItemTypeManagers["youtube"] = new Youtube();
        }

        public Task ItemRemoved(PlaylistItem item)
        {
            File.Delete(item.Item);
            return Task.FromResult(false);
        }

        public async Task<IEnumerable<PlaylistItem>> ParseItems(List<string> paths)
        {
            List<PlaylistItem> result = new List<PlaylistItem>();
            foreach (string path in paths)
            {
                var client = new YoutubeClient();
                var videoInfo = await client.GetVideoAsync(path);
                string title = videoInfo.Title;
                var streamInfoSet = await client.GetVideoMediaStreamInfosAsync(path);

                var list = streamInfoSet.Audio.Where(x => x.Container == YoutubeExplode.Models.MediaStreams.Container.M4A);

                YoutubeExplode.Models.MediaStreams.AudioStreamInfo best = null;

                foreach (var item in list)
                {
                    if (best == null || best.Bitrate < item.Bitrate)
                    {
                        best = item;
                    }
                }

                if (best == null)
                {
                    throw new Exception($"Could not find suitable download for {path}");
                }

                var ext = best.Container.ToString().ToLower();
                string localPath = Path.Combine(Path.GetTempPath(), Utils.Pathify(title) + "." + ext);
                await client.DownloadMediaStreamAsync(best, localPath);
                result.Add(new PlaylistItem() { Special = "youtube", ExternalName = title, Item = path });
            }
            return result;
        }
    }
}
