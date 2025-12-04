namespace YoutubeSearcher.Models
{
    public class VideoInfo
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public string ThumbnailUrl { get; set; } = string.Empty;
        public TimeSpan? Duration { get; set; }
        public string Url { get; set; } = string.Empty;
        public long? ViewCount { get; set; }
        public bool IsSelected { get; set; }
    }
}

