using BatistaFloramar.Domain.Entities;
using BatistaFloramar.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BatistaFloramar.Controllers
{
    [Authorize(AuthenticationSchemes = "AdminCookie")]
    public class AdminGaleriaController : Controller
    {
        private readonly BatistaFloramarDbContext _db;
        private readonly IWebHostEnvironment _env;

        public AdminGaleriaController(BatistaFloramarDbContext db, IWebHostEnvironment env)
        {
            _db = db;
            _env = env;
        }

        // ── Lista ──────────────────────────────────────────────────────────────
        public async Task<IActionResult> Index()
        {
            ViewBag.AdminSection = "galeria";
            ViewBag.Title = "Galeria de Fotos";
            var albuns = await _db.GaleriaAlbuns
                .Include(a => a.Fotos)
                .OrderByDescending(a => a.Data)
                .ToListAsync();
            return View(albuns);
        }

        // ── Novo Álbum ─────────────────────────────────────────────────────────
        [HttpGet]
        public IActionResult NovoAlbum()
        {
            ViewBag.AdminSection = "galeria";
            ViewBag.Title = "Novo Álbum";
            return View(new GaleriaAlbum { Data = DateTime.Today });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> NovoAlbum(GaleriaAlbum model)
        {
            ViewBag.AdminSection = "galeria";
            ViewBag.Title = "Novo Álbum";

            if (string.IsNullOrWhiteSpace(model.Nome))
                ModelState.AddModelError("Nome", "Informe o nome do álbum.");

            if (!ModelState.IsValid) return View(model);

            model.CriadoEm = DateTime.UtcNow;
            _db.GaleriaAlbuns.Add(model);
            await _db.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Álbum \"{model.Nome}\" criado! Agora adicione fotos a ele.";
            return RedirectToAction(nameof(EditarAlbum), new { id = model.Id });
        }

        // ── Editar Álbum + Fotos ───────────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> EditarAlbum(int id)
        {
            ViewBag.AdminSection = "galeria";
            ViewBag.Title = "Editar Álbum";
            var album = await _db.GaleriaAlbuns
                .Include(a => a.Fotos)
                .FirstOrDefaultAsync(a => a.Id == id);
            if (album == null) return NotFound();
            return View(album);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditarAlbum(int id, GaleriaAlbum model, List<IFormFile>? fotos)
        {
            ViewBag.AdminSection = "galeria";
            ViewBag.Title = "Editar Álbum";

            var album = await _db.GaleriaAlbuns
                .Include(a => a.Fotos)
                .FirstOrDefaultAsync(a => a.Id == id);
            if (album == null) return NotFound();

            if (string.IsNullOrWhiteSpace(model.Nome))
                ModelState.AddModelError("Nome", "Informe o nome do álbum.");

            if (!ModelState.IsValid) return View(album);

            album.Nome = model.Nome.Trim();
            album.Descricao = model.Descricao?.Trim();
            album.Data = model.Data;

            // Upload novas fotos
            if (fotos != null && fotos.Any())
            {
                foreach (var foto in fotos)
                {
                    if (foto.Length > 0)
                    {
                        var caminho = await SalvarFotoAsync(foto, id);
                        album.Fotos.Add(new GaleriaFoto { CaminhoArquivo = caminho, AlbumId = id });
                    }
                }
            }

            await _db.SaveChangesAsync();
            TempData["SuccessMessage"] = "Álbum atualizado com sucesso!";
            return RedirectToAction(nameof(EditarAlbum), new { id });
        }

        // ── Excluir Foto ───────────────────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ExcluirFoto(int fotoId, int albumId)
        {
            var foto = await _db.GaleriaFotos.FindAsync(fotoId);
            if (foto != null)
            {
                DeletarArquivo(foto.CaminhoArquivo);
                _db.GaleriaFotos.Remove(foto);
                await _db.SaveChangesAsync();
                TempData["SuccessMessage"] = "Foto excluída.";
            }
            return RedirectToAction(nameof(EditarAlbum), new { id = albumId });
        }

        // ── Excluir Álbum ──────────────────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ExcluirAlbum(int id)
        {
            var album = await _db.GaleriaAlbuns
                .Include(a => a.Fotos)
                .FirstOrDefaultAsync(a => a.Id == id);
            if (album != null)
            {
                foreach (var foto in album.Fotos)
                    DeletarArquivo(foto.CaminhoArquivo);
                _db.GaleriaAlbuns.Remove(album);
                await _db.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Álbum \"{album.Nome}\" excluído.";
            }
            return RedirectToAction(nameof(Index));
        }

        // ── Helpers ────────────────────────────────────────────────────────────
        private async Task<string> SalvarFotoAsync(IFormFile file, int albumId)
        {
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            var nome = $"{Guid.NewGuid()}{ext}";
            var pasta = Path.Combine(_env.WebRootPath, "images", "uploads", "galeria", albumId.ToString());
            Directory.CreateDirectory(pasta);
            var caminho = Path.Combine(pasta, nome);
            using var stream = new FileStream(caminho, FileMode.Create);
            await file.CopyToAsync(stream);
            return $"/images/uploads/galeria/{albumId}/{nome}";
        }

        private void DeletarArquivo(string? caminhoRelativo)
        {
            if (string.IsNullOrEmpty(caminhoRelativo)) return;
            var caminho = Path.Combine(_env.WebRootPath, caminhoRelativo.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
            if (System.IO.File.Exists(caminho))
                System.IO.File.Delete(caminho);
        }
    }
}
