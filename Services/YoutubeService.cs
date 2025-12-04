using YoutubeExplode;
using YoutubeExplode.Videos;
using YoutubeExplode.Search;
using YoutubeExplode.Playlists;
using YoutubeSearcher.Models;

namespace YoutubeSearcher.Services
{
    public class YoutubeService
    {
        private readonly YoutubeClient _youtubeClient;

        public YoutubeService()
        {
            _youtubeClient = new YoutubeClient();
        }

        public async Task<List<VideoInfo>> SearchVideosAsync(string query, int maxResults = 20)
        {
            var videos = new List<VideoInfo>();
            var count = 0;
            
            await foreach (var result in _youtubeClient.Search.GetVideosAsync(query))
            {
                if (count >= maxResults) break;
                
                try
                {
                    var video = await _youtubeClient.Videos.GetAsync(result.Id);
                    var thumbnail = video.Thumbnails.OrderByDescending(t => t.Resolution.Area).FirstOrDefault();
                    videos.Add(new VideoInfo
                    {
                        Id = video.Id,
                        Title = video.Title,
                        Author = video.Author.ChannelTitle,
                        ThumbnailUrl = thumbnail?.Url ?? string.Empty,
                        Duration = video.Duration,
                        Url = video.Url,
                        ViewCount = video.Engagement.ViewCount
                    });
                    count++;
                }
                catch { }
            }

            return videos;
        }

        public async Task<VideoInfo?> GetVideoInfoAsync(string videoId)
        {
            try
            {
                var video = await _youtubeClient.Videos.GetAsync(videoId);
                var thumbnail = video.Thumbnails.OrderByDescending(t => t.Resolution.Area).FirstOrDefault();
                return new VideoInfo
                {
                    Id = video.Id,
                    Title = video.Title,
                    Author = video.Author.ChannelTitle,
                    ThumbnailUrl = thumbnail?.Url ?? string.Empty,
                    Duration = video.Duration,
                    Url = video.Url,
                    ViewCount = video.Engagement.ViewCount
                };
            }
            catch
            {
                return null;
            }
        }

        public async Task<List<VideoInfo>> GetRelatedVideosAsync(string videoId, int maxResults = 10)
        {
            var relatedVideos = new List<VideoInfo>();
            
            try
            {
                var video = await _youtubeClient.Videos.GetAsync(videoId);
                var searchQuery = $"{video.Title} {video.Author.ChannelTitle}";
                var count = 0;
                
                await foreach (var result in _youtubeClient.Search.GetVideosAsync(searchQuery))
                {
                    if (result.Id == videoId) continue;
                    if (count >= maxResults) break;
                    
                    try
                    {
                        var relatedVideo = await _youtubeClient.Videos.GetAsync(result.Id);
                        var thumbnail = relatedVideo.Thumbnails.OrderByDescending(t => t.Resolution.Area).FirstOrDefault();
                        relatedVideos.Add(new VideoInfo
                        {
                            Id = relatedVideo.Id,
                            Title = relatedVideo.Title,
                            Author = relatedVideo.Author.ChannelTitle,
                            ThumbnailUrl = thumbnail?.Url ?? string.Empty,
                            Duration = relatedVideo.Duration,
                            Url = relatedVideo.Url,
                            ViewCount = relatedVideo.Engagement.ViewCount
                        });
                        count++;
                    }
                    catch { }
                }
            }
            catch { }

            return relatedVideos;
        }

        public async Task<List<VideoInfo>> GetPlaylistVideosAsync(string playlistUrl)
        {
            var videos = new List<VideoInfo>();
            
            try
            {
                var playlist = await _youtubeClient.Playlists.GetAsync(playlistUrl);
                
                await foreach (var video in _youtubeClient.Playlists.GetVideosAsync(playlistUrl))
                {
                    var thumbnail = video.Thumbnails.OrderByDescending(t => t.Resolution.Area).FirstOrDefault();
                    videos.Add(new VideoInfo
                    {
                        Id = video.Id,
                        Title = video.Title,
                        Author = video.Author.ChannelTitle,
                        ThumbnailUrl = thumbnail?.Url ?? string.Empty,
                        Duration = video.Duration,
                        Url = video.Url
                    });
                }
            }
            catch { }

            return videos;
        }

        public async Task<Stream> GetVideoStreamAsync(string videoId)
        {
            var streamManifest = await _youtubeClient.Videos.Streams.GetManifestAsync(videoId);
            var audioStream = streamManifest.GetAudioOnlyStreams().OrderByDescending(s => s.Bitrate).FirstOrDefault();
            if (audioStream == null)
                throw new Exception("Audio stream bulunamadı");
            return await _youtubeClient.Videos.Streams.GetAsync(audioStream);
        }
    }
}

