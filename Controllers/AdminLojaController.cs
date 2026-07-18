using BatistaFloramar.Domain.Entities;
using BatistaFloramar.Infrastructure;
using BatistaFloramar.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BatistaFloramar.Controllers
{
    [Authorize(AuthenticationSchemes = "AdminCookie")]
    public class AdminLojaController : Controller
    {
        private readonly BatistaFloramarDbContext _db;
        private readonly IWebHostEnvironment _env;

        public AdminLojaController(BatistaFloramarDbContext db, IWebHostEnvironment env)
        {
            _db = db;
            _env = env;
        }

        public async Task<IActionResult> Index()
        {
            ViewBag.AdminSection = "loja";
            ViewBag.Title = "Gerenciar Loja";
            var produtos = await _db.Produtos.OrderBy(p => p.Ordem).ThenByDescending(p => p.CriadoEm).ToListAsync();
            return View(produtos);
        }

        [HttpGet]
        public IActionResult Create()
        {
            ViewBag.AdminSection = "loja";
            ViewBag.Title = "Novo Produto";
            return View(new Produto());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Produto model, IFormFile? imagemArquivo)
        {
            ViewBag.AdminSection = "loja";
            ViewBag.Title = "Novo Produto";
            if (!ModelState.IsValid) return View(model);

            model.Slug = await SlugHelper.GerarUnicoAsync(
                model.Nome, null,
                async (s, id) => await _db.Produtos.AnyAsync(p => p.Slug == s && (!id.HasValue || p.Id != id.Value)));
            model.CriadoEm = DateTime.UtcNow;

            var imgPath = await SalvarImagemAsync(imagemArquivo);
            if (imgPath != null) model.Imagem = imgPath;

            _db.Produtos.Add(model);
            await _db.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Produto \"{model.Nome}\" criado com sucesso!";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            ViewBag.AdminSection = "loja";
            ViewBag.Title = "Editar Produto";
            var produto = await _db.Produtos.FindAsync(id);
            if (produto == null) return NotFound();
            return View(produto);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Produto model, IFormFile? imagemArquivo)
        {
            ViewBag.AdminSection = "loja";
            ViewBag.Title = "Editar Produto";
            if (id != model.Id) return BadRequest();
            if (!ModelState.IsValid) return View(model);

            var atual = await _db.Produtos.FindAsync(id);
            if (atual == null) return NotFound();

            if (atual.Nome != model.Nome)
            {
                atual.Slug = await SlugHelper.GerarUnicoAsync(
                    model.Nome, id,
                    async (s, ident) => await _db.Produtos.AnyAsync(p => p.Slug == s && p.Id != ident!.Value));
            }
            atual.Nome = model.Nome;
            atual.Descricao = model.Descricao;
            atual.Preco = model.Preco;
            atual.Ordem = model.Ordem;
            atual.Ativo = model.Ativo;

            var imgPath = await SalvarImagemAsync(imagemArquivo);
            if (imgPath != null) atual.Imagem = imgPath;

            await _db.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Produto \"{atual.Nome}\" atualizado!";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleAtivo(int id)
        {
            var produto = await _db.Produtos.FindAsync(id);
            if (produto == null) return NotFound();

            produto.Ativo = !produto.Ativo;
            await _db.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Produto \"{produto.Nome}\" {(produto.Ativo ? "ativado" : "desativado")}.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var produto = await _db.Produtos.FindAsync(id);
            if (produto == null) return NotFound();

            if (!string.IsNullOrEmpty(produto.Imagem))
            {
                var full = Path.Combine(_env.WebRootPath, produto.Imagem.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
                if (System.IO.File.Exists(full)) System.IO.File.Delete(full);
            }

            _db.Produtos.Remove(produto);
            await _db.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Produto \"{produto.Nome}\" removido.";
            return RedirectToAction(nameof(Index));
        }

        private async Task<string?> SalvarImagemAsync(IFormFile? arquivo)
        {
            if (arquivo == null || arquivo.Length == 0) return null;

            var ext = Path.GetExtension(arquivo.FileName).ToLowerInvariant();
            var allowed = new[] { ".jpg", ".jpeg", ".png", ".webp" };
            if (!allowed.Contains(ext))
            {
                TempData["ErrorMessage"] = "Formato de imagem não suportado. Use JPG, PNG ou WEBP.";
                return null;
            }
            if (arquivo.Length > 5 * 1024 * 1024)
            {
                TempData["ErrorMessage"] = "Imagem muito grande. Máximo 5 MB.";
                return null;
            }

            var pasta = Path.Combine(_env.WebRootPath, "images", "uploads", "loja");
            Directory.CreateDirectory(pasta);
            var nome = $"{Guid.NewGuid():N}{ext}";
            var caminho = Path.Combine(pasta, nome);
            await using var fs = new FileStream(caminho, FileMode.Create);
            await arquivo.CopyToAsync(fs);
            return $"/images/uploads/loja/{nome}";
        }
    }
}
