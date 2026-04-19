using BatistaFloramar.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BatistaFloramar.Controllers
{
    public class GaleriaController : Controller
    {
        private readonly BatistaFloramarDbContext _db;

        public GaleriaController(BatistaFloramarDbContext db)
        {
            _db = db;
        }

        public async Task<IActionResult> Index()
        {
            ViewBag.Title = "Galeria de Fotos | Comunidade Batista Floramar BH";
            ViewBag.MetaDescription = "Veja os momentos especiais da Comunidade Batista Floramar: cultos, eventos, batismos e muito mais. Igreja batista em Floramar, Belo Horizonte.";
            var albuns = await _db.GaleriaAlbuns
                .Include(a => a.Fotos)
                .OrderByDescending(a => a.Data)
                .ToListAsync();
            return View(albuns);
        }

        public async Task<IActionResult> Album(int id)
        {
            var album = await _db.GaleriaAlbuns
                .Include(a => a.Fotos)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (album == null) return NotFound();

            ViewBag.Title = $"{album.Nome} | Galeria | Comunidade Batista Floramar";
            ViewBag.MetaDescription = album.Descricao ?? $"Fotos do álbum {album.Nome} da Comunidade Batista Floramar.";
            return View(album);
        }
    }
}
