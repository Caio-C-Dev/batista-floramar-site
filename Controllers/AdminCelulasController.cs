using BatistaFloramar.Domain.Entities;
using BatistaFloramar.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BatistaFloramar.Controllers
{
    [Authorize(AuthenticationSchemes = "AdminCookie")]
    public class AdminCelulasController : Controller
    {
        private readonly BatistaFloramarDbContext _db;
        private readonly IWebHostEnvironment _env;

        public AdminCelulasController(BatistaFloramarDbContext db, IWebHostEnvironment env)
        {
            _db = db;
            _env = env;
        }

        public async Task<IActionResult> Index()
        {
            ViewBag.AdminSection = "celulas";
            ViewBag.Title = "Gerenciar Células";
            var celulas = await _db.Celulas.OrderBy(c => c.DiaSemana).ThenBy(c => c.Ordem).ToListAsync();
            return View(celulas);
        }

        [HttpGet]
        public IActionResult Create()
        {
            ViewBag.AdminSection = "celulas";
            ViewBag.Title = "Nova Célula";
            return View(new Celula());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Celula model, IFormFile? imagem)
        {
            ViewBag.AdminSection = "celulas";
            ViewBag.Title = "Nova Célula";
            if (!ModelState.IsValid) return View(model);

            if (imagem != null && imagem.Length > 0)
                model.ImagemUrl = await SalvarImagemAsync(imagem);

            model.DataCriacao = DateTime.UtcNow;
            _db.Celulas.Add(model);
            await _db.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Célula \"{model.Nome}\" criada com sucesso!";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            ViewBag.AdminSection = "celulas";
            ViewBag.Title = "Editar Célula";
            var celula = await _db.Celulas.FindAsync(id);
            if (celula == null) return NotFound();
            return View(celula);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Celula model, IFormFile? imagem)
        {
            ViewBag.AdminSection = "celulas";
            ViewBag.Title = "Editar Célula";
            if (id != model.Id) return BadRequest();
            if (!ModelState.IsValid) return View(model);

            var celula = await _db.Celulas.FindAsync(id);
            if (celula == null) return NotFound();

            celula.Nome = model.Nome;
            celula.DiaSemana = model.DiaSemana;
            celula.Horario = model.Horario;
            celula.Lideres = model.Lideres;
            celula.LiderNome = string.IsNullOrWhiteSpace(model.LiderNome) ? null : model.LiderNome.Trim();
            celula.Contato = model.Contato;
            celula.Endereco = model.Endereco;
            celula.Descricao = model.Descricao;
            celula.Ordem = model.Ordem;
            celula.Ativo = model.Ativo;
            celula.Latitude = model.Latitude;
            celula.Longitude = model.Longitude;

            if (imagem != null && imagem.Length > 0)
                celula.ImagemUrl = await SalvarImagemAsync(imagem);
            else if (!string.IsNullOrWhiteSpace(model.ImagemUrl))
                celula.ImagemUrl = model.ImagemUrl;

            await _db.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Célula \"{celula.Nome}\" atualizada!";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleAtivo(int id)
        {
            var celula = await _db.Celulas.FindAsync(id);
            if (celula == null) return NotFound();

            celula.Ativo = !celula.Ativo;
            await _db.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Célula \"{celula.Nome}\" {(celula.Ativo ? "ativada" : "desativada")}.";
            return RedirectToAction(nameof(Index));
        }

        private async Task<string> SalvarImagemAsync(IFormFile file)
        {
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            var nome = $"{Guid.NewGuid()}{ext}";
            var pasta = Path.Combine(_env.WebRootPath, "images", "uploads", "celulas");
            Directory.CreateDirectory(pasta);
            var caminho = Path.Combine(pasta, nome);
            using var stream = new FileStream(caminho, FileMode.Create);
            await file.CopyToAsync(stream);
            return $"/images/uploads/celulas/{nome}";
        }
    }
}

