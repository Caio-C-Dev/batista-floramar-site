using BatistaFloramar.Domain.Entities;
using BatistaFloramar.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BatistaFloramar.Controllers
{
    [Authorize(AuthenticationSchemes = "AdminCookie")]
    public class AdminPodcastsController : Controller
    {
        private readonly BatistaFloramarDbContext _db;
        public AdminPodcastsController(BatistaFloramarDbContext db) => _db = db;

        public async Task<IActionResult> Index()
        {
            ViewBag.AdminSection = "podcasts";
            ViewBag.Title = "Gerenciar Podcasts";
            var videos = await _db.PodcastVideos.OrderBy(v => v.Ordem).ToListAsync();
            return View(videos);
        }

        [HttpGet]
        public IActionResult Create()
        {
            ViewBag.AdminSection = "podcasts";
            ViewBag.Title = "Novo Vídeo / Podcast";
            return View(new PodcastVideo());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(PodcastVideo model)
        {
            ViewBag.AdminSection = "podcasts";
            ViewBag.Title = "Novo Vídeo / Podcast";
            if (!ModelState.IsValid) return View(model);

            model.DataPublicacao = DateTime.UtcNow;
            _db.PodcastVideos.Add(model);
            await _db.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Vídeo \"{model.Titulo}\" adicionado!";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            ViewBag.AdminSection = "podcasts";
            ViewBag.Title = "Editar Vídeo";
            var video = await _db.PodcastVideos.FindAsync(id);
            if (video == null) return NotFound();
            return View(video);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, PodcastVideo model)
        {
            ViewBag.AdminSection = "podcasts";
            ViewBag.Title = "Editar Vídeo";
            if (id != model.Id) return BadRequest();
            if (!ModelState.IsValid) return View(model);

            var existing = await _db.PodcastVideos.FindAsync(id);
            if (existing == null) return NotFound();

            existing.Titulo = model.Titulo;
            existing.Descricao = model.Descricao;
            existing.YoutubeVideoId = model.YoutubeVideoId;
            existing.StartSeconds = model.StartSeconds;
            existing.Ativo = model.Ativo;
            existing.Ordem = model.Ordem;

            await _db.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Vídeo \"{model.Titulo}\" atualizado!";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleAtivo(int id)
        {
            var video = await _db.PodcastVideos.FindAsync(id);
            if (video == null) return NotFound();

            video.Ativo = !video.Ativo;
            await _db.SaveChangesAsync();
            TempData["SuccessMessage"] = $"\"{video.Titulo}\" {(video.Ativo ? "ativado" : "desativado")}.";
            return RedirectToAction(nameof(Index));
        }
    }
}
