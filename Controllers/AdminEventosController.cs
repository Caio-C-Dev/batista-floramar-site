using BatistaFloramar.Domain.Entities;
using BatistaFloramar.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BatistaFloramar.Controllers
{
    [Authorize(AuthenticationSchemes = "AdminCookie")]
    public class AdminEventosController : Controller
    {
        private readonly BatistaFloramarDbContext _db;
        private readonly IWebHostEnvironment _env;

        public AdminEventosController(BatistaFloramarDbContext db, IWebHostEnvironment env)
        {
            _db = db;
            _env = env; 
        }

        // ── Lista ──────────────────────────────────────────────────────────────
        public async Task<IActionResult> Index()
        {
            ViewBag.Title = "Próximos Eventos";
            ViewBag.AdminSection = "eventos";
            var eventos = await _db.Eventos
                .OrderBy(e => e.DataEvento)
                .ToListAsync();
            return View(eventos);
        }

        // ── Novo ───────────────────────────────────────────────────────────────
        [HttpGet]
        public IActionResult NovoEvento()
        {
            ViewBag.Title = "Novo Evento";
            ViewBag.AdminSection = "eventos";
            return View(new Evento { DataEvento = DateTime.Today });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> NovoEvento(Evento model, IFormFile? banner)
        {
            ViewBag.Title = "Novo Evento";
            ViewBag.AdminSection = "eventos";

            if (string.IsNullOrWhiteSpace(model.Titulo))
                ModelState.AddModelError("Titulo", "Informe o título do evento.");

            if (!ModelState.IsValid)
                return View(model);

            if (banner != null && banner.Length > 0)
                model.ImagemBanner = await SalvarBannerAsync(banner);

            model.CriadoEm = DateTime.UtcNow;
            model.Ativo = true;
            _db.Eventos.Add(model);
            await _db.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Evento \"{model.Titulo}\" criado com sucesso!";
            return RedirectToAction(nameof(Index));
        }

        // ── Editar ─────────────────────────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> EditarEvento(int id)
        {
            ViewBag.Title = "Editar Evento";
            ViewBag.AdminSection = "eventos";
            var evento = await _db.Eventos.FindAsync(id);
            if (evento == null) return NotFound();
            return View(evento);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditarEvento(int id, Evento model, IFormFile? banner)
        {
            ViewBag.Title = "Editar Evento";
            ViewBag.AdminSection = "eventos";

            var evento = await _db.Eventos.FindAsync(id);
            if (evento == null) return NotFound();

            if (string.IsNullOrWhiteSpace(model.Titulo))
                ModelState.AddModelError("Titulo", "Informe o título do evento.");

            if (!ModelState.IsValid)
            {
                model.ImagemBanner = evento.ImagemBanner;
                return View(model);
            }

            evento.Titulo = model.Titulo;
            evento.Descricao = model.Descricao;
            evento.DataEvento = model.DataEvento;
            evento.Local = model.Local;
            evento.Ativo = model.Ativo;

            if (banner != null && banner.Length > 0)
            {
                DeletarBannerAntigo(evento.ImagemBanner);
                evento.ImagemBanner = await SalvarBannerAsync(banner);
            }

            await _db.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Evento \"{evento.Titulo}\" atualizado!";
            return RedirectToAction(nameof(Index));
        }

        // ── Excluir ────────────────────────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ExcluirEvento(int id)
        {
            var evento = await _db.Eventos.FindAsync(id);
            if (evento != null)
            {
                DeletarBannerAntigo(evento.ImagemBanner);
                _db.Eventos.Remove(evento);
                await _db.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Evento \"{evento.Titulo}\" excluído.";
            }
            return RedirectToAction(nameof(Index));
        }

        // ── Helpers ────────────────────────────────────────────────────────────
        private async Task<string> SalvarBannerAsync(IFormFile file)
        {
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            var nome = $"{Guid.NewGuid()}{ext}";
            var pasta = Path.Combine(_env.WebRootPath, "images", "eventos");
            Directory.CreateDirectory(pasta);
            var caminho = Path.Combine(pasta, nome);
            using var stream = new FileStream(caminho, FileMode.Create);
            await file.CopyToAsync(stream);
            return $"/images/eventos/{nome}";
        }

        private void DeletarBannerAntigo(string? caminhoRelativo)
        {
            if (string.IsNullOrEmpty(caminhoRelativo)) return;
            var caminho = Path.Combine(_env.WebRootPath, caminhoRelativo.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
            if (System.IO.File.Exists(caminho))
                System.IO.File.Delete(caminho);
        }
    }
}
