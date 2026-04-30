using System.Text.RegularExpressions;
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
        public bool IsShort { get; set; }
        public string ShortUrl => $"https://www.youtube.com/shorts/{VideoId}";
    }

    public class YouTubeRssService
    {
        private readonly HttpClient _http;
        private readonly string _channelId;
        private readonly string _channelHandle;
        private static List<YouTubeVideo>? _cache;
        private static DateTime _cacheExpiry = DateTime.MinValue;
        private static List<YouTubeVideo>? _shortsCache;
        private static DateTime _shortsCacheExpiry = DateTime.MinValue;

        public YouTubeRssService(HttpClient http, IConfiguration config)
        {
            _http = http;
            _channelId = config["YouTube:ChannelId"] ?? "";
            _channelHandle = config["YouTube:ChannelHandle"] ?? "comunidadebatistafloramar";
        }

        public async Task<List<YouTubeVideo>> GetLatestVideosAsync(int count = 5)
        {
            var all = await GetAllAsync();
            return all.Take(count).ToList();
        }

        /// <summary>
        /// Scrape direto da aba /shorts do canal — retorna SÓ Shorts (sem fallback).
        /// </summary>
        public async Task<List<YouTubeVideo>> GetLatestShortsAsync(int count = 8)
        {
            if (_shortsCache != null && DateTime.UtcNow < _shortsCacheExpiry)
                return _shortsCache.Take(count).ToList();

            if (string.IsNullOrWhiteSpace(_channelId) || !_channelId.StartsWith("UC"))
                return new List<YouTubeVideo>();

            // YouTube tem RSS oficial pra cada playlist do sistema. Shorts = "UUSH" + channelId sem "UC"
            var shortsPlaylistId = "UUSH" + _channelId.Substring(2);
            var url = $"https://www.youtube.com/feeds/videos.xml?playlist_id={shortsPlaylistId}";

            try
            {
                using var req = new HttpRequestMessage(HttpMethod.Get, url);
                req.Headers.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
                req.Headers.AcceptLanguage.ParseAdd("pt-BR,pt;q=0.9,en;q=0.5");
                req.Headers.Accept.ParseAdd("application/atom+xml,application/xml,text/xml;q=0.9");
                _http.Timeout = TimeSpan.FromSeconds(15);

                using var resp = await _http.SendAsync(req);
                Console.WriteLine($"[Shorts RSS] {url} → {(int)resp.StatusCode}");

                if (!resp.IsSuccessStatusCode)
                {
                    _shortsCache = new List<YouTubeVideo>();
                    _shortsCacheExpiry = DateTime.UtcNow.AddMinutes(10); // cache curto pra retry
                    return _shortsCache;
                }

                var xml = await resp.Content.ReadAsStringAsync();
                Console.WriteLine($"[Shorts RSS] XML length: {xml.Length}");

                XNamespace atom = "http://www.w3.org/2005/Atom";
                XNamespace yt = "http://www.youtube.com/xml/schemas/2015";
                XNamespace media = "http://search.yahoo.com/mrss/";

                var doc = XDocument.Parse(xml);
                var shorts = doc.Descendants(atom + "entry")
                    .Take(count)
                    .Select(e =>
                    {
                        var videoId = e.Element(yt + "videoId")?.Value ?? "";
                        return new YouTubeVideo
                        {
                            VideoId = videoId,
                            Title = e.Element(atom + "title")?.Value ?? "",
                            VideoUrl = $"https://www.youtube.com/shorts/{videoId}",
                            ThumbnailUrl = e.Descendants(media + "thumbnail").FirstOrDefault()?.Attribute("url")?.Value
                                           ?? $"https://i.ytimg.com/vi/{videoId}/hqdefault.jpg",
                            PublishedAt = DateTime.TryParse(e.Element(atom + "published")?.Value, out var dt) ? dt : DateTime.UtcNow,
                            IsShort = true
                        };
                    })
                    .ToList();

                Console.WriteLine($"[Shorts RSS] Parsed {shorts.Count} shorts");
                _shortsCache = shorts;
                _shortsCacheExpiry = DateTime.UtcNow.AddHours(1);
                return shorts;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Shorts RSS] ERROR: {ex.Message}");
                return _shortsCache ?? new List<YouTubeVideo>();
            }
        }


        private async Task<List<YouTubeVideo>> GetAllAsync()
        {
            if (_cache != null && DateTime.UtcNow < _cacheExpiry)
                return _cache;

            if (string.IsNullOrWhiteSpace(_channelId))
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
                    .Select(e =>
                    {
                        var videoId = e.Element(yt + "videoId")?.Value ?? "";
                        var title = e.Element(atom + "title")?.Value ?? "";
                        var titleLower = title.ToLowerInvariant();
                        var isShort = titleLower.Contains("#shorts")
                                      || titleLower.Contains("#short")
                                      || titleLower.Contains(" shorts")
                                      || titleLower.StartsWith("shorts");
                        return new YouTubeVideo
                        {
                            VideoId = videoId,
                            Title = title,
                            VideoUrl = $"https://www.youtube.com/watch?v={videoId}",
                            ThumbnailUrl = e.Descendants(media + "thumbnail").FirstOrDefault()?.Attribute("url")?.Value
                                           ?? $"https://i.ytimg.com/vi/{videoId}/hqdefault.jpg",
                            PublishedAt = DateTime.TryParse(e.Element(atom + "published")?.Value, out var dt) ? dt : DateTime.UtcNow,
                            IsShort = isShort
                        };
                    })
                    .ToList();

                _cache = videos;
                _cacheExpiry = DateTime.UtcNow.AddHours(1);
                return videos;
            }
            catch
            {
                return _cache ?? new List<YouTubeVideo>();
            }
        }
    }
}
