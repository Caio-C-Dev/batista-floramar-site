using BatistaFloramar.Domain.Entities;
using BatistaFloramar.Infrastructure.Data;
using BatistaFloramar.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace BatistaFloramar.Controllers
{
    [Route("AreaBatismo/{action=Index}/{id?}")]
    public class AreaBatismoController : Controller
    {
        private readonly BatistaFloramarDbContext _db;
        private const int TotalAulasEsperadas = 12;

        public AreaBatismoController(BatistaFloramarDbContext db)
        {
            _db = db;
        }

        // ── Auth ──────────────────────────────────────────────────────────

        [HttpGet]
        public IActionResult Login()
        {
            if (User.Identity?.IsAuthenticated == true &&
                User.Identity.AuthenticationType == "BatismoCookie")
                return RedirectToAction("Index");
            ViewBag.Title = "Área do Batismo";
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(AreaBatismoLoginViewModel model)
        {
            ViewBag.Title = "Área do Batismo";
            if (!ModelState.IsValid) return View(model);

            var credencial = await _db.AdminCredenciais
                .FirstOrDefaultAsync(c => c.Usuario == model.Usuario && c.Role == "Batismo");

            if (credencial == null || !BCrypt.Net.BCrypt.Verify(model.Senha, credencial.SenhaHash))
            {
                ViewBag.ErroLogin = "Usuário ou senha incorretos.";
                return View(model);
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, model.Usuario),
                new Claim(ClaimTypes.Role, "Batismo")
            };
            var identity = new ClaimsIdentity(claims, "BatismoCookie");
            var principal = new ClaimsPrincipal(identity);
            await HttpContext.SignInAsync("BatismoCookie", principal, new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddHours(12)
            });

            return RedirectToAction("Index");
        }

        [Authorize(AuthenticationSchemes = "BatismoCookie")]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync("BatismoCookie");
            return RedirectToAction("Login");
        }

        // ── Dashboard ─────────────────────────────────────────────────────

        [Authorize(AuthenticationSchemes = "BatismoCookie")]
        public async Task<IActionResult> Index()
        {
            ViewBag.Title = "Área do Batismo";
            ViewBag.Section = "dashboard";

            var aulasRealizadas = await _db.AulasBatismo.OrderBy(a => a.NumeroAula).ToListAsync();
            var numerosRealizados = aulasRealizadas.Select(a => a.NumeroAula).ToHashSet();
            var aulasFaltando = Enumerable.Range(1, TotalAulasEsperadas)
                .Where(n => !numerosRealizados.Contains(n))
                .ToList();

            ViewBag.TotalAulas = aulasRealizadas.Count;
            ViewBag.TotalBatizados = await _db.BatizadosHistorico.CountAsync();
            ViewBag.AulasFaltando = aulasFaltando;
            ViewBag.TotalAulasEsperadas = TotalAulasEsperadas;
            ViewBag.ProximaAula = aulasFaltando.Any() ? aulasFaltando.First() : (int?)null;

            return View(aulasRealizadas);
        }

        // ── Aulas ─────────────────────────────────────────────────────────

        [Authorize(AuthenticationSchemes = "BatismoCookie")]
        public async Task<IActionResult> Aulas()
        {
            ViewBag.Title = "Aulas Realizadas";
            ViewBag.Section = "aulas";

            var aulas = await _db.AulasBatismo
                .Include(a => a.Presencas)
                .OrderBy(a => a.NumeroAula)
                .ToListAsync();

            var numerosRealizados = aulas.Select(a => a.NumeroAula).ToHashSet();
            ViewBag.AulasFaltando = Enumerable.Range(1, TotalAulasEsperadas)
                .Where(n => !numerosRealizados.Contains(n))
                .ToList();
            ViewBag.TotalAulasEsperadas = TotalAulasEsperadas;

            return View(aulas);
        }

        [Authorize(AuthenticationSchemes = "BatismoCookie")]
        [HttpGet]
        public IActionResult NovaAula()
        {
            ViewBag.Title = "Registrar Aula";
            ViewBag.Section = "aulas";
            return View(new NovaAulaViewModel { DataAula = DateTime.Today });
        }

        [Authorize(AuthenticationSchemes = "BatismoCookie")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> NovaAula(NovaAulaViewModel model)
        {
            ViewBag.Title = "Registrar Aula";
            ViewBag.Section = "aulas";

            if (!ModelState.IsValid) return View(model);

            var aula = new AulaBatismo
            {
                Titulo = model.Titulo,
                NumeroAula = model.NumeroAula,
                DataAula = model.DataAula,
                ProfessorNome = model.ProfessorNome,
                Observacoes = model.Observacoes,
                CriadoEm = DateTime.UtcNow
            };

            _db.AulasBatismo.Add(aula);
            await _db.SaveChangesAsync();

            if (!string.IsNullOrWhiteSpace(model.PresentesTexto))
            {
                var presentes = model.PresentesTexto
                    .Split('\n', StringSplitOptions.RemoveEmptyEntries)
                    .Select(n => n.Trim())
                    .Where(n => !string.IsNullOrEmpty(n));
                foreach (var nome in presentes)
                    _db.PresencasAulaBatismo.Add(new PresencaAulaBatismo { AulaBatismoId = aula.Id, NomePessoa = nome, Presente = true });
            }

            if (!string.IsNullOrWhiteSpace(model.AusentesTexto))
            {
                var ausentes = model.AusentesTexto
                    .Split('\n', StringSplitOptions.RemoveEmptyEntries)
                    .Select(n => n.Trim())
                    .Where(n => !string.IsNullOrEmpty(n));
                foreach (var nome in ausentes)
                    _db.PresencasAulaBatismo.Add(new PresencaAulaBatismo { AulaBatismoId = aula.Id, NomePessoa = nome, Presente = false });
            }

            await _db.SaveChangesAsync();

            TempData["Sucesso"] = $"Aula {model.NumeroAula} — \"{model.Titulo}\" registrada com sucesso!";
            return RedirectToAction("Aulas");
        }

        [Authorize(AuthenticationSchemes = "BatismoCookie")]
        public async Task<IActionResult> DetalhesAula(int id)
        {
            var aula = await _db.AulasBatismo
                .Include(a => a.Presencas.OrderBy(p => p.NomePessoa))
                .FirstOrDefaultAsync(a => a.Id == id);

            if (aula == null) return NotFound();

            ViewBag.Title = $"Aula {aula.NumeroAula} — {aula.Titulo}";
            ViewBag.Section = "aulas";
            return View(aula);
        }

        [Authorize(AuthenticationSchemes = "BatismoCookie")]
        [HttpGet]
        public async Task<IActionResult> EditarAula(int id)
        {
            var aula = await _db.AulasBatismo
                .Include(a => a.Presencas)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (aula == null) return NotFound();

            ViewBag.Title = "Editar Aula";
            ViewBag.Section = "aulas";
            ViewBag.AulaId = id;

            var presentes = aula.Presencas.Where(p => p.Presente).Select(p => p.NomePessoa);
            var ausentes = aula.Presencas.Where(p => !p.Presente).Select(p => p.NomePessoa);

            var model = new NovaAulaViewModel
            {
                Titulo = aula.Titulo,
                NumeroAula = aula.NumeroAula,
                DataAula = aula.DataAula,
                ProfessorNome = aula.ProfessorNome,
                Observacoes = aula.Observacoes,
                PresentesTexto = string.Join("\n", presentes),
                AusentesTexto = string.Join("\n", ausentes)
            };

            return View(model);
        }

        [Authorize(AuthenticationSchemes = "BatismoCookie")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditarAula(int id, NovaAulaViewModel model)
        {
            ViewBag.Title = "Editar Aula";
            ViewBag.Section = "aulas";
            ViewBag.AulaId = id;

            if (!ModelState.IsValid) return View(model);

            var aula = await _db.AulasBatismo
                .Include(a => a.Presencas)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (aula == null) return NotFound();

            aula.Titulo = model.Titulo;
            aula.NumeroAula = model.NumeroAula;
            aula.DataAula = model.DataAula;
            aula.ProfessorNome = model.ProfessorNome;
            aula.Observacoes = model.Observacoes;

            _db.PresencasAulaBatismo.RemoveRange(aula.Presencas);

            if (!string.IsNullOrWhiteSpace(model.PresentesTexto))
            {
                var presentes = model.PresentesTexto
                    .Split('\n', StringSplitOptions.RemoveEmptyEntries)
                    .Select(n => n.Trim()).Where(n => !string.IsNullOrEmpty(n));
                foreach (var nome in presentes)
                    _db.PresencasAulaBatismo.Add(new PresencaAulaBatismo { AulaBatismoId = id, NomePessoa = nome, Presente = true });
            }

            if (!string.IsNullOrWhiteSpace(model.AusentesTexto))
            {
                var ausentes = model.AusentesTexto
                    .Split('\n', StringSplitOptions.RemoveEmptyEntries)
                    .Select(n => n.Trim()).Where(n => !string.IsNullOrEmpty(n));
                foreach (var nome in ausentes)
                    _db.PresencasAulaBatismo.Add(new PresencaAulaBatismo { AulaBatismoId = id, NomePessoa = nome, Presente = false });
            }

            await _db.SaveChangesAsync();

            TempData["Sucesso"] = "Aula atualizada com sucesso!";
            return RedirectToAction("DetalhesAula", new { id });
        }

        [Authorize(AuthenticationSchemes = "BatismoCookie")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ExcluirAula(int id)
        {
            var aula = await _db.AulasBatismo.FindAsync(id);
            if (aula != null)
            {
                _db.AulasBatismo.Remove(aula);
                await _db.SaveChangesAsync();
                TempData["Sucesso"] = "Aula excluída.";
            }
            return RedirectToAction("Aulas");
        }

        // ── Batizados ─────────────────────────────────────────────────────

        [Authorize(AuthenticationSchemes = "BatismoCookie")]
        public async Task<IActionResult> Batizados()
        {
            ViewBag.Title = "Histórico de Batizados";
            ViewBag.Section = "batizados";

            var batizados = await _db.BatizadosHistorico
                .OrderByDescending(b => b.DataBatismo)
                .ToListAsync();

            return View(batizados);
        }

        [Authorize(AuthenticationSchemes = "BatismoCookie")]
        [HttpGet]
        public IActionResult NovoBatizado()
        {
            ViewBag.Title = "Registrar Batizado";
            ViewBag.Section = "batizados";
            return View(new NovoBatizadoViewModel { DataBatismo = DateTime.Today });
        }

        [Authorize(AuthenticationSchemes = "BatismoCookie")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> NovoBatizado(NovoBatizadoViewModel model)
        {
            ViewBag.Title = "Registrar Batizado";
            ViewBag.Section = "batizados";

            if (!ModelState.IsValid) return View(model);

            _db.BatizadosHistorico.Add(new BatizadoHistorico
            {
                Nome = model.Nome,
                DataBatismo = model.DataBatismo,
                WhatsApp = model.WhatsApp,
                Observacoes = model.Observacoes,
                CriadoEm = DateTime.UtcNow
            });
            await _db.SaveChangesAsync();

            TempData["Sucesso"] = $"{model.Nome} adicionado ao histórico de batizados!";
            return RedirectToAction("Batizados");
        }

        [Authorize(AuthenticationSchemes = "BatismoCookie")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ExcluirBatizado(int id)
        {
            var batizado = await _db.BatizadosHistorico.FindAsync(id);
            if (batizado != null)
            {
                _db.BatizadosHistorico.Remove(batizado);
                await _db.SaveChangesAsync();
                TempData["Sucesso"] = "Registro removido.";
            }
            return RedirectToAction("Batizados");
        }
    }
}
