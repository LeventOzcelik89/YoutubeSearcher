using YoutubeExplode;
using YoutubeExplode.Videos;
using YoutubeExplode.Videos.Streams;
using YoutubeSearcher.Web.Models;
using FFMpegCore;
using FFMpegCore.Enums;

namespace YoutubeSearcher.Web.Services
{
    public class DownloadService
    {
        private readonly YoutubeClient _youtubeClient;
        private readonly string _downloadPath;

        public DownloadService()
        {
            _youtubeClient = new YoutubeClient();
            _downloadPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyMusic), "YoutubeSearcher");
            
            if (!Directory.Exists(_downloadPath))
                Directory.CreateDirectory(_downloadPath);
            
            // FFmpeg yolunu ayarla
            SetupFFmpeg();
        }
        
        private void SetupFFmpeg()
        {
            try
            {
                // Önce sistem PATH'inde FFmpeg'i kontrol et
                var pathEnv = Environment.GetEnvironmentVariable("PATH") ?? "";
                var paths = pathEnv.Split(Path.PathSeparator);
                
                foreach (var path in paths)
                {
                    var ffmpegPath = Path.Combine(path, "ffmpeg.exe");
                    if (File.Exists(ffmpegPath))
                    {
                        // FFmpeg bulundu, GlobalFFOptions'ı ayarla
                        GlobalFFOptions.Configure(new FFOptions { BinaryFolder = path });
                        return;
                    }
                }
                
                // FFmpeg bulunamadı - kullanıcıya bilgi verilecek
            }
            catch
            {
                // Hata durumunda devam et
            }
        }

        private string GetDownloadPathForQuery(string? searchQuery, string? channelName = null, string? playlistName = null)
        {
            var basePath = _downloadPath;

            // Kanal ve playlist varsa: kanaladi/playlistadi
            if (!string.IsNullOrWhiteSpace(channelName) && !string.IsNullOrWhiteSpace(playlistName))
            {
                var sanitizedChannel = SanitizeFileName(channelName);
                var sanitizedPlaylist = SanitizeFileName(playlistName);
                basePath = Path.Combine(_downloadPath, sanitizedChannel, sanitizedPlaylist);
            }
            // Sadece arama terimi varsa
            else if (!string.IsNullOrWhiteSpace(searchQuery))
            {
                var sanitizedQuery = SanitizeFileName(searchQuery);
                basePath = Path.Combine(_downloadPath, sanitizedQuery);
            }

            if (!Directory.Exists(basePath))
                Directory.CreateDirectory(basePath);
            
            return basePath;
        }

        public async Task<string> DownloadVideoAsync(VideoInfo videoInfo, string format = "mp3", string? searchQuery = null, string? channelName = null, string? playlistName = null, IProgress<double>? progress = null)
        {
            try
            {
                var downloadPath = GetDownloadPathForQuery(searchQuery, channelName, playlistName);
                var sanitizedTitle = SanitizeFileName(videoInfo.Title);
                var fileName = $"{sanitizedTitle}.{format}";
                var filePath = Path.Combine(downloadPath, fileName);

                if (File.Exists(filePath))
                {
                    var counter = 1;
                    var nameWithoutExt = Path.GetFileNameWithoutExtension(fileName);
                    var ext = Path.GetExtension(fileName);
                    do
                    {
                        fileName = $"{nameWithoutExt} ({counter}){ext}";
                        filePath = Path.Combine(downloadPath, fileName);
                        counter++;
                    } while (File.Exists(filePath));
                }

                if (format == "mp3")
                {
                    // MP3 için: Audio stream indir, sonra FFMpeg ile MP3'e çevir
                    // URL'den direkt VideoId parse et (daha güvenilir)
                    VideoId videoId;
                    if (!string.IsNullOrEmpty(videoInfo.Url) && (videoInfo.Url.Contains("youtube.com") || videoInfo.Url.Contains("youtu.be")))
                    {
                        videoId = VideoId.Parse(videoInfo.Url);
                    }
                    else if (!string.IsNullOrEmpty(videoInfo.Id))
                    {
                        videoId = VideoId.Parse(videoInfo.Id);
                    }
                    else
                    {
                        throw new Exception("Geçersiz video ID veya URL");
                    }
                    
                    // Stream manifest al - cipher hatası için farklı yaklaşım
                    StreamManifest streamManifest;
                    try
                    {
                        // Önce normal şekilde dene
                        streamManifest = await _youtubeClient.Videos.Streams.GetManifestAsync(videoId);
                    }
                    catch (YoutubeExplode.Exceptions.YoutubeExplodeException)
                    {
                        // Cipher hatası alırsak, video bilgilerini tekrar al ve tekrar dene
                        try
                        {
                            var video = await _youtubeClient.Videos.GetAsync(videoId);
                            await Task.Delay(500); // Kısa bekleme
                            streamManifest = await _youtubeClient.Videos.Streams.GetManifestAsync(videoId);
                        }
                        catch
                        {
                            throw new Exception("Bu video şu anda indirilemiyor. YouTube'un şifreleme mekanizması nedeniyle erişilemiyor. Lütfen daha sonra tekrar deneyin veya farklı bir video deneyin.");
                        }
                    }
                    
                    var audioStreamInfo = streamManifest.GetAudioOnlyStreams()
                        .OrderByDescending(s => s.Bitrate)
                        .FirstOrDefault();
                    
                    if (audioStreamInfo == null)
                        throw new Exception("Audio stream bulunamadı");

                    // Geçici dosya (orijinal format)
                    var tempExtension = audioStreamInfo.Container.Name;
                    var tempFile = Path.ChangeExtension(filePath, tempExtension);
                    
                    try
                    {
                        // Audio stream'i geçici dosyaya indir
                        var downloadProgress = new Progress<double>(p =>
                        {
                            // İndirme %70, conversion %30 olarak hesapla
                            progress?.Report(p * 0.7);
                        });
                        
                        await _youtubeClient.Videos.Streams.DownloadAsync(audioStreamInfo, tempFile, downloadProgress);
                        
                        progress?.Report(0.7);
                        
                        // FFMpeg ile MP3'e çevir
                        try
                        {
                            await FFMpegArguments
                                .FromFileInput(tempFile)
                                .OutputToFile(filePath, overwrite: true, options => 
                                {
                                    options.ForceFormat("mp3");
                                })
                                .ProcessAsynchronously();
                        }
                        catch (Exception ffmpegEx) when (ffmpegEx.Message.Contains("ffmpeg") || ffmpegEx.Message.Contains("FFmpeg"))
                        {
                            // FFmpeg bulunamadı - kullanıcıya detaylı bilgi ver
                            var errorMsg = "FFmpeg bulunamadı!\n\n" +
                                         "MP3 dönüşümü için FFmpeg gereklidir.\n\n" +
                                         "Çözüm:\n" +
                                         "1. FFmpeg'i indirin: https://ffmpeg.org/download.html\n" +
                                         "2. ffmpeg.exe dosyasını sistem PATH'ine ekleyin\n" +
                                         "   veya\n" +
                                         "3. FFmpeg'i proje klasörüne koyun\n\n" +
                                         $"Hata: {ffmpegEx.Message}";
                            throw new Exception(errorMsg, ffmpegEx);
                        }
                        
                        progress?.Report(1.0);
                    }
                    finally
                    {
                        // Geçici dosyayı sil
                        if (File.Exists(tempFile) && tempFile != filePath)
                        {
                            try 
                            { 
                                File.Delete(tempFile); 
                            } 
                            catch { }
                        }
                    }
                }
                else
                {
                    // Video için direkt stream indir
                    VideoId videoId;
                    if (!string.IsNullOrEmpty(videoInfo.Url) && (videoInfo.Url.Contains("youtube.com") || videoInfo.Url.Contains("youtu.be")))
                    {
                        videoId = VideoId.Parse(videoInfo.Url);
                    }
                    else if (!string.IsNullOrEmpty(videoInfo.Id))
                    {
                        videoId = VideoId.Parse(videoInfo.Id);
                    }
                    else
                    {
                        throw new Exception("Geçersiz video ID veya URL");
                    }
                    
                    StreamManifest streamManifest;
                    try
                    {
                        streamManifest = await _youtubeClient.Videos.Streams.GetManifestAsync(videoId);
                    }
                    catch (YoutubeExplode.Exceptions.YoutubeExplodeException)
                    {
                        try
                        {
                            var video = await _youtubeClient.Videos.GetAsync(videoId);
                            await Task.Delay(500);
                            streamManifest = await _youtubeClient.Videos.Streams.GetManifestAsync(videoId);
                        }
                        catch
                        {
                            throw new Exception("Bu video şu anda indirilemiyor. YouTube'un şifreleme mekanizması nedeniyle erişilemiyor. Lütfen daha sonra tekrar deneyin veya farklı bir video deneyin.");
                        }
                    }
                    var videoStreamInfo = streamManifest.GetMuxedStreams()
                        .OrderByDescending(s => s.VideoQuality)
                        .FirstOrDefault();
                    
                    if (videoStreamInfo == null)
                    {
                        // Muxed yoksa video+audio birleştir
                        var videoOnly = streamManifest.GetVideoOnlyStreams()
                            .Where(s => s.Container.Name == "mp4")
                            .OrderByDescending(s => s.VideoQuality)
                            .FirstOrDefault();
                        var audioOnly = streamManifest.GetAudioOnlyStreams()
                            .OrderByDescending(s => s.Bitrate)
                            .FirstOrDefault();
                        
                        if (videoOnly == null || audioOnly == null)
                            throw new Exception("Video stream bulunamadı");
                        
                        // Video ve audio'yu birleştir (basitleştirilmiş - sadece video indir)
                        await _youtubeClient.Videos.Streams.DownloadAsync(videoOnly, filePath, progress);
                    }
                    else
                    {
                        await _youtubeClient.Videos.Streams.DownloadAsync(videoStreamInfo, filePath, progress);
                    }
                }

                return filePath;
            }
            catch (Exception ex)
            {
                throw new Exception($"İndirme hatası: {ex.Message}");
            }
        }

        public async Task<List<string>> DownloadVideosAsync(List<VideoInfo> videos, string format = "mp3", string? searchQuery = null, string? channelName = null, string? playlistName = null, IProgress<(int current, int total)>? progress = null)
        {
            var downloadedFiles = new List<string>();
            var total = videos.Count;
            var current = 0;

            foreach (var video in videos)
            {
                try
                {
                    var progressReporter = new Progress<double>(p =>
                    {
                        progress?.Report((current, total));
                    });

                    var filePath = await DownloadVideoAsync(video, format, searchQuery, channelName, playlistName, progressReporter);
                    downloadedFiles.Add(filePath);
                }
                catch
                {
                    // Hata loglanabilir
                }
                finally
                {
                    current++;
                    progress?.Report((current, total));
                }
            }

            return downloadedFiles;
        }

        private string SanitizeFileName(string fileName)
        {
            var invalidChars = Path.GetInvalidFileNameChars();
            return string.Join("_", fileName.Split(invalidChars, StringSplitOptions.RemoveEmptyEntries))
                .TrimEnd('.');
        }

        public string GetDownloadPath() => _downloadPath;
    }
}

