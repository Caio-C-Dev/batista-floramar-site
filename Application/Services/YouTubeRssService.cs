using System.Xml.Linq;

namespace BatistaFloramar.Application.Services
{
    public class YouTubeVideo
    {
        public string VideoId { get; set; } = "";
        public string Title { get; set; } = "";
        public string ThumbnailUrl { get; set; } = "";
        public string VideoUrl { get; set; } = "";
        public DateTime PublishedAt { get; set; }
        public string PublishedFormatted => PublishedAt.ToString("dd MMM yyyy");
    }

    public class YouTubeRssService
    {
        private readonly HttpClient _http;
        private readonly string _channelId;
        private static List<YouTubeVideo>? _cache;
        private static DateTime _cacheExpiry = DateTime.MinValue;

        public YouTubeRssService(HttpClient http, IConfiguration config)
        {
            _http = http;
            _channelId = config["YouTube:ChannelId"] ?? "";
        }

        public async Task<List<YouTubeVideo>> GetLatestVideosAsync(int count = 5)
        {
            if (_cache != null && DateTime.UtcNow < _cacheExpiry)
                return _cache;

            if (string.IsNullOrWhiteSpace(_channelId) || _channelId == "UCd5R5bNSpiIk2Swx2KWSxlQ")
                return new List<YouTubeVideo>();

            try
            {
                var url = $"https://www.youtube.com/feeds/videos.xml?channel_id={_channelId}";
                var response = await _http.GetAsync(url);
                if (!response.IsSuccessStatusCode)
                    return _cache ?? new List<YouTubeVideo>();
                var xml = await response.Content.ReadAsStringAsync();

                XNamespace atom = "http://www.w3.org/2005/Atom";
                XNamespace yt = "http://www.youtube.com/xml/schemas/2015";
                XNamespace media = "http://search.yahoo.com/mrss/";

                var doc = XDocument.Parse(xml);
                var videos = doc.Descendants(atom + "entry")
                    .Take(count)
                    .Select(e =>
                    {
                        var videoId = e.Element(yt + "videoId")?.Value ?? "";
                        return new YouTubeVideo
                        {
                            VideoId = videoId,
                            Title = e.Element(atom + "title")?.Value ?? "",
                            VideoUrl = $"https://www.youtube.com/watch?v={videoId}",
                            ThumbnailUrl = e.Descendants(media + "thumbnail").FirstOrDefault()?.Attribute("url")?.Value
                                           ?? $"https://i.ytimg.com/vi/{videoId}/hqdefault.jpg",
                            PublishedAt = DateTime.TryParse(e.Element(atom + "published")?.Value, out var dt) ? dt : DateTime.UtcNow
                        };
                    })
                    .ToList();

                _cache = videos;
                _cacheExpiry = DateTime.UtcNow.AddHours(1);
                return videos;
            }
            catch
            {
                return new List<YouTubeVideo>();
            }
        }
    }
}
