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
            ViewBag.Title = "Sobre Nós";
            ViewBag.MetaDescription = "Conheça a Comunidade Batista Floramar, igreja bíblica em Belo Horizonte focada na exposição da Palavra. Nossa história, missão e valores.";
            return View();
        }
    }

    public class CultosController : Controller
    {
        public IActionResult Index()
        {
            ViewBag.Title = "Cultos";
            ViewBag.MetaDescription = "Horários de culto na Comunidade Batista Floramar em BH: Domingo 10h e 18h, Quarta-feira 20h. Igreja evangélica no Floramar, Belo Horizonte.";
            return View();
        }
    }

    public class MinisteriosController : Controller
    {
        private readonly BatistaFloramarDbContext _db;
        public MinisteriosController(BatistaFloramarDbContext db) => _db = db;

        public async Task<IActionResult> Index()
        {
            ViewBag.Title = "Ministérios";
            ViewBag.MetaDescription = "Conheça os ministérios da Comunidade Batista Floramar e descubra como servir com seus dons na obra de Deus em Belo Horizonte.";
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
            ViewBag.Title = "Doação";
            ViewBag.MetaDescription = "Contribua com a Comunidade Batista Floramar e ajude a sustentar a obra de Deus em Belo Horizonte. Doação via Pix ou cartão.";
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
            ViewBag.Title = "Células de Crescimento";
            ViewBag.MetaDescription = "Participe de uma célula de crescimento da Comunidade Batista Floramar. Pequenos grupos de discipulado, comunhão e estudo da Bíblia em Belo Horizonte.";
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
            ViewBag.Title = "Ao Vivo";
            ViewBag.MetaDescription = "Assista ao culto ao vivo da Comunidade Batista Floramar. Transmissões dos cultos de domingo e quarta-feira em Belo Horizonte.";
            return View();
        }
    }

    public class PodcastController : Controller
    {
        private readonly BatistaFloramarDbContext _db;
        public PodcastController(BatistaFloramarDbContext db) => _db = db;

        public async Task<IActionResult> Index()
        {
            ViewBag.Title = "Podcast";
            ViewBag.MetaDescription = "Ouça os podcasts da Comunidade Batista Floramar. Mensagens, devoções e ensinos bíblicos para edificar sua fé.";
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
            ViewBag.Title = "Na Palavra";
            ViewBag.MetaDescription = "Envie sua pergunta ao pastor da Comunidade Batista Floramar. Tire dúvidas sobre a Bíblia, fé e vida cristã.";
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
            ViewBag.Title = "Palavra do Pastor";
            ViewBag.MetaDescription = "Leia as mensagens e reflexões do pastor da Comunidade Batista Floramar. Palavra bíblica expositiva para edificar sua vida em Cristo.";
            var palavras = await _db.PalavrasDoPastor
                .Where(p => p.Publicado)
                .OrderByDescending(p => p.DataPublicacao)
                .ToListAsync();
            return View(palavras);
        }

        public async Task<IActionResult> Detalhe(int id)
        {
            var palavra = await _db.PalavrasDoPastor
                .FirstOrDefaultAsync(p => p.Id == id && p.Publicado);
            if (palavra == null) return NotFound();
            ViewBag.Title = palavra.Titulo;
            ViewBag.MetaDescription = !string.IsNullOrWhiteSpace(palavra.Conteudo)
                ? palavra.Conteudo.Length > 160 ? palavra.Conteudo[..157] + "..." : palavra.Conteudo
                : $"Leia a mensagem \u2018{palavra.Titulo}\u2019 do pastor da Comunidade Batista Floramar.";
            return View(palavra);
        }
    }

    public class SeriesController : Controller
    {
        private readonly BatistaFloramarDbContext _db;
        public SeriesController(BatistaFloramarDbContext db) => _db = db;

        public async Task<IActionResult> Index()
        {
            ViewBag.Title = "Séries de Mensagens";
            ViewBag.MetaDescription = "Explore as séries de mensagens da Comunidade Batista Floramar. Estudo bíblico expositivo em série para seu crescimento espiritual.";
            var series = await _db.SeriesMensagens
                .Where(s => s.Ativo)
                .OrderBy(s => s.Ordem)
                .ThenByDescending(s => s.CriadoEm)
                .ToListAsync();
            return View(series);
        }

        public async Task<IActionResult> Detalhe(int id)
        {
            var serie = await _db.SeriesMensagens
                .FirstOrDefaultAsync(s => s.Id == id && s.Ativo);
            if (serie == null) return NotFound();
            ViewBag.Title = serie.Nome;
            ViewBag.MetaDescription = !string.IsNullOrWhiteSpace(serie.Descricao)
                ? serie.Descricao.Length > 160 ? serie.Descricao[..157] + "..." : serie.Descricao
                : $"Série de mensagens \u2018{serie.Nome}\u2019 da Comunidade Batista Floramar.";
            return View(serie);
        }
    }
}
