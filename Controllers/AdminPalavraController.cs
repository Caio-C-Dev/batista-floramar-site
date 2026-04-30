using BatistaFloramar.Domain.Entities;
using BatistaFloramar.Infrastructure;
using BatistaFloramar.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BatistaFloramar.Controllers
{
    [Authorize(AuthenticationSchemes = "AdminCookie")]
    public class AdminPalavraController : Controller
    {
        private readonly BatistaFloramarDbContext _db;
        private readonly IWebHostEnvironment _env;

        public AdminPalavraController(BatistaFloramarDbContext db, IWebHostEnvironment env)
        {
            _db = db;
            _env = env;
        }

        public async Task<IActionResult> Index()
        {
            ViewBag.Title = "Palavra do Pastor";
            ViewBag.AdminSection = "palavra";
            var lista = await _db.PalavrasDoPastor
                .OrderByDescending(p => p.DataPublicacao)
                .ToListAsync();
            return View(lista);
        }

        [HttpGet]
        public IActionResult Nova()
        {
            ViewBag.Title = "Nova Palavra";
            ViewBag.AdminSection = "palavra";
            return View(new PalavraDoPastor
            {
                AutorNome = "Pr. Rodrigo N. Ferreira",
                DataPublicacao = DateTime.Today
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Nova(PalavraDoPastor model, IFormFile? imagem)
        {
            ViewBag.Title = "Nova Palavra";
            ViewBag.AdminSection = "palavra";

            if (string.IsNullOrWhiteSpace(model.Titulo))
                ModelState.AddModelError("Titulo", "Informe o título.");
            if (string.IsNullOrWhiteSpace(model.Conteudo))
                ModelState.AddModelError("Conteudo", "O conteúdo não pode estar vazio.");

            if (!ModelState.IsValid) return View(model);

            if (imagem != null && imagem.Length > 0)
                model.ImagemDestaque = await SalvarImagemAsync(imagem);

            model.Slug = await SlugHelper.GerarUnicoAsync(model.Titulo, null,
                async (s, _) => await _db.PalavrasDoPastor.AnyAsync(p => p.Slug == s));
            model.CriadoEm = DateTime.UtcNow;
            _db.PalavrasDoPastor.Add(model);
            await _db.SaveChangesAsync();

            TempData["SuccessMessage"] = $"\"{model.Titulo}\" salvo com sucesso!";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Editar(int id)
        {
            ViewBag.Title = "Editar Palavra";
            ViewBag.AdminSection = "palavra";
            var palavra = await _db.PalavrasDoPastor.FindAsync(id);
            if (palavra == null) return NotFound();
            return View(palavra);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Editar(int id, PalavraDoPastor model, IFormFile? imagem)
        {
            ViewBag.Title = "Editar Palavra";
            ViewBag.AdminSection = "palavra";

            var palavra = await _db.PalavrasDoPastor.FindAsync(id);
            if (palavra == null) return NotFound();

            if (string.IsNullOrWhiteSpace(model.Titulo))
                ModelState.AddModelError("Titulo", "Informe o título.");
            if (string.IsNullOrWhiteSpace(model.Conteudo))
                ModelState.AddModelError("Conteudo", "O conteúdo não pode estar vazio.");

            if (!ModelState.IsValid)
            {
                model.ImagemDestaque = palavra.ImagemDestaque;
                return View(model);
            }

            palavra.Titulo = model.Titulo;
            palavra.Slug = await SlugHelper.GerarUnicoAsync(model.Titulo, palavra.Id,
                async (s, excId) => await _db.PalavrasDoPastor.AnyAsync(p => p.Slug == s && p.Id != excId));
            palavra.Conteudo = model.Conteudo;
            palavra.Tipo = model.Tipo;
            palavra.AutorNome = model.AutorNome;
            palavra.DataPublicacao = model.DataPublicacao;
            palavra.Publicado = model.Publicado;

            if (imagem != null && imagem.Length > 0)
                palavra.ImagemDestaque = await SalvarImagemAsync(imagem);

            await _db.SaveChangesAsync();
            TempData["SuccessMessage"] = $"\"{palavra.Titulo}\" atualizado!";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Excluir(int id)
        {
            var palavra = await _db.PalavrasDoPastor.FindAsync(id);
            if (palavra != null)
            {
                _db.PalavrasDoPastor.Remove(palavra);
                await _db.SaveChangesAsync();
                TempData["SuccessMessage"] = $"\"{palavra.Titulo}\" excluído.";
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TogglePublicado(int id)
        {
            var palavra = await _db.PalavrasDoPastor.FindAsync(id);
            if (palavra == null) return NotFound();
            palavra.Publicado = !palavra.Publicado;
            await _db.SaveChangesAsync();
            TempData["SuccessMessage"] = palavra.Publicado ? "Publicado no site!" : "Retirado do site.";
            return RedirectToAction(nameof(Index));
        }

        private async Task<string> SalvarImagemAsync(IFormFile file)
        {
            var pasta = Path.Combine(_env.WebRootPath, "images", "uploads", "palavra");
            var nome = await ImageOptimizer.SaveOptimizedAsync(file, pasta);
            return $"/images/uploads/palavra/{nome}";
        }
    }
}
