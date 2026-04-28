using BatistaFloramar.Application.Services;
using BatistaFloramar.Domain.Entities;
using BatistaFloramar.Infrastructure.Data;
using BatistaFloramar.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BatistaFloramar.Controllers
{
    public class SobreController : Controller
    {
        public IActionResult Index()
        {
            ViewBag.Title = "Quem Somos | Igreja Batista em Belo Horizonte – Floramar";
            ViewBag.MetaDescription = "Conheça a Comunidade Batista Floramar, igreja Bíblica evangélica na zona norte de Belo Horizonte. Nossa história, missão e valores fundamentados na Palavra de Deus.";
            ViewBag.MetaKeywords = "quem somos igreja batista floramar, história igreja belo horizonte, missão igeja batista BH, zona norte belo horizonte";
            return View();
        }
    }

    public class CultosController : Controller
    {
        public IActionResult Index()
        {
            ViewBag.Title = "Cultos em Belo Horizonte | Horários | Igreja Batista Floramar";
            ViewBag.MetaDescription = "Horários de culto na Comunidade Batista Floramar, Floramar – BH: Domingo 10h e 18h, Quarta-feira 20h. Igreja evangélica batista perto de você na zona norte de Belo Horizonte.";
            ViewBag.MetaKeywords = "culto belo horizonte horário, culto domingo BH, culto quarta belo horizonte, horário culto floramar, igreja batista culto BH";
            return View();
        }
    }

    public class MinisteriosController : Controller
    {
        private readonly BatistaFloramarDbContext _db;
        public MinisteriosController(BatistaFloramarDbContext db) => _db = db;

        public async Task<IActionResult> Index()
        {
            ViewBag.Title = "Ministérios | Igreja Batista Floramar BH";
            ViewBag.MetaDescription = "Conheça os ministérios da Comunidade Batista Floramar e sirva com seus dons. Igreja evangélica em Belo Horizonte com ministérios de música, jovens, crianças e mais.";
            var ministerios = await _db.Ministerios
                .Where(m => m.Ativo)
                .OrderBy(m => m.Ordem)
                .ToListAsync();
            return View(ministerios);
        }

        public async Task<IActionResult> Detalhes(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return NotFound();

            var ministerio = await _db.Ministerios
                .Include(m => m.Fotos)
                .FirstOrDefaultAsync(m => m.Slug == id && m.Ativo);

            if (ministerio == null) return NotFound();
            ViewBag.Title = ministerio.Nome;
            ViewBag.MetaDescription = !string.IsNullOrWhiteSpace(ministerio.Descricao)
                ? ministerio.Descricao.Length > 160 ? ministerio.Descricao[..157] + "..." : ministerio.Descricao
                : $"Saiba mais sobre o ministério {ministerio.Nome} da Comunidade Batista Floramar em Belo Horizonte.";
            return View(ministerio);
        }
    }

    public class DoacaoController : Controller
    {
        private readonly DoacaoService _doacaoService;
        private readonly IConfiguration _config;

        public DoacaoController(DoacaoService doacaoService, IConfiguration config)
        {
            _doacaoService = doacaoService;
            _config = config;
        }

        public IActionResult Index()
        {
            ViewBag.Title = "Contribua com a Igreja | Doação | Comunidade Batista Floramar BH";
            ViewBag.MetaDescription = "Contribua com a Comunidade Batista Floramar e ajude a sustentar a obra de Deus em Belo Horizonte. Doação via Pix ou cartão.";
            ViewBag.MetaKeywords = "doação igreja belo horizonte, contribuição igreja batista BH, missão igreja floramar";
            ViewBag.MpPublicKey = _config["MercadoPago:PublicKey"] ?? "";
            return View();
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> CriarPagamento([FromBody] DoacaoPaymentRequest request)
        {
            if (request is null || request.TransactionAmount < 1)
                return Json(new { sucesso = false, mensagem = "Valor inválido." });

            var (sucesso, mensagem, _) = await _doacaoService.CriarPagamentoAsync(request);
            return Json(new { sucesso, mensagem });
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> Webhook([FromQuery] string? type, [FromQuery(Name = "data.id")] string? dataId)
        {
            if (type == "payment" && long.TryParse(dataId, out var id))
                await _doacaoService.ProcessarWebhookAsync(id);

            return Ok();
        }
    }

    public class CelulaController : Controller
    {
        private readonly BatistaFloramarDbContext _db;
        public CelulaController(BatistaFloramarDbContext db) => _db = db;

        public async Task<IActionResult> Index()
        {
            ViewBag.Title = "Células de Crescimento em BH | Grupo Bíblico | Igreja Batista Floramar";
            ViewBag.MetaDescription = "Participe de uma célula de crescimento da Comunidade Batista Floramar em Belo Horizonte. Pequenos grupos de discipulado e estudo bíblico perto de você.";
            ViewBag.MetaKeywords = "célula de crescimento belo horizonte, pequeno grupo bíblico BH, discipulado belo horizonte, grupo de estudo bíblico floramar, célula batista BH";
            var celulas = await _db.Celulas
                .Where(c => c.Ativo)
                .OrderBy(c => c.Ordem)
                .ToListAsync();
            return View(celulas);
        }
    }

    public class MissaoController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }

    public class AovivoController : Controller
    {
        public IActionResult Index()
        {
            ViewBag.Title = "Culto Ao Vivo | Transmissão | Igreja Batista Floramar BH";
            ViewBag.MetaDescription = "Assista ao culto ao vivo da Comunidade Batista Floramar. Transmissões dos cultos de domingo e quarta-feira em Belo Horizonte.";
            ViewBag.MetaKeywords = "culto ao vivo belo horizonte, Igreja ao vivo BH, culto online batista, transmissão culto floramar";
            return View();
        }
    }

    public class PodcastController : Controller
    {
        private readonly BatistaFloramarDbContext _db;
        public PodcastController(BatistaFloramarDbContext db) => _db = db;

        public async Task<IActionResult> Index()
        {
            ViewBag.Title = "Podcast Bíblico | Mensagens | Comunidade Batista Floramar BH";
            ViewBag.MetaDescription = "Ouça os podcasts da Comunidade Batista Floramar. Mensagens, devoções e ensinos bíblicos para edificar sua fé.";
            ViewBag.MetaKeywords = "podcast bíblico belo horizonte, podcast evangelico BH, ensino bíblico podcast, mensagem batista BH";
            var videos = await _db.PodcastVideos
                .Where(v => v.Ativo)
                .OrderBy(v => v.Ordem)
                .ToListAsync();
            return View(videos);
        }
    }

    public class NapalavraController : Controller
    {
        private readonly BatistaFloramarDbContext _dbContext;

        public NapalavraController(BatistaFloramarDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        [HttpGet]
        public IActionResult Index()
        {
            ViewBag.Title = "Fale com o Pastor | Contato | Igreja Batista Floramar BH";
            ViewBag.MetaDescription = "Entre em contato com a Comunidade Batista Floramar. Envie sua pergunta ao pastor ou solicite informações sobre a igreja em Belo Horizonte.";
            ViewBag.MetaKeywords = "contato igreja belo horizonte, falar com pastor BH, perguntas Bíblicas, contato batista floramar";
            return View(new PerguntaPastorViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(PerguntaPastorViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var pergunta = new PerguntaPastor
            {
                Nome = model.Nome,
                Email = model.Email,
                Telefone = model.Telefone,
                Assunto = model.Assunto,
                Pergunta = model.Pergunta,
                DataEnvio = DateTime.UtcNow
            };

            _dbContext.PerguntasPastor.Add(pergunta);
            await _dbContext.SaveChangesAsync();

            TempData["SuccessMessage"] = "Sua pergunta foi enviada com sucesso! Em breve nossa equipe encaminhará ao pastor.";
            return RedirectToAction(nameof(Index));
        }
    }

    public class NoticiasController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }

    public class LojaController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }

    public class PalavraDoPastorController : Controller
    {
        private readonly BatistaFloramarDbContext _db;
        public PalavraDoPastorController(BatistaFloramarDbContext db) => _db = db;

        public async Task<IActionResult> Index()
        {
            ViewBag.Title = "Palavra do Pastor | Igreja Batista Floramar BH";
            ViewBag.MetaDescription = "Reflexões e mensagens Bíblicas do pastor da Comunidade Batista Floramar em Belo Horizonte. Pregação expositiva séria e acessível para sua vida.";
            var palavras = await _db.PalavrasDoPastor
                .Where(p => p.Publicado)
                .OrderByDescending(p => p.DataPublicacao)
                .ToListAsync();
            return View(palavras);
        }

        public async Task<IActionResult> Detalhe(string id)
        {
            var palavra = await _db.PalavrasDoPastor
                .FirstOrDefaultAsync(p => p.Slug == id && p.Publicado);

            // Fallback: numeric ID for old links
            if (palavra == null && int.TryParse(id, out var legacyId))
                palavra = await _db.PalavrasDoPastor
                    .FirstOrDefaultAsync(p => p.Id == legacyId && p.Publicado);

            if (palavra == null) return NotFound();

            // Redirect to canonical slug URL if accessed via numeric ID
            if (id != palavra.Slug)
                return RedirectToActionPermanent(nameof(Detalhe), new { id = palavra.Slug });

            ViewBag.Title = palavra.Titulo;
            var _plain = System.Text.RegularExpressions.Regex.Replace(palavra.Conteudo ?? string.Empty, "<.*?>", string.Empty);
            ViewBag.MetaDescription = _plain.Length > 0
                ? (_plain.Length > 160 ? _plain[..157] + "..." : _plain)
                : string.Format("Leia a mensagem ‘{0}’ do pastor da Comunidade Batista Floramar.", palavra.Titulo);
            ViewBag.CanonicalUrl = string.Format("https://www.batistafloramar.com.br/PalavraDoPastor/Detalhe/{0}", palavra.Slug);
            ViewBag.OgType = "article";

            // Resolve og:image: featured image → first <img> in content → site default
            string? ogImg = null;
            if (!string.IsNullOrEmpty(palavra.ImagemDestaque))
            {
                ogImg = palavra.ImagemDestaque.StartsWith("http")
                    ? palavra.ImagemDestaque
                    : "https://www.batistafloramar.com.br" + palavra.ImagemDestaque;
            }
            else
            {
                var srcMatch = System.Text.RegularExpressions.Regex.Match(
                    palavra.Conteudo ?? string.Empty,
                    @"<img[^>]+src=[""’]([^""’]+)[""’]",
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                if (srcMatch.Success)
                {
                    var src = srcMatch.Groups[1].Value;
                    ogImg = src.StartsWith("http")
                        ? src
                        : "https://www.batistafloramar.com.br" + src;
                }
            }
            if (ogImg != null)
            {
                ViewBag.OgImage       = ogImg;
                ViewBag.OgImageWidth  = "1200";
                ViewBag.OgImageHeight = "630";
            }
            return View(palavra);
        }
    }
    public class SeriesController : Controller
    {
        private readonly BatistaFloramarDbContext _db;
        public SeriesController(BatistaFloramarDbContext db) => _db = db;

        public async Task<IActionResult> Index()
        {
            ViewBag.Title = "Séries de Mensagens Bíblicas | Batista Floramar BH";
            ViewBag.MetaDescription = "Assista e acompanhe as séries de mensagens Bíblicas da Comunidade Batista Floramar em Belo Horizonte. Pregação expositiva séria para seu crescimento espiritual.";
            var series = await _db.SeriesMensagens
                .Where(s => s.Ativo)
                .OrderBy(s => s.Ordem)
                .ThenByDescending(s => s.CriadoEm)
                .ToListAsync();
            return View(series);
        }

        public async Task<IActionResult> Detalhe(string id)
        {
            var serie = await _db.SeriesMensagens
                .FirstOrDefaultAsync(s => s.Slug == id && s.Ativo);

            if (serie == null && int.TryParse(id, out var legacyId))
                serie = await _db.SeriesMensagens
                    .FirstOrDefaultAsync(s => s.Id == legacyId && s.Ativo);

            if (serie == null) return NotFound();

            if (id != serie.Slug)
                return RedirectToActionPermanent(nameof(Detalhe), new { id = serie.Slug });

            ViewBag.Title = serie.Nome;
            ViewBag.MetaDescription = !string.IsNullOrWhiteSpace(serie.Descricao)
                ? serie.Descricao.Length > 160 ? serie.Descricao[..157] + "..." : serie.Descricao
                : $"Série de mensagens '{serie.Nome}' da Comunidade Batista Floramar.";
            return View(serie);
        }
    }
}
