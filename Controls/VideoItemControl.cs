using System.ComponentModel;
using YoutubeSearcher.Models;

namespace YoutubeSearcher.Controls
{
    public partial class VideoItemControl : UserControl
    {
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public VideoInfo VideoInfo { get; private set; }
        public event EventHandler<VideoInfo>? DownloadClicked;
        public event EventHandler<VideoInfo>? PreviewClicked;
        public event EventHandler<bool>? SelectionChanged;

        public VideoItemControl(VideoInfo videoInfo)
        {
            InitializeComponent();
            VideoInfo = videoInfo;
            LoadVideoInfo();
        }

        private void LoadVideoInfo()
        {
            lblTitle.Text = VideoInfo.Title;
            lblAuthor.Text = VideoInfo.Author;
            lblDuration.Text = VideoInfo.Duration?.ToString(@"mm\:ss") ?? "N/A";
            checkBoxSelect.Checked = VideoInfo.IsSelected;
            
            // Thumbnail yükleme (async)
            Task.Run(async () =>
            {
                try
                {
                    using var httpClient = new HttpClient();
                    var imageBytes = await httpClient.GetByteArrayAsync(VideoInfo.ThumbnailUrl);
                    using var ms = new MemoryStream(imageBytes);
                    var image = Image.FromStream(ms);
                    
                    if (InvokeRequired)
                        Invoke(() => pictureBoxThumbnail.Image = image);
                    else
                        pictureBoxThumbnail.Image = image;
                }
                catch { }
            });
        }

        private void btnDownload_Click(object sender, EventArgs e)
        {
            DownloadClicked?.Invoke(this, VideoInfo);
        }

        private void btnPreview_Click(object sender, EventArgs e)
        {
            PreviewClicked?.Invoke(this, VideoInfo);
        }

        private void checkBoxSelect_CheckedChanged(object sender, EventArgs e)
        {
            VideoInfo.IsSelected = checkBoxSelect.Checked;
            SelectionChanged?.Invoke(this, checkBoxSelect.Checked);
        }
    }
}

