using BatistaFloramar.Domain.Entities;
using BatistaFloramar.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BatistaFloramar.Controllers
{
    [Authorize(AuthenticationSchemes = "AdminCookie")]
    public class AdminMinisteriosController : Controller
    {
        private readonly BatistaFloramarDbContext _db;
        private readonly IWebHostEnvironment _env;

        public AdminMinisteriosController(BatistaFloramarDbContext db, IWebHostEnvironment env)
        {
            _db = db;
            _env = env;
        }

        public async Task<IActionResult> Index()
        {
            ViewBag.AdminSection = "ministerios";
            ViewBag.Title = "Gerenciar Ministérios";
            var ministerios = await _db.Ministerios.OrderBy(m => m.Ordem).ToListAsync();
            return View(ministerios);
        }

        [HttpGet]
        public IActionResult Create()
        {
            ViewBag.AdminSection = "ministerios";
            ViewBag.Title = "Novo Ministério";
            return View(new Ministerio());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Ministerio model)
        {
            ViewBag.AdminSection = "ministerios";
            ViewBag.Title = "Novo Ministério";
            if (!ModelState.IsValid) return View(model);

            _db.Ministerios.Add(model);
            await _db.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Ministério \"{model.Nome}\" criado com sucesso!";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            ViewBag.AdminSection = "ministerios";
            ViewBag.Title = "Editar Ministério";
            var ministerio = await _db.Ministerios.FindAsync(id);
            if (ministerio == null) return NotFound();
            return View(ministerio);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Ministerio model)
        {
            ViewBag.AdminSection = "ministerios";
            ViewBag.Title = "Editar Ministério";
            if (id != model.Id) return BadRequest();
            if (!ModelState.IsValid) return View(model);

            _db.Ministerios.Update(model);
            await _db.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Ministério \"{model.Nome}\" atualizado!";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleAtivo(int id)
        {
            var ministerio = await _db.Ministerios.FindAsync(id);
            if (ministerio == null) return NotFound();

            ministerio.Ativo = !ministerio.Ativo;
            await _db.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Ministério \"{ministerio.Nome}\" {(ministerio.Ativo ? "ativado" : "desativado")}.";
            return RedirectToAction(nameof(Index));
        }

        // ── Fotos ─────────────────────────────────────────────────────────────

        [HttpGet]
        public async Task<IActionResult> Fotos(int id)
        {
            ViewBag.AdminSection = "ministerios";
            var ministerio = await _db.Ministerios
                .Include(m => m.Fotos)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (ministerio == null) return NotFound();
            ViewBag.Title = $"Fotos — {ministerio.Nome}";
            return View(ministerio);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadFoto(int id, IFormFile arquivo, string? legenda)
        {
            var ministerio = await _db.Ministerios.FindAsync(id);
            if (ministerio == null) return NotFound();

            if (arquivo == null || arquivo.Length == 0)
            {
                TempData["ErrorMessage"] = "Nenhum arquivo selecionado.";
                return RedirectToAction(nameof(Fotos), new { id });
            }

            var ext = Path.GetExtension(arquivo.FileName).ToLowerInvariant();
            var allowed = new[] { ".jpg", ".jpeg", ".png", ".webp", ".gif" };
            if (!allowed.Contains(ext))
            {
                TempData["ErrorMessage"] = "Formato de arquivo não suportado. Use JPG, PNG, WEBP ou GIF.";
                return RedirectToAction(nameof(Fotos), new { id });
            }

            if (arquivo.Length > 5 * 1024 * 1024)
            {
                TempData["ErrorMessage"] = "Arquivo muito grande. Máximo 5 MB.";
                return RedirectToAction(nameof(Fotos), new { id });
            }

            var pasta = Path.Combine(_env.WebRootPath, "images", "ministerios", ministerio.Slug);
            Directory.CreateDirectory(pasta);

            var nomeArquivo = $"{Guid.NewGuid():N}{ext}";
            var caminho = Path.Combine(pasta, nomeArquivo);

            await using (var fs = new FileStream(caminho, FileMode.Create))
                await arquivo.CopyToAsync(fs);

            _db.MinisterioFotos.Add(new MinisterioFoto
            {
                MinisterioId = id,
                CaminhoArquivo = $"/images/ministerios/{ministerio.Slug}/{nomeArquivo}",
                Legenda = legenda?.Trim(),
                DataUpload = DateTime.UtcNow
            });
            await _db.SaveChangesAsync();

            TempData["SuccessMessage"] = "Foto adicionada com sucesso!";
            return RedirectToAction(nameof(Fotos), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteFoto(int fotoId, int ministerioId)
        {
            var foto = await _db.MinisterioFotos.FindAsync(fotoId);
            if (foto == null) return NotFound();

            // Apaga o arquivo físico
            var fullPath = Path.Combine(_env.WebRootPath, foto.CaminhoArquivo.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
            if (System.IO.File.Exists(fullPath))
                System.IO.File.Delete(fullPath);

            _db.MinisterioFotos.Remove(foto);
            await _db.SaveChangesAsync();
            TempData["SuccessMessage"] = "Foto removida.";
            return RedirectToAction(nameof(Fotos), new { id = ministerioId });
        }
    }
}
