namespace YoutubeSearcher.Web.Models
{
    public class PlaylistInfo
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string? ThumbnailUrl { get; set; }
        public int VideoCount { get; set; }
        public string? ChannelName { get; set; }
    }
}

