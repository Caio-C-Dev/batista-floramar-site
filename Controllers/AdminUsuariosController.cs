using BatistaFloramar.Infrastructure.Data;
using BatistaFloramar.Domain.Entities;
using BatistaFloramar.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BatistaFloramar.Controllers
{
    [Authorize(AuthenticationSchemes = "AdminCookie")]
    public class AdminUsuariosController : Controller
    {
        private const string AdminPrincipal = "AdmCaio";

        private readonly BatistaFloramarDbContext _db;

        public AdminUsuariosController(BatistaFloramarDbContext db)
        {
            _db = db;
        }

        /// <summary>Retorna redirect de acesso negado se o usuário logado não for o AdmCaio.</summary>
        private IActionResult? VerificarAdmCaio()
        {
            if (User.Identity?.Name != AdminPrincipal)
            {
                TempData["ErrorMessage"] = "Acesso restrito ao administrador principal.";
                return RedirectToAction("Index", "Admin");
            }
            return null;
        }

        public async Task<IActionResult> Index()
        {
            var acesso = VerificarAdmCaio();
            if (acesso != null) return acesso;

            ViewBag.AdminSection = "usuarios";
            ViewBag.Title = "Usuários";
            var usuarios = await _db.AdminCredenciais
                .OrderBy(u => u.Usuario)
                .Select(u => new UsuarioListItemViewModel
                {
                    Id = u.Id,
                    Usuario = u.Usuario,
                    Role = u.Role ?? "Admin",
                    CriadoEm = u.CriadoEm
                })
                .ToListAsync();
            return View(usuarios);
        }

        [HttpGet]
        public IActionResult Criar()
        {
            var acesso = VerificarAdmCaio();
            if (acesso != null) return acesso;

            ViewBag.AdminSection = "usuarios";
            ViewBag.Title = "Novo Usuário";
            return View(new CriarUsuarioViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Criar(CriarUsuarioViewModel model)
        {
            var acesso = VerificarAdmCaio();
            if (acesso != null) return acesso;

            ViewBag.AdminSection = "usuarios";
            ViewBag.Title = "Novo Usuário";

            if (!ModelState.IsValid) return View(model);

            var existe = await _db.AdminCredenciais.AnyAsync(u => u.Usuario == model.Usuario);
            if (existe)
            {
                ModelState.AddModelError("Usuario", "Já existe um usuário com esse nome.");
                return View(model);
            }

            _db.AdminCredenciais.Add(new AdminCredencial
            {
                Usuario = model.Usuario,
                SenhaHash = BCrypt.Net.BCrypt.HashPassword(model.Senha),
                Role = string.IsNullOrWhiteSpace(model.Role) ? null : model.Role.Trim(),
                CriadoEm = DateTime.UtcNow
            });
            try
            {
                await _db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Erro ao salvar no banco: {ex.InnerException?.Message ?? ex.Message}");
                return View(model);
            }

            TempData["SuccessMessage"] = $"Usuário '{model.Usuario}' criado com sucesso.";
            return RedirectToAction("Index");
        }

        [HttpGet]
        public async Task<IActionResult> AlterarSenha(int id)
        {
            var acesso = VerificarAdmCaio();
            if (acesso != null) return acesso;

            ViewBag.AdminSection = "usuarios";
            ViewBag.Title = "Alterar Senha";

            var usuario = await _db.AdminCredenciais.FindAsync(id);
            if (usuario == null) return NotFound();

            return View(new AlterarSenhaViewModel { Id = id, Usuario = usuario.Usuario });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AlterarSenha(AlterarSenhaViewModel model)
        {
            var acesso = VerificarAdmCaio();
            if (acesso != null) return acesso;

            ViewBag.AdminSection = "usuarios";
            ViewBag.Title = "Alterar Senha";

            if (!ModelState.IsValid) return View(model);

            var usuario = await _db.AdminCredenciais.FindAsync(model.Id);
            if (usuario == null) return NotFound();

            usuario.SenhaHash = BCrypt.Net.BCrypt.HashPassword(model.NovaSenha);
            try
            {
                await _db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Erro ao salvar no banco: {ex.InnerException?.Message ?? ex.Message}");
                return View(model);
            }

            TempData["SuccessMessage"] = $"Senha do usuário '{usuario.Usuario}' alterada com sucesso.";
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Excluir(int id)
        {
            var acesso = VerificarAdmCaio();
            if (acesso != null) return acesso;

            var total = await _db.AdminCredenciais.CountAsync();
            if (total <= 1)
            {
                TempData["ErrorMessage"] = "Não é possível excluir o único usuário do sistema.";
                return RedirectToAction("Index");
            }

            var usuario = await _db.AdminCredenciais.FindAsync(id);
            if (usuario == null) return NotFound();

            // Impedir auto-exclusão
            var usuarioAtual = User.Identity?.Name;
            if (usuario.Usuario == usuarioAtual)
            {
                TempData["ErrorMessage"] = "Você não pode excluir o seu próprio usuário.";
                return RedirectToAction("Index");
            }

            _db.AdminCredenciais.Remove(usuario);
            await _db.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Usuário '{usuario.Usuario}' excluído.";
            return RedirectToAction("Index");
        }
    }
}
