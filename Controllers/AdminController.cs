using BatistaFloramar.Infrastructure.Data;
using BatistaFloramar.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace BatistaFloramar.Controllers
{
    public class AdminController : Controller
    {
        private readonly BatistaFloramarDbContext _db;

        public AdminController(BatistaFloramarDbContext db)
        {
            _db = db;
        }

        [Authorize(AuthenticationSchemes = "AdminCookie")]
        public async Task<IActionResult> Index()
        {
            ViewBag.AdminSection = "dashboard";
            ViewBag.Title = "Dashboard";
            ViewBag.TotalCelulas = await _db.Celulas.CountAsync(c => c.Ativo);
            ViewBag.TotalMinisterios = await _db.Ministerios.CountAsync(m => m.Ativo);
            ViewBag.TotalPodcasts = await _db.PodcastVideos.CountAsync(p => p.Ativo);
            ViewBag.TotalMensagens = await _db.PerguntasPastor.CountAsync();
            ViewBag.TotalSeries = await _db.SeriesMensagens.CountAsync(s => s.Ativo);
            ViewBag.TotalPalavras = await _db.PalavrasDoPastor.CountAsync(p => p.Publicado);
            ViewBag.TotalEventosSemanais = await _db.EventosSemanais.CountAsync(e => e.Ativo);
            return View();
        }

        [HttpGet]
        public IActionResult Login()
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToAction("Index");
            ViewBag.Title = "Área do Pastor";
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            ViewBag.Title = "Área do Pastor";
            if (!ModelState.IsValid) return View(model);

            var credencial = await _db.AdminCredenciais
                .FirstOrDefaultAsync(c => c.Usuario == model.Usuario);

            if (credencial == null || !BCrypt.Net.BCrypt.Verify(model.Senha, credencial.SenhaHash))
            {
                ViewBag.ErroLogin = "Usuário ou senha incorretos.";
                return View(model);
            }

            var claims = new List<Claim> { new Claim(ClaimTypes.Name, model.Usuario) };
            var identity = new ClaimsIdentity(claims, "AdminCookie");
            var principal = new ClaimsPrincipal(identity);
            await HttpContext.SignInAsync("AdminCookie", principal, new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8)
            });

            return RedirectToAction("Index");
        }

        [Authorize(AuthenticationSchemes = "AdminCookie")]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync("AdminCookie");
            return RedirectToAction("Login");
        }

        // ROTA DE DEBUG TEMPORÁRIA — REMOVER DEPOIS
        [HttpGet]
        public async Task<IActionResult> Debug()
        {
            try
            {
                var credenciais = await _db.AdminCredenciais.ToListAsync();
                var sb = new System.Text.StringBuilder();
                sb.AppendLine($"Total de credenciais no banco: {credenciais.Count}");
                foreach (var c in credenciais)
                {
                    sb.AppendLine($"  Id={c.Id} | Usuario='{c.Usuario}' | HashPreview='{c.SenhaHash[..20]}...' | CriadoEm={c.CriadoEm}");
                    bool hashOk = BCrypt.Net.BCrypt.Verify("Floramar@2026", c.SenhaHash);
                    sb.AppendLine($"  BCrypt.Verify('Floramar@2026') = {hashOk}");
                }
                sb.AppendLine();
                sb.AppendLine($"User.Identity.IsAuthenticated = {User.Identity?.IsAuthenticated}");
                return Content(sb.ToString(), "text/plain");
            }
            catch (Exception ex)
            {
                return Content($"ERRO: {ex.GetType().Name}: {ex.Message}\n\n{ex.StackTrace}", "text/plain");
            }
        }
    }
}
