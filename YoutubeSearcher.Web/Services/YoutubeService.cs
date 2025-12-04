using YoutubeExplode;
using YoutubeExplode.Videos;
using YoutubeExplode.Search;
using YoutubeExplode.Playlists;
using YoutubeSearcher.Web.Models;
using YoutubeExplode.Common;

namespace YoutubeSearcher.Web.Services
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
                        ViewCount = video.Engagement.ViewCount,
                        LikeCount = video.Engagement.LikeCount,
                        DislikeCount = video.Engagement.DislikeCount,
                        AverageRating = video.Engagement.AverageRating
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
                    ViewCount = video.Engagement.ViewCount,
                    LikeCount = video.Engagement.LikeCount,
                    DislikeCount = video.Engagement.DislikeCount,
                    AverageRating = video.Engagement.AverageRating
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
                            ViewCount = relatedVideo.Engagement.ViewCount,
                            LikeCount = relatedVideo.Engagement.LikeCount,
                            DislikeCount = relatedVideo.Engagement.DislikeCount,
                            AverageRating = relatedVideo.Engagement.AverageRating
                        });
                        count++;
                    }
                    catch { }
                }
            }
            catch { }

            return relatedVideos;
        }



        public async IAsyncEnumerable<VideoInfo> GetPlaylistVideosAsync(string playlistUrl)
        {
            // İsterseniz playlist bilgisini gerçekten kullanıyorsanız açın, yoksa kaldırın:
            
            await foreach (var video in _youtubeClient.Playlists.GetVideosAsync(playlistUrl))
            {
                // Thumbnail'i güvenli biçimde seç
                var thumbnail = video.Thumbnails.OrderByDescending(t => t.Resolution.Area).FirstOrDefault();

                VideoInfo videoInfo;

                try
                {
                    // PlaylistVideo'da eksik alanlar olabilir; tam video bilgisi çek
                    var fullVideo = await _youtubeClient.Videos.GetAsync(video.Id);

                    // Engagement çıkarıldıysa sadece mevcut alanları kullanın
                    videoInfo = new VideoInfo
                    {
                        Id = fullVideo.Id,
                        Title = fullVideo.Title,
                        Author = fullVideo.Author.ChannelTitle,
                        ThumbnailUrl = thumbnail?.Url ?? string.Empty,
                        Duration = fullVideo.Duration,
                        Url = fullVideo.Url,
                        ViewCount = fullVideo.Engagement.ViewCount,
                        LikeCount = fullVideo.Engagement.LikeCount,
                        DislikeCount = fullVideo.Engagement.DislikeCount,
                        AverageRating = fullVideo.Engagement.AverageRating
                    };
                }
                catch
                {
                    // Hata durumunda temel bilgileri dön
                    videoInfo = new VideoInfo
                    {
                        Id = video.Id,
                        Title = video.Title,
                        Author = video.Author?.ChannelTitle ?? video.Author?.Title ?? string.Empty,
                        ThumbnailUrl = thumbnail?.Url ?? string.Empty,
                        Duration = video.Duration,
                        Url = video.Url
                    };
                }

                yield return videoInfo;
            }
        }


        //public async IAsyncEnumerable<VideoInfo> GetPlaylistVideosAsync(string playlistUrl)
        //{
        //    try
        //    {
        //        //  var playlist = await _youtubeClient.Playlists.GetAsync(playlistUrl);

        //        await foreach (var video in _youtubeClient.Playlists.GetVideosAsync(playlistUrl))
        //        {
        //            var thumbnail = video.Thumbnails.OrderByDescending(t => t.Resolution.Area).FirstOrDefault();

        //            // PlaylistVideo'da Engagement yok, video bilgisini tekrar çek
        //            VideoInfo videoInfo;
        //            try
        //            {
        //                var fullVideo = await _youtubeClient.Videos.GetAsync(video.Id);
        //                videoInfo = new VideoInfo
        //                {
        //                    Id = fullVideo.Id,
        //                    Title = fullVideo.Title,
        //                    Author = fullVideo.Author.ChannelTitle,
        //                    ThumbnailUrl = thumbnail?.Url ?? string.Empty,
        //                    Duration = fullVideo.Duration,
        //                    Url = fullVideo.Url,
        //                    ViewCount = fullVideo.Engagement.ViewCount,
        //                    LikeCount = fullVideo.Engagement.LikeCount,
        //                    DislikeCount = fullVideo.Engagement.DislikeCount,
        //                    AverageRating = fullVideo.Engagement.AverageRating
        //                };
        //            }
        //            catch
        //            {
        //                // Hata durumunda sadece temel bilgileri kullan
        //                videoInfo = new VideoInfo
        //                {
        //                    Id = video.Id,
        //                    Title = video.Title,
        //                    Author = video.Author.ChannelTitle,
        //                    ThumbnailUrl = thumbnail?.Url ?? string.Empty,
        //                    Duration = video.Duration,
        //                    Url = video.Url
        //                };
        //            }
        //            yield return videoInfo;
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        // Hata durumunda hiçbir şey döndürme
        //        yield break;
        //    }
        //}

        public async Task<Stream> GetVideoStreamAsync(string videoId)
        {
            var streamManifest = await _youtubeClient.Videos.Streams.GetManifestAsync(videoId);
            var audioStream = streamManifest.GetAudioOnlyStreams().OrderByDescending(s => s.Bitrate).FirstOrDefault();
            if (audioStream == null)
                throw new Exception("Audio stream bulunamadı");
            return await _youtubeClient.Videos.Streams.GetAsync(audioStream);
        }

        public IAsyncEnumerable<YoutubeExplode.Search.VideoSearchResult> GetSearchStreamAsync(string query)
        {
            return _youtubeClient.Search.GetVideosAsync(query);
        }

        public async Task<YoutubeExplode.Channels.Channel> GetChannelByHandleOrUrlAsync(string channelInput)
        {
            try
            {
                // URL ise parse et
                if (channelInput.Contains("youtube.com") || channelInput.Contains("youtu.be"))
                {
                    return await _youtubeClient.Channels.GetByHandleAsync(channelInput);
                }
                else
                {
                    // Handle veya isim ile ara
                    return await _youtubeClient.Channels.GetByHandleAsync(channelInput);
                }
            }
            catch
            {
                // Handle ile bulamazsa, arama yap
                await foreach (var result in _youtubeClient.Search.GetChannelsAsync(channelInput))
                {
                    try
                    {
                        return await _youtubeClient.Channels.GetAsync(result.Id);
                    }
                    catch { }
                }
                throw new Exception("Kanal bulunamadı");
            }
        }

        public async Task<List<PlaylistInfo>> GetChannelPlaylistsAsync(string channelId)
        {
            var playlists = new List<PlaylistInfo>();

            try
            {
                var channel = await _youtubeClient.Channels.GetAsync(channelId);


                //  [LÖ] Ben yaptım bunları hızlı bypass
                //var videos = await _youtubeClient.Channels.GetUploadsAsync(channel.Id);
                //playlists.Add(new PlaylistInfo
                //{
                //    Id = "001",
                //    Title = "All Videos",
                //    ChannelName = channel.Title,
                //    ThumbnailUrl = channel.Thumbnails?.OrderByDescending(t => t.Resolution.Area).FirstOrDefault()?.Url,
                //    Url = $"https://www.youtube.com/channel/{channel.Id}/videos",
                //    VideoCount = videos.Count()
                //});
                //return playlists;


                // Kanalın uploads playlist'ini al (kanalın tüm videoları)
                // Not: GetUploadsAsync bir PlaylistId döndürür, ancak bu API'de direkt kullanılamaz
                // Bu yüzden sadece arama ile playlist'leri buluyoruz

                // Kanalın diğer playlist'lerini arama ile bul
                var searchQuery = $"channel:{channel.Title} playlist";
                var count = 0;
                await foreach (var result in _youtubeClient.Search.GetPlaylistsAsync(searchQuery))
                {
                    if (count >= 20) break; // Maksimum 20 playlist

                    try
                    {
                        var playlistDetails = await _youtubeClient.Playlists.GetAsync(result.Id);
                        // Playlist'in bu kanala ait olduğunu kontrol et
                        if (playlistDetails?.Author?.ChannelId == channelId ||
                            playlistDetails?.Author?.ChannelTitle == channel.Title ||
                            playlistDetails?.Author?.ChannelUrl == channel.Url)
                        {
                            // Video sayısını say
                            var videoCount = 0;
                            await foreach (var _ in _youtubeClient.Playlists.GetVideosAsync(result.Id))
                            {
                                videoCount++;
                            }

                            playlists.Add(new PlaylistInfo
                            {
                                Id = result.Id,
                                Title = result.Title,
                                Url = result.Url,
                                ThumbnailUrl = result.Thumbnails?.OrderByDescending(t => t.Resolution.Area).FirstOrDefault()?.Url,
                                VideoCount = videoCount,
                                ChannelName = channel.Title
                            });
                            count++;
                        }
                    }
                    catch { }
                }
            }
            catch { }

            return playlists;
        }

        public async Task<List<VideoInfo>> GetPlaylistVideosFastAsync(string playlistUrl)
        {
            var videos = new List<VideoInfo>();

            try
            {
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
    }
}

