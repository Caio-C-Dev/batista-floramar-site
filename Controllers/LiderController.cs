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
    public class LiderController : Controller
    {
        private readonly BatistaFloramarDbContext _db;

        public LiderController(BatistaFloramarDbContext db)
        {
            _db = db;
        }

        // ── Helpers ──────────────────────────────────────────────────────────────

        private int? GetCelulaIdLogada()
        {
            var claim = User.FindFirstValue("CelulaId");
            return int.TryParse(claim, out var id) ? id : null;
        }

        /// <summary>
        /// Capitaliza apenas a primeira letra; mantém o restante como foi digitado.
        /// Ex.: "joão" → "João", "  joão  " → "João"
        /// </summary>
        private static string CapitalizarPrimeira(string valor)
        {
            var v = valor.Trim();
            if (string.IsNullOrEmpty(v)) return v;
            return char.ToUpper(v[0]) + v.Substring(1);
        }

        // ── Login ─────────────────────────────────────────────────────────────────

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            // Verifica somente o cookie de líder (não o de admin)
            if (User.FindFirstValue("CelulaId") != null)
                return RedirectToAction("Dashboard");

            ViewBag.ReturnUrl = returnUrl;
            return View(new LiderLoginViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LiderLoginViewModel model, string? returnUrl = null)
        {
            if (!ModelState.IsValid)
                return View(model);

            var nomeCelula = model.NomeCelula.Trim();
            var senhaDigitada = CapitalizarPrimeira(model.NomeLider);

            // Busca célula pelo nome (case-insensitive)
            var celula = await _db.Celulas
                .FirstOrDefaultAsync(c => c.Nome.Trim().ToLower() == nomeCelula.ToLower() && c.Ativo);

            if (celula == null || string.IsNullOrWhiteSpace(celula.LiderNome))
            {
                ModelState.AddModelError(string.Empty, "Célula ou líder não encontrado. Verifique os dados informados.");
                return View(model);
            }

            // MVP: verifica se nome digitado (primeira letra maiúscula) bate com LiderNome cadastrado
            // Futuro: substituir por BCrypt.Verify(senhaDigitada, celula.LiderNomeSenhaHash)
            var senhaCorreta = CapitalizarPrimeira(celula.LiderNome);
            if (senhaDigitada != senhaCorreta)
            {
                ModelState.AddModelError(string.Empty, "Célula ou líder não encontrado. Verifique os dados informados.");
                return View(model);
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, celula.LiderNome),
                new Claim("CelulaId", celula.Id.ToString()),
                new Claim("NomeCelula", celula.Nome)
            };

            var identity = new ClaimsIdentity(claims, "LiderCookie");
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync("LiderCookie", principal, new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddHours(12)
            });

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            return RedirectToAction("Dashboard");
        }

        [HttpGet]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync("LiderCookie");
            return Redirect("/");
        }

        // ── Dashboard ─────────────────────────────────────────────────────────────

        [Authorize(AuthenticationSchemes = "LiderCookie")]
        [HttpGet]
        public async Task<IActionResult> Dashboard()
        {
            var celulaId = GetCelulaIdLogada();
            if (celulaId == null) return RedirectToAction("Login");

            var celula = await _db.Celulas.FindAsync(celulaId);
            if (celula == null) return RedirectToAction("Login");

            var integrantes = await _db.Integrantes
                .Where(i => i.CelulaId == celulaId && i.Ativo)
                .OrderBy(i => i.Nome)
                .ToListAsync();

            // Calcula percentual de presença por integrante (últimas 10 reuniões normais)
            var presencasIds = await _db.Presencas
                .Where(p => p.CelulaId == celulaId && p.Tipo == TipoPresenca.Normal)
                .OrderByDescending(p => p.Data)
                .Take(10)
                .Select(p => p.Id)
                .ToListAsync();

            var detalhes = await _db.PresencasDetalhes
                .Where(d => presencasIds.Contains(d.PresencaId))
                .ToListAsync();

            var integrantesVm = integrantes.Select(i =>
            {
                var meus = detalhes.Where(d => d.IntegranteId == i.Id).ToList();
                double? pct = meus.Count > 0
                    ? Math.Round((double)meus.Count(d => d.Presente) / meus.Count * 100, 1)
                    : null;
                return new IntegranteItemViewModel
                {
                    Id = i.Id,
                    Nome = i.Nome,
                    Visitante = i.Visitante,
                    DataIngresso = i.DataIngresso,
                    PercentualPresenca = pct
                };
            }).ToList();

            var ultimasPresencas = await _db.Presencas
                .Where(p => p.CelulaId == celulaId)
                .OrderByDescending(p => p.Data)
                .Take(5)
                .Select(p => new PresencaResumoViewModel
                {
                    Id = p.Id,
                    Data = p.Data,
                    Tipo = p.Tipo.ToString(),
                    TotalPresentes = p.Detalhes.Count(d => d.Presente),
                    TotalIntegrantes = p.Detalhes.Count
                })
                .ToListAsync();

            var vm = new LiderDashboardViewModel
            {
                CelulaId = celula.Id,
                NomeCelula = celula.Nome,
                NomeLider = celula.LiderNome ?? string.Empty,
                Integrantes = integrantesVm,
                UltimasPresencas = ultimasPresencas
            };

            return View(vm);
        }

        // ── Integrantes ───────────────────────────────────────────────────────────

        [Authorize(AuthenticationSchemes = "LiderCookie")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AdicionarIntegrante(LiderAdicionarIntegranteViewModel model)
        {
            var celulaId = GetCelulaIdLogada();
            if (celulaId == null) return RedirectToAction("Login");

            if (!ModelState.IsValid)
            {
                TempData["Erro"] = "Nome inválido.";
                return RedirectToAction("Dashboard");
            }

            var integrante = new Integrante
            {
                Nome = model.Nome.Trim(),
                CelulaId = celulaId.Value,
                Visitante = model.Visitante,
                DataIngresso = DateTime.UtcNow
            };

            _db.Integrantes.Add(integrante);
            await _db.SaveChangesAsync();

            TempData["Sucesso"] = $"Integrante \"{integrante.Nome}\" adicionado com sucesso!";
            return RedirectToAction("Dashboard");
        }

        [Authorize(AuthenticationSchemes = "LiderCookie")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoverIntegrante(int id)
        {
            var celulaId = GetCelulaIdLogada();
            if (celulaId == null) return RedirectToAction("Login");

            var integrante = await _db.Integrantes
                .FirstOrDefaultAsync(i => i.Id == id && i.CelulaId == celulaId);

            if (integrante == null)
            {
                TempData["Erro"] = "Integrante não encontrado.";
                return RedirectToAction("Dashboard");
            }

            // Soft delete para preservar histórico
            integrante.Ativo = false;
            await _db.SaveChangesAsync();

            TempData["Sucesso"] = $"Integrante \"{integrante.Nome}\" removido.";
            return RedirectToAction("Dashboard");
        }

        // ── Presença ──────────────────────────────────────────────────────────────

        [Authorize(AuthenticationSchemes = "LiderCookie")]
        [HttpGet]
        public async Task<IActionResult> Presenca()
        {
            var celulaId = GetCelulaIdLogada();
            if (celulaId == null) return RedirectToAction("Login");

            var integrantes = await _db.Integrantes
                .Where(i => i.CelulaId == celulaId && i.Ativo)
                .OrderBy(i => i.Nome)
                .ToListAsync();

            var vm = new LiderRegistrarPresencaViewModel
            {
                CelulaId = celulaId.Value,
                Data = DateTime.Today,
                Tipo = "Normal",
                Integrantes = integrantes.Select(i => new IntegrantePresencaItem
                {
                    IntegranteId = i.Id,
                    Nome = i.Nome,
                    Presente = false
                }).ToList()
            };

            return View(vm);
        }

        [Authorize(AuthenticationSchemes = "LiderCookie")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Presenca(LiderRegistrarPresencaViewModel model)
        {
            var celulaId = GetCelulaIdLogada();
            if (celulaId == null) return RedirectToAction("Login");

            // Garante que é a célula do líder logado
            model.CelulaId = celulaId.Value;

            if (!ModelState.IsValid)
                return View(model);

            if (!Enum.TryParse<TipoPresenca>(model.Tipo, out var tipo))
                tipo = TipoPresenca.Normal;

            var presenca = new Presenca
            {
                Data = model.Data.Date,
                CelulaId = celulaId.Value,
                Tipo = tipo,
                CriadoEm = DateTime.UtcNow
            };
            _db.Presencas.Add(presenca);
            await _db.SaveChangesAsync();

            if ((tipo == TipoPresenca.Normal || tipo == TipoPresenca.CelulaLivre) && model.Integrantes.Any())
            {
                // Valida que os integrantes pertencem à célula logada
                var idsValidos = await _db.Integrantes
                    .Where(i => i.CelulaId == celulaId && i.Ativo)
                    .Select(i => i.Id)
                    .ToListAsync();

                var detalhes = model.Integrantes
                    .Where(i => idsValidos.Contains(i.IntegranteId))
                    .Select(i => new PresencaDetalhe
                    {
                        PresencaId = presenca.Id,
                        IntegranteId = i.IntegranteId,
                        Presente = i.Presente,
                        Justificativa = string.IsNullOrWhiteSpace(i.Justificativa) ? null : i.Justificativa.Trim()
                    });

                _db.PresencasDetalhes.AddRange(detalhes);
                await _db.SaveChangesAsync();
            }

            TempData["Sucesso"] = "Presença registrada com sucesso!";
            return RedirectToAction("Dashboard");
        }

        // ── Histórico ─────────────────────────────────────────────────────────────

        [Authorize(AuthenticationSchemes = "LiderCookie")]
        [HttpGet]
        public async Task<IActionResult> Historico()
        {
            var celulaId = GetCelulaIdLogada();
            if (celulaId == null) return RedirectToAction("Login");

            var celula = await _db.Celulas.FindAsync(celulaId);
            if (celula == null) return RedirectToAction("Login");

            var presencas = await _db.Presencas
                .Where(p => p.CelulaId == celulaId)
                .OrderByDescending(p => p.Data)
                .Include(p => p.Detalhes)
                    .ThenInclude(d => d.Integrante)
                .Take(30)
                .ToListAsync();

            var vm = new LiderHistoricoViewModel
            {
                CelulaId = celulaId.Value,
                NomeCelula = celula.Nome,
                Presencas = presencas.Select(p => new PresencaDetalhadaViewModel
                {
                    Id = p.Id,
                    Data = p.Data,
                    Tipo = p.Tipo switch
                    {
                        TipoPresenca.Normal => "Normal",
                        TipoPresenca.NaoHoveCelula => "Não houve célula",
                        TipoPresenca.CelulaLivre => "Célula livre",
                        _ => p.Tipo.ToString()
                    },
                    Detalhes = p.Detalhes.Select(d => new DetalhePresencaItemViewModel
                    {
                        NomeIntegrante = d.Integrante.Nome,
                        Presente = d.Presente,
                        Justificativa = d.Justificativa
                    }).OrderBy(d => d.NomeIntegrante).ToList()
                }).ToList()
            };

            return View(vm);
        }
    }
}
