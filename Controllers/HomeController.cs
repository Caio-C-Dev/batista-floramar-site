using BatistaFloramar.Application.Services;
using BatistaFloramar.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BatistaFloramar.Controllers
{
    public class HomeController : Controller
    {
        private readonly YouTubeRssService _youtube;
        private readonly BibleService _bible;
        private readonly BatistaFloramarDbContext _db;
        private readonly IConfiguration _config;

        public HomeController(YouTubeRssService youtube, BibleService bible, BatistaFloramarDbContext db, IConfiguration config)
        {
            _youtube = youtube;
            _bible = bible;
            _db = db;
            _config = config;
        }

        public async Task<IActionResult> Index()
        {
            ViewBag.Title = "Igreja Batista em Belo Horizonte | Comunidade Batista Floramar – Floramar BH";
            ViewBag.MetaDescription = "Igreja evangélica batista no bairro Floramar, Belo Horizonte – MG. Cultos domingo 10h, 18h e quarta 20h. Venha crescer na Palavra e fazer parte da nossa família em Cristo.";
            ViewBag.MetaKeywords = "Floramar, Floramar BH, Jardim Guanabara, igreja Floramar, igreja Jardim Guanabara, igreja batista Floramar, igreja batista Jardim Guanabara, culto Floramar, culto Jardim Guanabara, culto domingo Floramar, igreja batista belo horizonte, igreja Bíblica BH, igreja evangélica belo horizonte, igreja zona norte BH, culto belo horizonte, Comunidade Batista Floramar";
            ViewBag.CanonicalUrl = "https://www.batistafloramar.com.br/";
            ViewBag.LatestVideos = await _youtube.GetLatestVideosAsync(5);
            ViewBag.Versiculo = await _bible.GetVersiculoDoDiaAsync();
            ViewBag.Ministerios = await _db.Ministerios
                .Where(m => m.Ativo)
                .OrderBy(m => m.Ordem)
                .ToListAsync();
            ViewBag.ProximosEventos = await _db.Eventos
                .Where(e => e.Ativo && e.DataEvento >= DateTime.Today)
                .OrderBy(e => e.DataEvento)
                .Take(6)
                .ToListAsync();
            ViewBag.UltimosPodcasts = await _db.PodcastVideos
                .Where(p => p.Ativo)
                .OrderByDescending(p => p.DataPublicacao)
                .Take(3)
                .ToListAsync();
            ViewBag.UltimasPalavras = await _db.PalavrasDoPastor
                .Where(p => p.Publicado)
                .OrderByDescending(p => p.DataPublicacao)
                .Take(3)
                .ToListAsync();
            ViewBag.Celulas = await _db.Celulas
                .Where(c => c.Ativo)
                .OrderBy(c => c.Ordem)
                .ToListAsync();
            ViewBag.EventosSemanais = await _db.EventosSemanais
                .Where(e => e.Ativo)
                .OrderBy(e => e.Ordem)
                .ToListAsync();
            ViewBag.MpPublicKey = _config["MercadoPago:PublicKey"] ?? "";
            return View();
        }
    }
}
