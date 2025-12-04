using FFMpegCore;
using Microsoft.AspNetCore.Mvc;
using System.Runtime.CompilerServices;
using YoutubeExplode;
using YoutubeExplode.Playlists;
using YoutubeSearcher.Web.Models;
using YoutubeSearcher.Web.Services;

namespace YoutubeSearcher.Web.Controllers
{
    public class ChannelController : Controller
    {
        private readonly YoutubeClient _youtubeClient;
        private readonly YoutubeService _youtubeService;
        private readonly DownloadService _downloadService;
        private readonly ILogger<ChannelController> _logger;

        public ChannelController(YoutubeService youtubeService, DownloadService downloadService, ILogger<ChannelController> logger)
        {
            _youtubeClient = new YoutubeClient();
            _youtubeService = youtubeService;
            _downloadService = downloadService;
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Playlist()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> SearchPlaylist(string playlist)
        {

            if (string.IsNullOrWhiteSpace(playlist))
            {
                return Json(new { success = false, message = "Playlist url boş olamaz" });
            }

            var playlistDetail = await _youtubeClient.Playlists.GetAsync(playlist);

            var taskList = new List<Task>();
            using var youtubeClient = new YoutubeClient();
            var playlistName = string.Join("_", playlistDetail.Title.Split(Path.GetInvalidFileNameChars()));
            var outputDir = @"C:\Users\Meyra\Music\YoutubeSearcher\Playlists\";

            await foreach (var video in _youtubeService.GetPlaylistVideosAsync(playlist))
            {
                taskList.Add(Task.Run(() => ProcessVideoAsync(video, youtubeClient, outputDir, playlistName)));
            }

            await Task.WhenAll(taskList);
            return Json(new { success = true });
        }

        static async Task ProcessVideoAsync(VideoInfo video, YoutubeClient youtube, string outputDir, string playlistName)
        {
            // Manifest ve en yüksek bitrateli ses stream'i
            var streamManifest = await youtube.Videos.Streams.GetManifestAsync(video.Url);
            var streamInfo = streamManifest.GetAudioOnlyStreams()
                                           .OrderByDescending(s => s.Bitrate)
                                           .FirstOrDefault();

            outputDir = outputDir + @"\" + playlistName;

            if (streamInfo == null)
                throw new InvalidOperationException("Uygun ses akışı bulunamadı.");

            // Dosya adını temizle
            var safeTitle = string.Join("_", video.Title.Split(Path.GetInvalidFileNameChars()));
            var tempFile = Path.Combine(outputDir, $"{safeTitle}.{streamInfo.Container}");
            var mp3File = Path.Combine(outputDir, $"{safeTitle}.mp3");

            // İndir
            await youtube.Videos.Streams.DownloadAsync(streamInfo, tempFile);

            // FFmpeg ile MP3'e çevir
            try
            {
                // 1) Kapak görselini indir
                using var http = new HttpClient();
                var bytes = await http.GetByteArrayAsync(video.ThumbnailUrl);

                // İçerik tipine göre uzantı belirle (varsayılan jpg)
                // Not: Head isteğiyle content-type çekilebilir; pratikte jpg veya png çoğu durumda yeterli.
                string ext = ".jpg";
                try
                {
                    using var resp = await http.GetAsync(video.ThumbnailUrl, HttpCompletionOption.ResponseHeadersRead);
                    if (resp.Content.Headers.ContentType?.MediaType?.Contains("png", StringComparison.OrdinalIgnoreCase) == true)
                        ext = ".png";
                    else if (resp.Content.Headers.ContentType?.MediaType?.Contains("jpeg", StringComparison.OrdinalIgnoreCase) == true ||
                             resp.Content.Headers.ContentType?.MediaType?.Contains("jpg", StringComparison.OrdinalIgnoreCase) == true)
                        ext = ".jpg";
                }
                catch { /* sessizce geç */ }

                var tempCoverPath = Path.Combine(Path.GetTempPath(), $"cover_{Guid.NewGuid():N}{ext}");
                await System.IO.File.WriteAllBytesAsync(tempCoverPath, bytes);

                await FFMpegArguments
                    .FromFileInput(tempFile)
                    .AddFileInput(tempCoverPath)
                    .OutputToFile(mp3File, overwrite: true, options =>
                    {
                        options.ForceFormat("mp3");
                        options.WithCustomArgument("-c:a libmp3lame");
                        options.WithCustomArgument("-b:a 192k");           // bitrate örneği
                        options.WithCustomArgument($"-metadata album=\"{EscapeForFfmpeg(playlistName)}\"");
                        if (ext == ".png")
                        {
                            options.WithCustomArgument("-c:v png");
                        }
                        else
                        {
                            // varsayılanı jpg kabul ederek en yaygın uyumlu codec
                            options.WithCustomArgument("-c:v mjpeg");
                        }

                        options.WithCustomArgument("-id3v2_version 3");    // uyumluluk için ID3v2.3
                        options.WithCustomArgument("-map 0:a -map 1");     // 0: ses, 1: görsel
                        options.WithCustomArgument("-disposition:v attached_pic");

                        // İsteğe bağlı metadata
                        options.WithCustomArgument("-metadata:s:v title=\"Album cover\"");
                        options.WithCustomArgument("-metadata:s:v comment=\"Cover (front)\"");

                    })
                    .ProcessAsynchronously();
            }
            catch (Exception ffmpegEx) when (ffmpegEx.Message.Contains("ffmpeg", StringComparison.OrdinalIgnoreCase))
            {
                var errorMsg =
                    "FFmpeg bulunamadı!\n\n" +
                    "MP3 dönüşümü için FFmpeg gereklidir.\n" +
                    "1) https://ffmpeg.org/download.html adresinden indirin\n" +
                    "2) ffmpeg.exe'yi PATH'e ekleyin veya proje klasörüne koyun.\n\n" +
                    $"Hata: {ffmpegEx.Message}";
                throw new Exception(errorMsg, ffmpegEx);
            }
            finally
            {
                // Geçici dosyayı sil
                if (System.IO.File.Exists(tempFile))
                {
                    try { System.IO.File.Delete(tempFile); } catch { /* ignore */ }
                }
            }
        }


        static string EscapeForFfmpeg(string? s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            return s.Replace("\"", "\\\""); // çift tırnakları kaçır
        }


        [HttpPost]
        public async Task<IActionResult> SearchChannel(string channelInput)
        {
            if (string.IsNullOrWhiteSpace(channelInput))
            {
                return Json(new { success = false, message = "Kanal adı veya URL boş olamaz" });
            }

            try
            {
                var channel = await _youtubeService.GetChannelByHandleOrUrlAsync(channelInput);
                var playlists = await _youtubeService.GetChannelPlaylistsAsync(channel.Id);

                return Json(new
                {
                    success = true,
                    channel = new
                    {
                        id = channel.Id,
                        title = channel.Title,
                        handle = channel.Title ?? "",
                        url = channel.Url
                    },
                    playlists = playlists
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kanal arama hatası");
                return Json(new { success = false, message = $"Kanal bulunamadı: {ex.Message}" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> GetPlaylistVideos(string playlistUrl)
        {
            if (string.IsNullOrWhiteSpace(playlistUrl))
            {
                return Json(new { success = false, message = "Playlist URL boş olamaz" });
            }

            try
            {
                var videos = await _youtubeService.GetPlaylistVideosFastAsync(playlistUrl);
                return Json(new { success = true, videos = videos });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Playlist video hatası");
                return Json(new { success = false, message = $"Playlist yüklenemedi: {ex.Message}" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> DownloadPlaylistVideos([FromBody] PlaylistDownloadRequest request)
        {
            try
            {
                if (request.VideoIds == null || request.VideoIds.Count == 0)
                {
                    return Json(new { success = false, message = "Hiç video seçilmedi" });
                }

                var videos = new List<VideoInfo>();
                foreach (var videoId in request.VideoIds)
                {
                    var video = await _youtubeService.GetVideoInfoAsync(videoId);
                    if (video != null)
                    {
                        videos.Add(video);
                    }
                }

                if (videos.Count == 0)
                {
                    return Json(new { success = false, message = "Geçerli video bulunamadı" });
                }

                // Toplu indirme arka planda çalışacak
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await _downloadService.DownloadVideosAsync(
                            videos,
                            request.Format ?? "mp3",
                            null,
                            request.ChannelName ?? "",
                            request.PlaylistName ?? ""
                        );
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Playlist indirme hatası");
                    }
                });

                return Json(new { success = true, message = $"{videos.Count} video indirme kuyruğuna eklendi" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Playlist indirme başlatma hatası");
                return Json(new { success = false, message = $"Hata: {ex.Message}" });
            }
        }
    }

    public class PlaylistDownloadRequest
    {
        public List<string> VideoIds { get; set; } = new();
        public string? Format { get; set; } = "mp3";
        public string? ChannelName { get; set; }
        public string? PlaylistName { get; set; }
    }
}

