using FFMpegCore;
using Google.Apis.Download;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Diagnostics;
using System.Text.RegularExpressions;
using YoutubeExplode;
using YoutubeExplode.Common;
using YoutubeExplode.Playlists;
using YoutubeExplode.Videos.Streams;
using YoutubeSearcher.Web.Models;
using YoutubeSearcher.Web.Services;

namespace YoutubeSearcher.Web.Controllers
{
    public class DownloadController : Controller
    {
        private readonly DownloadService _downloadService;
        private readonly YoutubeService _youtubeService;
        private readonly ILogger<DownloadController> _logger;

        public DownloadController(DownloadService downloadService, YoutubeService youtubeService, ILogger<DownloadController> logger)
        {
            _downloadService = downloadService;
            _youtubeService = youtubeService;
            _logger = logger;
        }

        [HttpPost]
        public async Task<IActionResult> DownloadSingle(string videoId, string format = "mp3", string searchQuery = "")
        {
            try
            {
                var videoInfo = await _youtubeService.GetVideoInfoAsync(videoId);
                if (videoInfo == null)
                {
                    return Json(new { success = false, message = "Video bulunamadı" });
                }

                // İndirme işlemi arka planda çalışacak
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await _downloadService.DownloadVideoAsync(videoInfo, format, searchQuery);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "İndirme hatası: {VideoId}", videoId);
                    }
                });

                return Json(new { success = true, message = "İndirme başlatıldı" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "İndirme başlatma hatası");
                return Json(new { success = false, message = $"Hata: {ex.Message}" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> DownloadMultiple([FromBody] DownloadRequest request)
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
                        await _downloadService.DownloadVideosAsync(videos, request.Format ?? "mp3", request.SearchQuery ?? "");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Toplu indirme hatası");
                    }
                });

                return Json(new { success = true, message = $"{videos.Count} video indirme kuyruğuna eklendi" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Toplu indirme başlatma hatası");
                return Json(new { success = false, message = $"Hata: {ex.Message}" });
            }
        }

        [HttpGet]
        public IActionResult GetDownloadPath()
        {
            var path = _downloadService.GetDownloadPath();
            return Json(new { path = path });
        }

        [HttpGet]
        public async Task<IActionResult> Test()
        {

            //  var url = "https://www.youtube.com/watch?v=Ch6xdV_ZjdU&list=RDMMCh6xdV_ZjdU&index=1";
            //  var url = "https://www.youtube.com/watch?v=49Kh1mS4Fhs&list=PLzdbroo8EhDvW9CPUMhb68RI3FyxkVy3M";
            //  var url = "https://www.youtube.com/watch?v=aRsi3SHma6c&list=RDCLAK5uy_nkN2Fde5lIJN38ta7Tyvr8Uona03aHnRo&index=2";
            var url = "https://www.youtube.com/watch?v=F0d8JJUNkqo&list=PL4fGSI1pDJn5kI81J1fYWK5eZRl1zJ5kM&index=5";

            var maxDegree = Environment.ProcessorCount; // Sizin istediğiniz eşzamanlılık
            var limiter = new SemaphoreSlim(maxDegree);
            var tasks = new List<Task>();

            using var youtube = new YoutubeClient();
            var outputDir = @"C:\Users\Meyra\Music\YoutubeSearcher\_test\";
            Directory.CreateDirectory(outputDir);

            var videos = await youtube.Playlists.GetVideosAsync(url);

            foreach (var video in videos)
            {
                // Her iterasyonda başlayacak gerçek bir Task yaratıyoruz
                tasks.Add(Task.Run(async () =>
                {
                    await limiter.WaitAsync();
                    try
                    {
                        await ProcessVideoAsync(video, youtube, outputDir);
                    }
                    catch { }
                    finally
                    {
                        limiter.Release();
                    }
                }));
            }

            await Task.WhenAll(tasks);

            return Json(new { success = true, message = $"{videos.Count} video indirme tamamlandı" });

        }

        static async Task ProcessVideoAsync(PlaylistVideo video, YoutubeClient youtube, string outputDir)
        {
            // Manifest ve en yüksek bitrateli ses stream'i
            var streamManifest = await youtube.Videos.Streams.GetManifestAsync(video.Url);
            var streamInfo = streamManifest.GetAudioOnlyStreams()
                                           .OrderByDescending(s => s.Bitrate)
                                           .FirstOrDefault();

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
                var bytes = await http.GetByteArrayAsync(video.Thumbnails.GetWithHighestResolution().Url);

                // İçerik tipine göre uzantı belirle (varsayılan jpg)
                // Not: Head isteğiyle content-type çekilebilir; pratikte jpg veya png çoğu durumda yeterli.
                string ext = ".jpg";
                try
                {
                    using var resp = await http.GetAsync(video.Thumbnails.GetWithHighestResolution().Url, HttpCompletionOption.ResponseHeadersRead);
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

    }



    public class DownloadRequest
    {
        public List<string> VideoIds { get; set; } = new();
        public string? Format { get; set; } = "mp3";
        public string? SearchQuery { get; set; } = "";
    }
}

