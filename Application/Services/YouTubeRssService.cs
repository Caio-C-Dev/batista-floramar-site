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

        // Default hardcoded — garante funcionamento mesmo sem env var/appsettings em prod.
        private const string DEFAULT_CHANNEL_ID = "UCd5R5bNSpiIk2Swx2KWSxlQ";
        private const string DEFAULT_CHANNEL_HANDLE = "comunidadebatistafloramar";

        public YouTubeRssService(HttpClient http, IConfiguration config)
        {
            _http = http;
            var cid = config["YouTube:ChannelId"];
            _channelId = string.IsNullOrWhiteSpace(cid) ? DEFAULT_CHANNEL_ID : cid;
            var ch = config["YouTube:ChannelHandle"];
            _channelHandle = string.IsNullOrWhiteSpace(ch) ? DEFAULT_CHANNEL_HANDLE : ch;
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
            Console.WriteLine($"[Shorts] GetLatestShortsAsync called. ChannelId='{_channelId}' CacheCount={_shortsCache?.Count ?? -1} CacheValid={(_shortsCache != null && DateTime.UtcNow < _shortsCacheExpiry)}");

            if (_shortsCache != null && DateTime.UtcNow < _shortsCacheExpiry)
                return _shortsCache.Take(count).ToList();

            if (string.IsNullOrWhiteSpace(_channelId) || !_channelId.StartsWith("UC"))
            {
                Console.WriteLine($"[Shorts] Aborted: invalid channel id '{_channelId}'");
                return new List<YouTubeVideo>();
            }

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

                // Fallback: se RSS Shorts retornou 0 (canal sem shorts ou bloqueio), pega últimos vídeos
                if (shorts.Count == 0)
                {
                    Console.WriteLine("[Shorts RSS] 0 shorts → fallback latest videos");
                    var all = await GetAllAsync();
                    shorts = all.Take(count).ToList();
                }

                _shortsCache = shorts;
                _shortsCacheExpiry = DateTime.UtcNow.AddHours(1);
                return shorts;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Shorts RSS] ERROR: {ex.Message}");
                // Fallback em erro: tenta latest videos
                try
                {
                    var all = await GetAllAsync();
                    var fb = all.Take(count).ToList();
                    Console.WriteLine($"[Shorts RSS] Catch fallback → {fb.Count} videos");
                    _shortsCache = fb;
                    _shortsCacheExpiry = DateTime.UtcNow.AddMinutes(15);
                    return fb;
                }
                catch (Exception ex2)
                {
                    Console.WriteLine($"[Shorts RSS] Fallback also failed: {ex2.Message}");
                    return _shortsCache ?? new List<YouTubeVideo>();
                }
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
