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
        public IActionResult Index() => View();
    }

    public class CultosController : Controller
    {
        public IActionResult Index() => View();
    }

    public class MinisteriosController : Controller
    {
        private readonly BatistaFloramarDbContext _db;
        public MinisteriosController(BatistaFloramarDbContext db) => _db = db;

        public async Task<IActionResult> Index()
        {
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
            return View();
        }
    }

    public class PodcastController : Controller
    {
        private readonly BatistaFloramarDbContext _db;
        public PodcastController(BatistaFloramarDbContext db) => _db = db;

        public async Task<IActionResult> Index()
        {
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
            return View(palavra);
        }
    }

    public class SeriesController : Controller
    {
        private readonly BatistaFloramarDbContext _db;
        public SeriesController(BatistaFloramarDbContext db) => _db = db;

        public async Task<IActionResult> Index()
        {
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
            return View(serie);
        }
    }
}
