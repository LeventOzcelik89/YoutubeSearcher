using YoutubeSearcher.Web.Models;
using YoutubeSearcher.Web.Services;

namespace YoutubeSearcher.Web.Services
{
    public class SearchService
    {
        private readonly YoutubeService _youtubeService;

        public SearchService(YoutubeService youtubeService)
        {
            _youtubeService = youtubeService;
        }

        public async Task<(VideoInfo? mainVideo, List<VideoInfo> relatedVideos)> SearchWithRelatedAsync(string query)
        {
            var searchResults = await _youtubeService.SearchVideosAsync(query, 20);

            if (searchResults.Count == 0)
                return (null, new List<VideoInfo>());

            var mainVideo = searchResults[0];
            //var relatedVideos = await _youtubeService.GetRelatedVideosAsync(mainVideo.Id, 10);
            var relatedVideos = searchResults.Skip(1).ToList();

            return (mainVideo, relatedVideos);
        }

        public async Task<List<VideoInfo>> SearchByArtistAsync(string artistName, int maxResults = 20)
        {
            var query = $"artist:{artistName}";
            return await _youtubeService.SearchVideosAsync(query, maxResults);
        }
    }
}

