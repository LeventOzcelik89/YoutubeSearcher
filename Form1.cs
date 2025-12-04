using YoutubeSearcher.Models;
using YoutubeSearcher.Services;
using YoutubeSearcher.Controls;

namespace YoutubeSearcher
{
    public partial class Form1 : Form
    {
        private readonly YoutubeService _youtubeService;
        private readonly DownloadService _downloadService;
        private readonly SearchService _searchService;
        
        private VideoInfo? _currentMainVideo;
        private List<VideoInfo> _relatedVideos = new();
        private List<VideoInfo> _searchResults = new();

        public Form1()
        {
            InitializeComponent();
            
            _youtubeService = new YoutubeService();
            _downloadService = new DownloadService();
            _searchService = new SearchService(_youtubeService);
            
            UpdateUI();
        }

        private async void BtnSearch_Click(object? sender, EventArgs e)
        {
            await PerformSearch();
        }

        private async void TxtSearch_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                await PerformSearch();
            }
        }

        private async Task PerformSearch()
        {
            var query = txtSearch.Text.Trim();
            if (string.IsNullOrEmpty(query))
                return;

            try
            {
                SetLoading(true);
                lblStatus.Text = "Aranıyor...";

                // Ana arama ve benzer şarkılar
                var (mainVideo, relatedVideos) = await _searchService.SearchWithRelatedAsync(query);
                
                _currentMainVideo = mainVideo;
                _relatedVideos = relatedVideos;

                // Arama sonuçları listesi
                _searchResults = await _youtubeService.SearchVideosAsync(query, 10);

                UpdateUI();
                lblStatus.Text = "Arama tamamlandı";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Arama hatası: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                lblStatus.Text = "Arama hatası";
            }
            finally
            {
                SetLoading(false);
            }
        }

        private void UpdateUI()
        {
            // Ana video gösterimi
            if (_currentMainVideo != null)
            {
                lblMainVideoTitle.Text = $"{_currentMainVideo.Title}\n\n{_currentMainVideo.Author}\nSüre: {_currentMainVideo.Duration?.ToString(@"mm\:ss") ?? "N/A"}";
                LoadThumbnail(pictureBoxMainThumbnail, _currentMainVideo.ThumbnailUrl);
                panelMainVideo.Visible = true;
            }
            else
            {
                panelMainVideo.Visible = false;
            }

            // Benzer şarkılar
            flowPanelRelated.Controls.Clear();
            foreach (var video in _relatedVideos)
            {
                var control = new VideoItemControl(video);
                control.DownloadClicked += Control_DownloadClicked;
                control.PreviewClicked += Control_PreviewClicked;
                control.SelectionChanged += Control_SelectionChanged;
                flowPanelRelated.Controls.Add(control);
            }

            // Arama sonuçları
            flowPanelSearchResults.Controls.Clear();
            foreach (var video in _searchResults)
            {
                var control = new VideoItemControl(video);
                control.DownloadClicked += Control_DownloadClicked;
                control.PreviewClicked += Control_PreviewClicked;
                control.SelectionChanged += Control_SelectionChanged;
                flowPanelSearchResults.Controls.Add(control);
            }
        }

        private void LoadThumbnail(PictureBox pictureBox, string url)
        {
            Task.Run(async () =>
            {
                try
                {
                    using var httpClient = new HttpClient();
                    var imageBytes = await httpClient.GetByteArrayAsync(url);
                    using var ms = new MemoryStream(imageBytes);
                    var image = Image.FromStream(ms);
                    
                    if (InvokeRequired)
                        Invoke(() => pictureBox.Image = image);
                    else
                        pictureBox.Image = image;
                }
                catch { }
            });
        }

        private async void Control_DownloadClicked(object? sender, VideoInfo videoInfo)
        {
            await DownloadVideo(videoInfo);
        }

        private void Control_PreviewClicked(object? sender, VideoInfo videoInfo)
        {
            PreviewVideo(videoInfo);
        }

        private void Control_SelectionChanged(object? sender, bool isSelected)
        {
            // Checkbox değişikliği - gerekirse işlem yapılabilir
        }

        private async void BtnDownloadMain_Click(object? sender, EventArgs e)
        {
            if (_currentMainVideo != null)
            {
                await DownloadVideo(_currentMainVideo);
            }
        }

        private void BtnPreviewMain_Click(object? sender, EventArgs e)
        {
            if (_currentMainVideo != null)
            {
                PreviewVideo(_currentMainVideo);
            }
        }

        private async void BtnDownloadSelected_Click(object? sender, EventArgs e)
        {
            var selectedVideos = _relatedVideos.Where(v => v.IsSelected).ToList();
            
            if (selectedVideos.Count == 0)
            {
                MessageBox.Show("Lütfen indirmek için en az bir şarkı seçin.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            await DownloadVideos(selectedVideos);
        }

        private async Task DownloadVideo(VideoInfo videoInfo)
        {
            try
            {
                SetLoading(true);
                lblStatus.Text = $"İndiriliyor: {videoInfo.Title}";
                
                var format = radioMp3.Checked ? "mp3" : "mp4";
                var progress = new Progress<double>(p =>
                {
                    if (InvokeRequired)
                        Invoke(() => progressBar.Value = (int)(p * 100));
                    else
                        progressBar.Value = (int)(p * 100);
                });

                var filePath = await _downloadService.DownloadVideoAsync(videoInfo, format, progress);
                
                lblStatus.Text = $"İndirildi: {Path.GetFileName(filePath)}";
                MessageBox.Show($"İndirme tamamlandı!\n\n{filePath}", "Başarılı", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"İndirme hatası: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                lblStatus.Text = "İndirme hatası";
            }
            finally
            {
                SetLoading(false);
                progressBar.Value = 0;
            }
        }

        private async Task DownloadVideos(List<VideoInfo> videos)
        {
            try
            {
                SetLoading(true);
                lblStatus.Text = $"{videos.Count} şarkı indiriliyor...";
                
                var format = radioMp3.Checked ? "mp3" : "mp4";
                var progress = new Progress<(int current, int total)>(p =>
                {
                    if (InvokeRequired)
                    {
                        Invoke(() =>
                        {
                            progressBar.Maximum = p.total;
                            progressBar.Value = p.current;
                            lblStatus.Text = $"İndiriliyor: {p.current}/{p.total}";
                        });
                    }
                    else
                    {
                        progressBar.Maximum = p.total;
                        progressBar.Value = p.current;
                        lblStatus.Text = $"İndiriliyor: {p.current}/{p.total}";
                    }
                });

                var downloadedFiles = await _downloadService.DownloadVideosAsync(videos, format, progress);
                
                lblStatus.Text = $"{downloadedFiles.Count} şarkı başarıyla indirildi!";
                MessageBox.Show($"{downloadedFiles.Count} şarkı başarıyla indirildi!\n\nKonum: {_downloadService.GetDownloadPath()}", 
                    "Başarılı", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"İndirme hatası: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                lblStatus.Text = "İndirme hatası";
            }
            finally
            {
                SetLoading(false);
                progressBar.Value = 0;
            }
        }

        private void PreviewVideo(VideoInfo videoInfo)
        {
            try
            {
                // YouTube URL'sini varsayılan tarayıcıda aç
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = videoInfo.Url,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Önizleme hatası: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void SetLoading(bool isLoading)
        {
            btnSearch.Enabled = !isLoading;
            txtSearch.Enabled = !isLoading;
            btnDownloadMain.Enabled = !isLoading && _currentMainVideo != null;
            btnDownloadSelected.Enabled = !isLoading;
        }
    }
}
