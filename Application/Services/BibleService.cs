using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;

namespace BatistaFloramar.Application.Services
{
    public class BibleVerseDto
    {
        public string Texto { get; set; } = "";
        public string Livro { get; set; } = "";
        public string Referencia { get; set; } = "";
    }

    /// <summary>
    /// Referência canônica de um versículo (abreviação abibliadigital, capítulo, versículo).
    /// </summary>
    public record VerseRef(string Abbrev, string Book, int Chapter, int Verse);

    public class BibleService
    {
        private readonly HttpClient _http;
        private readonly IMemoryCache _cache;
        private readonly IConfiguration _config;
        private readonly ILogger<BibleService> _log;

        private const string VersaoApi = "nvi";

        public BibleService(HttpClient http, IMemoryCache cache, IConfiguration config, ILogger<BibleService> log)
        {
            _http = http;
            _cache = cache;
            _config = config;
            _log = log;
        }

        public async Task<BibleVerseDto> GetVersiculoDoDiaAsync()
        {
            var hojeBR = HojeBrasilia();
            var seed = hojeBR.Year * 1000 + hojeBR.DayOfYear;
            var index = new Random(seed).Next(FamousVerses.Length);
            var refDoDia = FamousVerses[index];

            var cacheKey = $"versiculo_{hojeBR:yyyy-MM-dd}_{refDoDia.Abbrev}_{refDoDia.Chapter}_{refDoDia.Verse}";
            if (_cache.TryGetValue<BibleVerseDto>(cacheKey, out var cached) && cached != null)
                return cached;

            BibleVerseDto resultado;
            try
            {
                resultado = await BuscarDaApiAsync(refDoDia) ?? FallbackLocal(refDoDia);
            }
            catch (Exception ex)
            {
                _log.LogWarning(ex, "Falha ao buscar versículo {Ref} da abibliadigital — usando fallback", refDoDia);
                resultado = FallbackLocal(refDoDia);
            }

            // Cacheia até meia-noite BR (próxima troca)
            var amanhaMeiaNoiteBR = hojeBR.AddDays(1);
            var ttl = amanhaMeiaNoiteBR - DateTime.UtcNow.AddHours(-3);
            if (ttl < TimeSpan.FromMinutes(5)) ttl = TimeSpan.FromHours(24);
            _cache.Set(cacheKey, resultado, ttl);

            return resultado;
        }

        private async Task<BibleVerseDto?> BuscarDaApiAsync(VerseRef r)
        {
            var token = _config["AbibliaDigital:Token"]
                        ?? Environment.GetEnvironmentVariable("ABIBLIADIGITAL_TOKEN");

            var url = $"https://www.abibliadigital.com.br/api/verses/{VersaoApi}/{r.Abbrev}/{r.Chapter}/{r.Verse}";
            using var req = new HttpRequestMessage(HttpMethod.Get, url);
            if (!string.IsNullOrWhiteSpace(token))
                req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            _http.Timeout = TimeSpan.FromSeconds(8);

            using var resp = await _http.SendAsync(req);
            if (!resp.IsSuccessStatusCode)
            {
                _log.LogInformation("abibliadigital retornou {Status} para {Url}", (int)resp.StatusCode, url);
                return null;
            }

            await using var stream = await resp.Content.ReadAsStreamAsync();
            using var doc = await JsonDocument.ParseAsync(stream);
            var root = doc.RootElement;

            string texto = root.TryGetProperty("text", out var t) ? (t.GetString() ?? "") : "";
            int chapter   = root.TryGetProperty("chapter", out var c) ? c.GetInt32() : r.Chapter;
            int verseNum  = root.TryGetProperty("number",  out var n) ? n.GetInt32() : r.Verse;
            string livro = r.Book;
            if (root.TryGetProperty("book", out var bk) && bk.TryGetProperty("name", out var bn))
                livro = bn.GetString() ?? r.Book;

            if (string.IsNullOrWhiteSpace(texto))
                return null;

            return new BibleVerseDto
            {
                Texto      = texto.Trim(),
                Livro      = livro,
                Referencia = $"{livro} {chapter}:{verseNum} (NVI)"
            };
        }

        private static DateTime HojeBrasilia()
        {
            try
            {
                var tz = TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo");
                return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz).Date;
            }
            catch
            {
                try
                {
                    var tz = TimeZoneInfo.FindSystemTimeZoneById("E. South America Standard Time");
                    return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz).Date;
                }
                catch { return DateTime.UtcNow.AddHours(-3).Date; }
            }
        }

        // Fallback simples se API falhar — texto genérico com referência correta.
        private static BibleVerseDto FallbackLocal(VerseRef r) => new BibleVerseDto
        {
            Texto = "Acesse a referência ao lado para ler na íntegra.",
            Livro = r.Book,
            Referencia = $"{r.Book} {r.Chapter}:{r.Verse}"
        };

        // ────────────────────────────────────────────────────────────────────
        // ~430 versículos famosos/clássicos protestantes — distribuídos AT/NT.
        // Selecionado 1/dia por Random com seed (Year*1000+DayOfYear) →
        // mesmo versículo o dia todo, ordem aleatória a cada dia.
        // ────────────────────────────────────────────────────────────────────
        private static readonly VerseRef[] FamousVerses = new[]
        {
            // ── Gênesis ──
            new VerseRef("gn",  "Gênesis",     1, 1),
            new VerseRef("gn",  "Gênesis",     1, 27),
            new VerseRef("gn",  "Gênesis",     2, 24),
            new VerseRef("gn",  "Gênesis",     3, 15),
            new VerseRef("gn",  "Gênesis",    12, 2),
            new VerseRef("gn",  "Gênesis",    15, 6),
            new VerseRef("gn",  "Gênesis",    28, 15),
            new VerseRef("gn",  "Gênesis",    50, 20),
            // ── Êxodo ──
            new VerseRef("ex",  "Êxodo",       3, 14),
            new VerseRef("ex",  "Êxodo",      14, 14),
            new VerseRef("ex",  "Êxodo",      15, 2),
            new VerseRef("ex",  "Êxodo",      33, 14),
            // ── Levítico ──
            new VerseRef("lv",  "Levítico",   19, 18),
            new VerseRef("lv",  "Levítico",   20, 26),
            // ── Números ──
            new VerseRef("nm",  "Números",     6, 24),
            new VerseRef("nm",  "Números",     6, 25),
            new VerseRef("nm",  "Números",     6, 26),
            new VerseRef("nm",  "Números",    23, 19),
            // ── Deuteronômio ──
            new VerseRef("dt",  "Deuteronômio", 6, 5),
            new VerseRef("dt",  "Deuteronômio", 6, 6),
            new VerseRef("dt",  "Deuteronômio", 6, 7),
            new VerseRef("dt",  "Deuteronômio", 8, 3),
            new VerseRef("dt",  "Deuteronômio", 30, 19),
            new VerseRef("dt",  "Deuteronômio", 31, 6),
            new VerseRef("dt",  "Deuteronômio", 31, 8),
            new VerseRef("dt",  "Deuteronômio", 33, 27),
            // ── Josué ──
            new VerseRef("js",  "Josué",       1, 5),
            new VerseRef("js",  "Josué",       1, 7),
            new VerseRef("js",  "Josué",       1, 8),
            new VerseRef("js",  "Josué",       1, 9),
            new VerseRef("js",  "Josué",      24, 15),
            // ── Juízes ──
            new VerseRef("jz",  "Juízes",      6, 12),
            // ── Rute ──
            new VerseRef("rt",  "Rute",        1, 16),
            // ── 1 Samuel ──
            new VerseRef("1sm", "1 Samuel",    2, 30),
            new VerseRef("1sm", "1 Samuel",    3, 9),
            new VerseRef("1sm", "1 Samuel",   12, 24),
            new VerseRef("1sm", "1 Samuel",   16, 7),
            new VerseRef("1sm", "1 Samuel",   17, 47),
            // ── 2 Samuel ──
            new VerseRef("2sm", "2 Samuel",   22, 31),
            new VerseRef("2sm", "2 Samuel",   22, 33),
            // ── 1 Reis ──
            new VerseRef("1rs", "1 Reis",      8, 23),
            new VerseRef("1rs", "1 Reis",     19, 11),
            new VerseRef("1rs", "1 Reis",     19, 12),
            // ── 2 Reis ──
            new VerseRef("2rs", "2 Reis",      6, 16),
            // ── 1 Crônicas ──
            new VerseRef("1cr", "1 Crônicas", 16, 11),
            new VerseRef("1cr", "1 Crônicas", 28, 20),
            new VerseRef("1cr", "1 Crônicas", 29, 11),
            // ── 2 Crônicas ──
            new VerseRef("2cr", "2 Crônicas",  7, 14),
            new VerseRef("2cr", "2 Crônicas", 16, 9),
            new VerseRef("2cr", "2 Crônicas", 20, 15),
            new VerseRef("2cr", "2 Crônicas", 20, 17),
            // ── Esdras ──
            new VerseRef("ed",  "Esdras",      8, 22),
            // ── Neemias ──
            new VerseRef("ne",  "Neemias",     1, 11),
            new VerseRef("ne",  "Neemias",     4, 14),
            new VerseRef("ne",  "Neemias",     8, 10),
            // ── Ester ──
            new VerseRef("et",  "Ester",       4, 14),
            // ── Jó ──
            new VerseRef("job", "Jó",          1, 21),
            new VerseRef("job", "Jó",         19, 25),
            new VerseRef("job", "Jó",         23, 10),
            new VerseRef("job", "Jó",         42, 2),
            new VerseRef("job", "Jó",         42, 5),
            // ── Salmos ──
            new VerseRef("sl",  "Salmos",      1, 1),
            new VerseRef("sl",  "Salmos",      1, 2),
            new VerseRef("sl",  "Salmos",      1, 3),
            new VerseRef("sl",  "Salmos",      4, 8),
            new VerseRef("sl",  "Salmos",      8, 1),
            new VerseRef("sl",  "Salmos",     16, 11),
            new VerseRef("sl",  "Salmos",     18, 2),
            new VerseRef("sl",  "Salmos",     19, 1),
            new VerseRef("sl",  "Salmos",     19, 14),
            new VerseRef("sl",  "Salmos",     22, 1),
            new VerseRef("sl",  "Salmos",     23, 1),
            new VerseRef("sl",  "Salmos",     23, 4),
            new VerseRef("sl",  "Salmos",     23, 6),
            new VerseRef("sl",  "Salmos",     24, 1),
            new VerseRef("sl",  "Salmos",     27, 1),
            new VerseRef("sl",  "Salmos",     27, 14),
            new VerseRef("sl",  "Salmos",     28, 7),
            new VerseRef("sl",  "Salmos",     29, 11),
            new VerseRef("sl",  "Salmos",     30, 5),
            new VerseRef("sl",  "Salmos",     31, 24),
            new VerseRef("sl",  "Salmos",     32, 8),
            new VerseRef("sl",  "Salmos",     33, 12),
            new VerseRef("sl",  "Salmos",     34, 1),
            new VerseRef("sl",  "Salmos",     34, 8),
            new VerseRef("sl",  "Salmos",     34, 18),
            new VerseRef("sl",  "Salmos",     37, 4),
            new VerseRef("sl",  "Salmos",     37, 5),
            new VerseRef("sl",  "Salmos",     37, 7),
            new VerseRef("sl",  "Salmos",     37, 23),
            new VerseRef("sl",  "Salmos",     40, 1),
            new VerseRef("sl",  "Salmos",     42, 1),
            new VerseRef("sl",  "Salmos",     42, 11),
            new VerseRef("sl",  "Salmos",     46, 1),
            new VerseRef("sl",  "Salmos",     46, 10),
            new VerseRef("sl",  "Salmos",     47, 1),
            new VerseRef("sl",  "Salmos",     50, 15),
            new VerseRef("sl",  "Salmos",     51, 10),
            new VerseRef("sl",  "Salmos",     51, 17),
            new VerseRef("sl",  "Salmos",     55, 22),
            new VerseRef("sl",  "Salmos",     56, 3),
            new VerseRef("sl",  "Salmos",     62, 1),
            new VerseRef("sl",  "Salmos",     62, 5),
            new VerseRef("sl",  "Salmos",     63, 1),
            new VerseRef("sl",  "Salmos",     63, 3),
            new VerseRef("sl",  "Salmos",     66, 18),
            new VerseRef("sl",  "Salmos",     67, 1),
            new VerseRef("sl",  "Salmos",     68, 5),
            new VerseRef("sl",  "Salmos",     71, 14),
            new VerseRef("sl",  "Salmos",     73, 25),
            new VerseRef("sl",  "Salmos",     84, 10),
            new VerseRef("sl",  "Salmos",     86, 11),
            new VerseRef("sl",  "Salmos",     90, 12),
            new VerseRef("sl",  "Salmos",     91, 1),
            new VerseRef("sl",  "Salmos",     91, 11),
            new VerseRef("sl",  "Salmos",     91, 14),
            new VerseRef("sl",  "Salmos",     92, 1),
            new VerseRef("sl",  "Salmos",     95, 6),
            new VerseRef("sl",  "Salmos",     96, 1),
            new VerseRef("sl",  "Salmos",    100, 3),
            new VerseRef("sl",  "Salmos",    100, 4),
            new VerseRef("sl",  "Salmos",    103, 1),
            new VerseRef("sl",  "Salmos",    103, 8),
            new VerseRef("sl",  "Salmos",    103, 12),
            new VerseRef("sl",  "Salmos",    105, 4),
            new VerseRef("sl",  "Salmos",    107, 1),
            new VerseRef("sl",  "Salmos",    111, 10),
            new VerseRef("sl",  "Salmos",    112, 7),
            new VerseRef("sl",  "Salmos",    115, 1),
            new VerseRef("sl",  "Salmos",    116, 1),
            new VerseRef("sl",  "Salmos",    118, 24),
            new VerseRef("sl",  "Salmos",    119, 9),
            new VerseRef("sl",  "Salmos",    119, 11),
            new VerseRef("sl",  "Salmos",    119, 18),
            new VerseRef("sl",  "Salmos",    119, 33),
            new VerseRef("sl",  "Salmos",    119, 50),
            new VerseRef("sl",  "Salmos",    119, 72),
            new VerseRef("sl",  "Salmos",    119, 89),
            new VerseRef("sl",  "Salmos",    119, 105),
            new VerseRef("sl",  "Salmos",    119, 130),
            new VerseRef("sl",  "Salmos",    119, 165),
            new VerseRef("sl",  "Salmos",    121, 1),
            new VerseRef("sl",  "Salmos",    121, 2),
            new VerseRef("sl",  "Salmos",    121, 7),
            new VerseRef("sl",  "Salmos",    121, 8),
            new VerseRef("sl",  "Salmos",    122, 1),
            new VerseRef("sl",  "Salmos",    126, 5),
            new VerseRef("sl",  "Salmos",    127, 1),
            new VerseRef("sl",  "Salmos",    127, 3),
            new VerseRef("sl",  "Salmos",    130, 5),
            new VerseRef("sl",  "Salmos",    133, 1),
            new VerseRef("sl",  "Salmos",    136, 1),
            new VerseRef("sl",  "Salmos",    138, 8),
            new VerseRef("sl",  "Salmos",    139, 14),
            new VerseRef("sl",  "Salmos",    139, 23),
            new VerseRef("sl",  "Salmos",    139, 24),
            new VerseRef("sl",  "Salmos",    143, 8),
            new VerseRef("sl",  "Salmos",    145, 18),
            new VerseRef("sl",  "Salmos",    146, 5),
            new VerseRef("sl",  "Salmos",    147, 3),
            new VerseRef("sl",  "Salmos",    150, 6),
            // ── Provérbios ──
            new VerseRef("pv",  "Provérbios",  1, 7),
            new VerseRef("pv",  "Provérbios",  3, 5),
            new VerseRef("pv",  "Provérbios",  3, 6),
            new VerseRef("pv",  "Provérbios",  3, 9),
            new VerseRef("pv",  "Provérbios",  4, 7),
            new VerseRef("pv",  "Provérbios",  4, 23),
            new VerseRef("pv",  "Provérbios",  8, 17),
            new VerseRef("pv",  "Provérbios", 10, 12),
            new VerseRef("pv",  "Provérbios", 11, 14),
            new VerseRef("pv",  "Provérbios", 12, 25),
            new VerseRef("pv",  "Provérbios", 13, 20),
            new VerseRef("pv",  "Provérbios", 14, 12),
            new VerseRef("pv",  "Provérbios", 15, 1),
            new VerseRef("pv",  "Provérbios", 15, 13),
            new VerseRef("pv",  "Provérbios", 15, 22),
            new VerseRef("pv",  "Provérbios", 16, 3),
            new VerseRef("pv",  "Provérbios", 16, 7),
            new VerseRef("pv",  "Provérbios", 16, 9),
            new VerseRef("pv",  "Provérbios", 16, 18),
            new VerseRef("pv",  "Provérbios", 17, 17),
            new VerseRef("pv",  "Provérbios", 17, 22),
            new VerseRef("pv",  "Provérbios", 18, 10),
            new VerseRef("pv",  "Provérbios", 18, 21),
            new VerseRef("pv",  "Provérbios", 18, 22),
            new VerseRef("pv",  "Provérbios", 19, 21),
            new VerseRef("pv",  "Provérbios", 20, 24),
            new VerseRef("pv",  "Provérbios", 22, 1),
            new VerseRef("pv",  "Provérbios", 22, 6),
            new VerseRef("pv",  "Provérbios", 23, 7),
            new VerseRef("pv",  "Provérbios", 24, 16),
            new VerseRef("pv",  "Provérbios", 27, 17),
            new VerseRef("pv",  "Provérbios", 28, 13),
            new VerseRef("pv",  "Provérbios", 29, 25),
            new VerseRef("pv",  "Provérbios", 31, 10),
            new VerseRef("pv",  "Provérbios", 31, 30),
            // ── Eclesiastes ──
            new VerseRef("ec",  "Eclesiastes", 3, 1),
            new VerseRef("ec",  "Eclesiastes", 3, 11),
            new VerseRef("ec",  "Eclesiastes", 4, 9),
            new VerseRef("ec",  "Eclesiastes", 4, 10),
            new VerseRef("ec",  "Eclesiastes", 7, 9),
            new VerseRef("ec",  "Eclesiastes",11, 1),
            new VerseRef("ec",  "Eclesiastes",12, 1),
            new VerseRef("ec",  "Eclesiastes",12, 13),
            // ── Cantares ──
            new VerseRef("ct",  "Cantares",    2, 4),
            new VerseRef("ct",  "Cantares",    8, 7),
            // ── Isaías ──
            new VerseRef("is",  "Isaías",      1, 18),
            new VerseRef("is",  "Isaías",      6, 3),
            new VerseRef("is",  "Isaías",      6, 8),
            new VerseRef("is",  "Isaías",      7, 14),
            new VerseRef("is",  "Isaías",      9, 6),
            new VerseRef("is",  "Isaías",     26, 3),
            new VerseRef("is",  "Isaías",     30, 15),
            new VerseRef("is",  "Isaías",     33, 6),
            new VerseRef("is",  "Isaías",     40, 8),
            new VerseRef("is",  "Isaías",     40, 28),
            new VerseRef("is",  "Isaías",     40, 29),
            new VerseRef("is",  "Isaías",     40, 31),
            new VerseRef("is",  "Isaías",     41, 10),
            new VerseRef("is",  "Isaías",     41, 13),
            new VerseRef("is",  "Isaías",     43, 1),
            new VerseRef("is",  "Isaías",     43, 2),
            new VerseRef("is",  "Isaías",     43, 18),
            new VerseRef("is",  "Isaías",     43, 19),
            new VerseRef("is",  "Isaías",     43, 25),
            new VerseRef("is",  "Isaías",     46, 4),
            new VerseRef("is",  "Isaías",     49, 15),
            new VerseRef("is",  "Isaías",     49, 16),
            new VerseRef("is",  "Isaías",     53, 5),
            new VerseRef("is",  "Isaías",     53, 6),
            new VerseRef("is",  "Isaías",     54, 10),
            new VerseRef("is",  "Isaías",     54, 17),
            new VerseRef("is",  "Isaías",     55, 6),
            new VerseRef("is",  "Isaías",     55, 8),
            new VerseRef("is",  "Isaías",     55, 9),
            new VerseRef("is",  "Isaías",     55, 11),
            new VerseRef("is",  "Isaías",     57, 15),
            new VerseRef("is",  "Isaías",     58, 11),
            new VerseRef("is",  "Isaías",     60, 1),
            new VerseRef("is",  "Isaías",     61, 1),
            new VerseRef("is",  "Isaías",     61, 3),
            new VerseRef("is",  "Isaías",     64, 8),
            new VerseRef("is",  "Isaías",     65, 24),
            // ── Jeremias ──
            new VerseRef("jr",  "Jeremias",    1, 5),
            new VerseRef("jr",  "Jeremias",   17, 7),
            new VerseRef("jr",  "Jeremias",   17, 8),
            new VerseRef("jr",  "Jeremias",   17, 9),
            new VerseRef("jr",  "Jeremias",   18, 6),
            new VerseRef("jr",  "Jeremias",   23, 24),
            new VerseRef("jr",  "Jeremias",   29, 11),
            new VerseRef("jr",  "Jeremias",   29, 12),
            new VerseRef("jr",  "Jeremias",   29, 13),
            new VerseRef("jr",  "Jeremias",   31, 3),
            new VerseRef("jr",  "Jeremias",   32, 17),
            new VerseRef("jr",  "Jeremias",   32, 27),
            new VerseRef("jr",  "Jeremias",   33, 3),
            // ── Lamentações ──
            new VerseRef("lm",  "Lamentações", 3, 22),
            new VerseRef("lm",  "Lamentações", 3, 23),
            new VerseRef("lm",  "Lamentações", 3, 25),
            new VerseRef("lm",  "Lamentações", 3, 26),
            // ── Ezequiel ──
            new VerseRef("ez",  "Ezequiel",   36, 26),
            new VerseRef("ez",  "Ezequiel",   37, 5),
            // ── Daniel ──
            new VerseRef("dn",  "Daniel",      2, 20),
            new VerseRef("dn",  "Daniel",      2, 21),
            new VerseRef("dn",  "Daniel",      3, 17),
            new VerseRef("dn",  "Daniel",      3, 18),
            new VerseRef("dn",  "Daniel",      6, 23),
            new VerseRef("dn",  "Daniel",      9, 9),
            new VerseRef("dn",  "Daniel",     12, 3),
            // ── Oséias ──
            new VerseRef("os",  "Oséias",      6, 6),
            new VerseRef("os",  "Oséias",     10, 12),
            new VerseRef("os",  "Oséias",     14, 9),
            // ── Joel ──
            new VerseRef("jl",  "Joel",        2, 13),
            new VerseRef("jl",  "Joel",        2, 25),
            new VerseRef("jl",  "Joel",        2, 28),
            new VerseRef("jl",  "Joel",        2, 32),
            // ── Amós ──
            new VerseRef("am",  "Amós",        5, 24),
            // ── Jonas ──
            new VerseRef("jn",  "Jonas",       2, 9),
            // ── Miqueias ──
            new VerseRef("mq",  "Miqueias",    6, 8),
            new VerseRef("mq",  "Miqueias",    7, 7),
            // ── Naum ──
            new VerseRef("na",  "Naum",        1, 7),
            // ── Habacuque ──
            new VerseRef("hc",  "Habacuque",   2, 4),
            new VerseRef("hc",  "Habacuque",   3, 17),
            new VerseRef("hc",  "Habacuque",   3, 18),
            // ── Sofonias ──
            new VerseRef("sf",  "Sofonias",    3, 17),
            // ── Zacarias ──
            new VerseRef("zc",  "Zacarias",    4, 6),
            // ── Malaquias ──
            new VerseRef("ml",  "Malaquias",   3, 6),
            new VerseRef("ml",  "Malaquias",   3, 10),

            // ──────────── NOVO TESTAMENTO ────────────

            // ── Mateus ──
            new VerseRef("mt",  "Mateus",      1, 21),
            new VerseRef("mt",  "Mateus",      1, 23),
            new VerseRef("mt",  "Mateus",      4, 4),
            new VerseRef("mt",  "Mateus",      4, 19),
            new VerseRef("mt",  "Mateus",      5, 3),
            new VerseRef("mt",  "Mateus",      5, 6),
            new VerseRef("mt",  "Mateus",      5, 8),
            new VerseRef("mt",  "Mateus",      5, 9),
            new VerseRef("mt",  "Mateus",      5, 14),
            new VerseRef("mt",  "Mateus",      5, 16),
            new VerseRef("mt",  "Mateus",      5, 44),
            new VerseRef("mt",  "Mateus",      6, 6),
            new VerseRef("mt",  "Mateus",      6, 9),
            new VerseRef("mt",  "Mateus",      6, 14),
            new VerseRef("mt",  "Mateus",      6, 19),
            new VerseRef("mt",  "Mateus",      6, 24),
            new VerseRef("mt",  "Mateus",      6, 25),
            new VerseRef("mt",  "Mateus",      6, 26),
            new VerseRef("mt",  "Mateus",      6, 33),
            new VerseRef("mt",  "Mateus",      6, 34),
            new VerseRef("mt",  "Mateus",      7, 1),
            new VerseRef("mt",  "Mateus",      7, 7),
            new VerseRef("mt",  "Mateus",      7, 12),
            new VerseRef("mt",  "Mateus",      7, 13),
            new VerseRef("mt",  "Mateus",      7, 24),
            new VerseRef("mt",  "Mateus",      9, 37),
            new VerseRef("mt",  "Mateus",     10, 28),
            new VerseRef("mt",  "Mateus",     10, 32),
            new VerseRef("mt",  "Mateus",     11, 28),
            new VerseRef("mt",  "Mateus",     11, 29),
            new VerseRef("mt",  "Mateus",     11, 30),
            new VerseRef("mt",  "Mateus",     12, 34),
            new VerseRef("mt",  "Mateus",     16, 18),
            new VerseRef("mt",  "Mateus",     16, 24),
            new VerseRef("mt",  "Mateus",     16, 26),
            new VerseRef("mt",  "Mateus",     17, 20),
            new VerseRef("mt",  "Mateus",     18, 20),
            new VerseRef("mt",  "Mateus",     19, 14),
            new VerseRef("mt",  "Mateus",     19, 26),
            new VerseRef("mt",  "Mateus",     20, 28),
            new VerseRef("mt",  "Mateus",     22, 37),
            new VerseRef("mt",  "Mateus",     22, 39),
            new VerseRef("mt",  "Mateus",     24, 35),
            new VerseRef("mt",  "Mateus",     24, 42),
            new VerseRef("mt",  "Mateus",     25, 21),
            new VerseRef("mt",  "Mateus",     25, 40),
            new VerseRef("mt",  "Mateus",     26, 41),
            new VerseRef("mt",  "Mateus",     28, 19),
            new VerseRef("mt",  "Mateus",     28, 20),
            // ── Marcos ──
            new VerseRef("mc",  "Marcos",      1, 15),
            new VerseRef("mc",  "Marcos",      9, 23),
            new VerseRef("mc",  "Marcos",      9, 35),
            new VerseRef("mc",  "Marcos",     10, 27),
            new VerseRef("mc",  "Marcos",     10, 45),
            new VerseRef("mc",  "Marcos",     11, 24),
            new VerseRef("mc",  "Marcos",     12, 30),
            new VerseRef("mc",  "Marcos",     12, 31),
            new VerseRef("mc",  "Marcos",     16, 15),
            // ── Lucas ──
            new VerseRef("lc",  "Lucas",       1, 37),
            new VerseRef("lc",  "Lucas",       2, 10),
            new VerseRef("lc",  "Lucas",       2, 11),
            new VerseRef("lc",  "Lucas",       2, 14),
            new VerseRef("lc",  "Lucas",       6, 27),
            new VerseRef("lc",  "Lucas",       6, 31),
            new VerseRef("lc",  "Lucas",       6, 38),
            new VerseRef("lc",  "Lucas",       9, 23),
            new VerseRef("lc",  "Lucas",      11, 9),
            new VerseRef("lc",  "Lucas",      12, 7),
            new VerseRef("lc",  "Lucas",      12, 15),
            new VerseRef("lc",  "Lucas",      12, 31),
            new VerseRef("lc",  "Lucas",      14, 11),
            new VerseRef("lc",  "Lucas",      15, 7),
            new VerseRef("lc",  "Lucas",      16, 13),
            new VerseRef("lc",  "Lucas",      18, 1),
            new VerseRef("lc",  "Lucas",      18, 27),
            new VerseRef("lc",  "Lucas",      19, 10),
            new VerseRef("lc",  "Lucas",      21, 33),
            new VerseRef("lc",  "Lucas",      22, 42),
            new VerseRef("lc",  "Lucas",      23, 34),
            new VerseRef("lc",  "Lucas",      24, 6),
            // ── João ──
            new VerseRef("jo",  "João",        1, 1),
            new VerseRef("jo",  "João",        1, 12),
            new VerseRef("jo",  "João",        1, 14),
            new VerseRef("jo",  "João",        1, 29),
            new VerseRef("jo",  "João",        3, 3),
            new VerseRef("jo",  "João",        3, 16),
            new VerseRef("jo",  "João",        3, 30),
            new VerseRef("jo",  "João",        3, 36),
            new VerseRef("jo",  "João",        4, 14),
            new VerseRef("jo",  "João",        4, 24),
            new VerseRef("jo",  "João",        5, 24),
            new VerseRef("jo",  "João",        6, 35),
            new VerseRef("jo",  "João",        6, 37),
            new VerseRef("jo",  "João",        6, 63),
            new VerseRef("jo",  "João",        7, 38),
            new VerseRef("jo",  "João",        8, 12),
            new VerseRef("jo",  "João",        8, 32),
            new VerseRef("jo",  "João",        8, 36),
            new VerseRef("jo",  "João",       10, 10),
            new VerseRef("jo",  "João",       10, 11),
            new VerseRef("jo",  "João",       10, 27),
            new VerseRef("jo",  "João",       10, 28),
            new VerseRef("jo",  "João",       11, 25),
            new VerseRef("jo",  "João",       11, 26),
            new VerseRef("jo",  "João",       12, 26),
            new VerseRef("jo",  "João",       13, 34),
            new VerseRef("jo",  "João",       13, 35),
            new VerseRef("jo",  "João",       14, 1),
            new VerseRef("jo",  "João",       14, 2),
            new VerseRef("jo",  "João",       14, 6),
            new VerseRef("jo",  "João",       14, 13),
            new VerseRef("jo",  "João",       14, 15),
            new VerseRef("jo",  "João",       14, 21),
            new VerseRef("jo",  "João",       14, 26),
            new VerseRef("jo",  "João",       14, 27),
            new VerseRef("jo",  "João",       15, 4),
            new VerseRef("jo",  "João",       15, 5),
            new VerseRef("jo",  "João",       15, 7),
            new VerseRef("jo",  "João",       15, 11),
            new VerseRef("jo",  "João",       15, 12),
            new VerseRef("jo",  "João",       15, 13),
            new VerseRef("jo",  "João",       15, 16),
            new VerseRef("jo",  "João",       16, 13),
            new VerseRef("jo",  "João",       16, 24),
            new VerseRef("jo",  "João",       16, 33),
            new VerseRef("jo",  "João",       17, 3),
            new VerseRef("jo",  "João",       17, 17),
            new VerseRef("jo",  "João",       19, 30),
            new VerseRef("jo",  "João",       20, 29),
            new VerseRef("jo",  "João",       20, 31),
            // ── Atos ──
            new VerseRef("at",  "Atos",        1, 8),
            new VerseRef("at",  "Atos",        2, 21),
            new VerseRef("at",  "Atos",        2, 38),
            new VerseRef("at",  "Atos",        2, 42),
            new VerseRef("at",  "Atos",        4, 12),
            new VerseRef("at",  "Atos",        5, 29),
            new VerseRef("at",  "Atos",       13, 38),
            new VerseRef("at",  "Atos",       16, 31),
            new VerseRef("at",  "Atos",       17, 11),
            new VerseRef("at",  "Atos",       17, 24),
            new VerseRef("at",  "Atos",       17, 28),
            new VerseRef("at",  "Atos",       20, 24),
            new VerseRef("at",  "Atos",       20, 35),
            // ── Romanos ──
            new VerseRef("rm",  "Romanos",     1, 16),
            new VerseRef("rm",  "Romanos",     1, 17),
            new VerseRef("rm",  "Romanos",     3, 23),
            new VerseRef("rm",  "Romanos",     3, 24),
            new VerseRef("rm",  "Romanos",     5, 1),
            new VerseRef("rm",  "Romanos",     5, 3),
            new VerseRef("rm",  "Romanos",     5, 5),
            new VerseRef("rm",  "Romanos",     5, 8),
            new VerseRef("rm",  "Romanos",     6, 23),
            new VerseRef("rm",  "Romanos",     8, 1),
            new VerseRef("rm",  "Romanos",     8, 14),
            new VerseRef("rm",  "Romanos",     8, 15),
            new VerseRef("rm",  "Romanos",     8, 18),
            new VerseRef("rm",  "Romanos",     8, 26),
            new VerseRef("rm",  "Romanos",     8, 28),
            new VerseRef("rm",  "Romanos",     8, 31),
            new VerseRef("rm",  "Romanos",     8, 32),
            new VerseRef("rm",  "Romanos",     8, 35),
            new VerseRef("rm",  "Romanos",     8, 37),
            new VerseRef("rm",  "Romanos",     8, 38),
            new VerseRef("rm",  "Romanos",     8, 39),
            new VerseRef("rm",  "Romanos",    10, 9),
            new VerseRef("rm",  "Romanos",    10, 13),
            new VerseRef("rm",  "Romanos",    10, 17),
            new VerseRef("rm",  "Romanos",    11, 33),
            new VerseRef("rm",  "Romanos",    11, 36),
            new VerseRef("rm",  "Romanos",    12, 1),
            new VerseRef("rm",  "Romanos",    12, 2),
            new VerseRef("rm",  "Romanos",    12, 9),
            new VerseRef("rm",  "Romanos",    12, 12),
            new VerseRef("rm",  "Romanos",    12, 18),
            new VerseRef("rm",  "Romanos",    12, 21),
            new VerseRef("rm",  "Romanos",    13, 8),
            new VerseRef("rm",  "Romanos",    13, 10),
            new VerseRef("rm",  "Romanos",    14, 8),
            new VerseRef("rm",  "Romanos",    15, 4),
            new VerseRef("rm",  "Romanos",    15, 13),
            new VerseRef("rm",  "Romanos",    16, 20),
            // ── 1 Coríntios ──
            new VerseRef("1co", "1 Coríntios", 1, 18),
            new VerseRef("1co", "1 Coríntios", 1, 25),
            new VerseRef("1co", "1 Coríntios", 2, 9),
            new VerseRef("1co", "1 Coríntios", 3, 6),
            new VerseRef("1co", "1 Coríntios", 3, 11),
            new VerseRef("1co", "1 Coríntios", 3, 16),
            new VerseRef("1co", "1 Coríntios", 6, 19),
            new VerseRef("1co", "1 Coríntios", 9, 24),
            new VerseRef("1co", "1 Coríntios",10, 13),
            new VerseRef("1co", "1 Coríntios",10, 31),
            new VerseRef("1co", "1 Coríntios",12, 27),
            new VerseRef("1co", "1 Coríntios",13, 1),
            new VerseRef("1co", "1 Coríntios",13, 4),
            new VerseRef("1co", "1 Coríntios",13, 7),
            new VerseRef("1co", "1 Coríntios",13, 13),
            new VerseRef("1co", "1 Coríntios",15, 3),
            new VerseRef("1co", "1 Coríntios",15, 33),
            new VerseRef("1co", "1 Coríntios",15, 55),
            new VerseRef("1co", "1 Coríntios",15, 57),
            new VerseRef("1co", "1 Coríntios",15, 58),
            new VerseRef("1co", "1 Coríntios",16, 13),
            // ── 2 Coríntios ──
            new VerseRef("2co", "2 Coríntios", 1, 3),
            new VerseRef("2co", "2 Coríntios", 4, 7),
            new VerseRef("2co", "2 Coríntios", 4, 16),
            new VerseRef("2co", "2 Coríntios", 5, 7),
            new VerseRef("2co", "2 Coríntios", 5, 17),
            new VerseRef("2co", "2 Coríntios", 5, 21),
            new VerseRef("2co", "2 Coríntios", 6, 14),
            new VerseRef("2co", "2 Coríntios", 9, 7),
            new VerseRef("2co", "2 Coríntios",10, 4),
            new VerseRef("2co", "2 Coríntios",12, 9),
            new VerseRef("2co", "2 Coríntios",12, 10),
            // ── Gálatas ──
            new VerseRef("gl",  "Gálatas",     2, 20),
            new VerseRef("gl",  "Gálatas",     5, 1),
            new VerseRef("gl",  "Gálatas",     5, 13),
            new VerseRef("gl",  "Gálatas",     5, 16),
            new VerseRef("gl",  "Gálatas",     5, 22),
            new VerseRef("gl",  "Gálatas",     5, 23),
            new VerseRef("gl",  "Gálatas",     6, 2),
            new VerseRef("gl",  "Gálatas",     6, 7),
            new VerseRef("gl",  "Gálatas",     6, 9),
            // ── Efésios ──
            new VerseRef("ef",  "Efésios",     1, 3),
            new VerseRef("ef",  "Efésios",     1, 7),
            new VerseRef("ef",  "Efésios",     2, 4),
            new VerseRef("ef",  "Efésios",     2, 8),
            new VerseRef("ef",  "Efésios",     2, 10),
            new VerseRef("ef",  "Efésios",     3, 20),
            new VerseRef("ef",  "Efésios",     4, 2),
            new VerseRef("ef",  "Efésios",     4, 15),
            new VerseRef("ef",  "Efésios",     4, 26),
            new VerseRef("ef",  "Efésios",     4, 29),
            new VerseRef("ef",  "Efésios",     4, 32),
            new VerseRef("ef",  "Efésios",     5, 1),
            new VerseRef("ef",  "Efésios",     5, 8),
            new VerseRef("ef",  "Efésios",     5, 15),
            new VerseRef("ef",  "Efésios",     5, 20),
            new VerseRef("ef",  "Efésios",     5, 25),
            new VerseRef("ef",  "Efésios",     6, 1),
            new VerseRef("ef",  "Efésios",     6, 4),
            new VerseRef("ef",  "Efésios",     6, 10),
            new VerseRef("ef",  "Efésios",     6, 11),
            new VerseRef("ef",  "Efésios",     6, 12),
            new VerseRef("ef",  "Efésios",     6, 18),
            // ── Filipenses ──
            new VerseRef("fp",  "Filipenses",  1, 6),
            new VerseRef("fp",  "Filipenses",  1, 21),
            new VerseRef("fp",  "Filipenses",  2, 3),
            new VerseRef("fp",  "Filipenses",  2, 5),
            new VerseRef("fp",  "Filipenses",  2, 9),
            new VerseRef("fp",  "Filipenses",  2, 13),
            new VerseRef("fp",  "Filipenses",  3, 13),
            new VerseRef("fp",  "Filipenses",  3, 14),
            new VerseRef("fp",  "Filipenses",  3, 20),
            new VerseRef("fp",  "Filipenses",  4, 4),
            new VerseRef("fp",  "Filipenses",  4, 6),
            new VerseRef("fp",  "Filipenses",  4, 7),
            new VerseRef("fp",  "Filipenses",  4, 8),
            new VerseRef("fp",  "Filipenses",  4, 11),
            new VerseRef("fp",  "Filipenses",  4, 13),
            new VerseRef("fp",  "Filipenses",  4, 19),
            // ── Colossenses ──
            new VerseRef("cl",  "Colossenses", 1, 13),
            new VerseRef("cl",  "Colossenses", 1, 16),
            new VerseRef("cl",  "Colossenses", 2, 6),
            new VerseRef("cl",  "Colossenses", 2, 9),
            new VerseRef("cl",  "Colossenses", 3, 1),
            new VerseRef("cl",  "Colossenses", 3, 12),
            new VerseRef("cl",  "Colossenses", 3, 13),
            new VerseRef("cl",  "Colossenses", 3, 14),
            new VerseRef("cl",  "Colossenses", 3, 15),
            new VerseRef("cl",  "Colossenses", 3, 16),
            new VerseRef("cl",  "Colossenses", 3, 17),
            new VerseRef("cl",  "Colossenses", 3, 23),
            // ── 1 Tessalonicenses ──
            new VerseRef("1ts", "1 Tessalonicenses", 4, 13),
            new VerseRef("1ts", "1 Tessalonicenses", 4, 16),
            new VerseRef("1ts", "1 Tessalonicenses", 5, 11),
            new VerseRef("1ts", "1 Tessalonicenses", 5, 16),
            new VerseRef("1ts", "1 Tessalonicenses", 5, 17),
            new VerseRef("1ts", "1 Tessalonicenses", 5, 18),
            new VerseRef("1ts", "1 Tessalonicenses", 5, 21),
            new VerseRef("1ts", "1 Tessalonicenses", 5, 23),
            // ── 2 Tessalonicenses ──
            new VerseRef("2ts", "2 Tessalonicenses", 3, 3),
            new VerseRef("2ts", "2 Tessalonicenses", 3, 13),
            // ── 1 Timóteo ──
            new VerseRef("1tm", "1 Timóteo",   1, 15),
            new VerseRef("1tm", "1 Timóteo",   2, 5),
            new VerseRef("1tm", "1 Timóteo",   4, 8),
            new VerseRef("1tm", "1 Timóteo",   4, 12),
            new VerseRef("1tm", "1 Timóteo",   6, 6),
            new VerseRef("1tm", "1 Timóteo",   6, 10),
            new VerseRef("1tm", "1 Timóteo",   6, 12),
            // ── 2 Timóteo ──
            new VerseRef("2tm", "2 Timóteo",   1, 7),
            new VerseRef("2tm", "2 Timóteo",   1, 9),
            new VerseRef("2tm", "2 Timóteo",   2, 15),
            new VerseRef("2tm", "2 Timóteo",   3, 16),
            new VerseRef("2tm", "2 Timóteo",   4, 7),
            // ── Tito ──
            new VerseRef("tt",  "Tito",        2, 11),
            new VerseRef("tt",  "Tito",        3, 5),
            // ── Hebreus ──
            new VerseRef("hb",  "Hebreus",     4, 12),
            new VerseRef("hb",  "Hebreus",     4, 15),
            new VerseRef("hb",  "Hebreus",     4, 16),
            new VerseRef("hb",  "Hebreus",     9, 27),
            new VerseRef("hb",  "Hebreus",    10, 23),
            new VerseRef("hb",  "Hebreus",    10, 24),
            new VerseRef("hb",  "Hebreus",    11, 1),
            new VerseRef("hb",  "Hebreus",    11, 6),
            new VerseRef("hb",  "Hebreus",    12, 1),
            new VerseRef("hb",  "Hebreus",    12, 2),
            new VerseRef("hb",  "Hebreus",    12, 6),
            new VerseRef("hb",  "Hebreus",    12, 11),
            new VerseRef("hb",  "Hebreus",    12, 14),
            new VerseRef("hb",  "Hebreus",    13, 5),
            new VerseRef("hb",  "Hebreus",    13, 8),
            new VerseRef("hb",  "Hebreus",    13, 15),
            // ── Tiago ──
            new VerseRef("tg",  "Tiago",       1, 2),
            new VerseRef("tg",  "Tiago",       1, 5),
            new VerseRef("tg",  "Tiago",       1, 12),
            new VerseRef("tg",  "Tiago",       1, 17),
            new VerseRef("tg",  "Tiago",       1, 19),
            new VerseRef("tg",  "Tiago",       1, 22),
            new VerseRef("tg",  "Tiago",       1, 27),
            new VerseRef("tg",  "Tiago",       2, 17),
            new VerseRef("tg",  "Tiago",       3, 18),
            new VerseRef("tg",  "Tiago",       4, 6),
            new VerseRef("tg",  "Tiago",       4, 7),
            new VerseRef("tg",  "Tiago",       4, 8),
            new VerseRef("tg",  "Tiago",       4, 10),
            new VerseRef("tg",  "Tiago",       4, 14),
            new VerseRef("tg",  "Tiago",       5, 16),
            // ── 1 Pedro ──
            new VerseRef("1pe", "1 Pedro",     1, 3),
            new VerseRef("1pe", "1 Pedro",     1, 7),
            new VerseRef("1pe", "1 Pedro",     1, 8),
            new VerseRef("1pe", "1 Pedro",     1, 15),
            new VerseRef("1pe", "1 Pedro",     2, 9),
            new VerseRef("1pe", "1 Pedro",     2, 24),
            new VerseRef("1pe", "1 Pedro",     3, 15),
            new VerseRef("1pe", "1 Pedro",     4, 8),
            new VerseRef("1pe", "1 Pedro",     4, 10),
            new VerseRef("1pe", "1 Pedro",     5, 6),
            new VerseRef("1pe", "1 Pedro",     5, 7),
            new VerseRef("1pe", "1 Pedro",     5, 8),
            // ── 2 Pedro ──
            new VerseRef("2pe", "2 Pedro",     1, 3),
            new VerseRef("2pe", "2 Pedro",     1, 5),
            new VerseRef("2pe", "2 Pedro",     3, 9),
            new VerseRef("2pe", "2 Pedro",     3, 18),
            // ── 1 João ──
            new VerseRef("1jo", "1 João",      1, 7),
            new VerseRef("1jo", "1 João",      1, 9),
            new VerseRef("1jo", "1 João",      2, 1),
            new VerseRef("1jo", "1 João",      2, 15),
            new VerseRef("1jo", "1 João",      3, 1),
            new VerseRef("1jo", "1 João",      3, 18),
            new VerseRef("1jo", "1 João",      4, 4),
            new VerseRef("1jo", "1 João",      4, 7),
            new VerseRef("1jo", "1 João",      4, 9),
            new VerseRef("1jo", "1 João",      4, 18),
            new VerseRef("1jo", "1 João",      4, 19),
            new VerseRef("1jo", "1 João",      5, 4),
            new VerseRef("1jo", "1 João",      5, 11),
            new VerseRef("1jo", "1 João",      5, 14),
            // ── Judas ──
            new VerseRef("jd",  "Judas",       1, 24),
            // ── Apocalipse ──
            new VerseRef("ap",  "Apocalipse",  1, 8),
            new VerseRef("ap",  "Apocalipse",  3, 20),
            new VerseRef("ap",  "Apocalipse", 21, 4),
            new VerseRef("ap",  "Apocalipse", 22, 13),
            new VerseRef("ap",  "Apocalipse", 22, 20),
        };
    }
}
