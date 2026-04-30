using BatistaFloramar.Domain.Entities;
using BatistaFloramar.Infrastructure;
using BatistaFloramar.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BatistaFloramar.Controllers
{
    [Authorize(AuthenticationSchemes = "AdminCookie")]
    public class AdminSeriesController : Controller
    {
        private readonly BatistaFloramarDbContext _db;
        private readonly IWebHostEnvironment _env;

        public AdminSeriesController(BatistaFloramarDbContext db, IWebHostEnvironment env)
        {
            _db = db;
            _env = env;
        }

        public async Task<IActionResult> Index()
        {
            ViewBag.Title = "Séries de Mensagens";
            ViewBag.AdminSection = "series";
            var lista = await _db.SeriesMensagens
                .OrderBy(s => s.Ordem)
                .ThenByDescending(s => s.CriadoEm)
                .ToListAsync();
            return View(lista);
        }

        [HttpGet]
        public IActionResult Nova()
        {
            ViewBag.Title = "Nova Série";
            ViewBag.AdminSection = "series";
            return View(new SerieMensagem());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Nova(SerieMensagem model, IFormFile? capa)
        {
            ViewBag.Title = "Nova Série";
            ViewBag.AdminSection = "series";

            if (string.IsNullOrWhiteSpace(model.Nome))
                ModelState.AddModelError("Nome", "Informe o nome da série.");
            if (string.IsNullOrWhiteSpace(model.PlaylistId))
                ModelState.AddModelError("PlaylistId", "Informe o ID da playlist do YouTube.");

            if (!ModelState.IsValid) return View(model);

            if (capa != null && capa.Length > 0)
                model.ImagemCapa = await SalvarCapaAsync(capa);

            model.Slug = await SlugHelper.GerarUnicoAsync(model.Nome, null,
                async (s, _) => await _db.SeriesMensagens.AnyAsync(x => x.Slug == s));
            model.CriadoEm = DateTime.UtcNow;
            model.Ativo = true;
            _db.SeriesMensagens.Add(model);
            await _db.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Série \"{model.Nome}\" criada!";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Editar(int id)
        {
            ViewBag.Title = "Editar Série";
            ViewBag.AdminSection = "series";
            var serie = await _db.SeriesMensagens.FindAsync(id);
            if (serie == null) return NotFound();
            return View(serie);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Editar(int id, SerieMensagem model, IFormFile? capa)
        {
            ViewBag.Title = "Editar Série";
            ViewBag.AdminSection = "series";

            var serie = await _db.SeriesMensagens.FindAsync(id);
            if (serie == null) return NotFound();

            if (string.IsNullOrWhiteSpace(model.Nome))
                ModelState.AddModelError("Nome", "Informe o nome da série.");
            if (string.IsNullOrWhiteSpace(model.PlaylistId))
                ModelState.AddModelError("PlaylistId", "Informe o ID da playlist do YouTube.");

            if (!ModelState.IsValid)
            {
                model.ImagemCapa = serie.ImagemCapa;
                return View(model);
            }

            serie.Nome = model.Nome;
            serie.Slug = await SlugHelper.GerarUnicoAsync(model.Nome, serie.Id,
                async (s, excId) => await _db.SeriesMensagens.AnyAsync(x => x.Slug == s && x.Id != excId));
            serie.PlaylistId = model.PlaylistId;
            serie.Descricao = model.Descricao;
            serie.Ativo = model.Ativo;
            serie.Ordem = model.Ordem;

            if (capa != null && capa.Length > 0)
                serie.ImagemCapa = await SalvarCapaAsync(capa);

            await _db.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Série \"{serie.Nome}\" atualizada!";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Excluir(int id)
        {
            var serie = await _db.SeriesMensagens.FindAsync(id);
            if (serie != null)
            {
                _db.SeriesMensagens.Remove(serie);
                await _db.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Série \"{serie.Nome}\" excluída.";
            }
            return RedirectToAction(nameof(Index));
        }

        private async Task<string> SalvarCapaAsync(IFormFile file)
        {
            var pasta = Path.Combine(_env.WebRootPath, "images", "uploads", "series");
            var nome = await ImageOptimizer.SaveOptimizedAsync(file, pasta);
            return $"/images/uploads/series/{nome}";
        }
    }
}
