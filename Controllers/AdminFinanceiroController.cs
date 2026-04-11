using BatistaFloramar.Application.Services;
using BatistaFloramar.Domain.Entities;
using BatistaFloramar.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BatistaFloramar.Controllers
{
    [Authorize(AuthenticationSchemes = "AdminCookie")]
    public class AdminFinanceiroController : Controller
    {
        private readonly FinanceiroService _fin;
        private readonly BatistaFloramarDbContext _db;

        public AdminFinanceiroController(FinanceiroService fin, BatistaFloramarDbContext db)
        {
            _fin = fin;
            _db = db;
        }

        // ─── Dashboard ───────────────────────────────────────────────────────────

        public async Task<IActionResult> Dashboard(int? mes, int? ano)
        {
            ViewBag.AdminSection = "financeiro";
            ViewBag.Title = "Gestão Financeira";

            var hoje = DateTime.Today;
            var m = mes ?? hoje.Month;
            var a = ano ?? hoje.Year;

            var dto = await _fin.GetDashboardAsync(m, a);

            ViewBag.Anos = Enumerable.Range(hoje.Year - 3, 5).Reverse().ToList();
            return View(dto);
        }

        // ─── Entradas ────────────────────────────────────────────────────────────

        public async Task<IActionResult> Entradas(int? mes, int? ano, string? tipo, int? ministerioId)
        {
            ViewBag.AdminSection = "financeiro";
            ViewBag.Title = "Entradas Financeiras";

            TipoEntrada? tipoEnum = Enum.TryParse<TipoEntrada>(tipo, out var t) ? t : null;
            var (itens, total) = await _fin.ListarEntradasAsync(mes, ano, tipoEnum, ministerioId);

            ViewBag.Total = total;
            ViewBag.MesSel = mes;
            ViewBag.AnoSel = ano ?? DateTime.Today.Year;
            ViewBag.TipoSel = tipo;
            ViewBag.MinisterioSel = ministerioId;
            ViewBag.Ministerios = await _db.Ministerios.Where(m2 => m2.Ativo).OrderBy(m2 => m2.Nome).ToListAsync();
            ViewBag.Anos = Enumerable.Range(DateTime.Today.Year - 3, 5).Reverse().ToList();
            return View(itens);
        }

        [HttpGet]
        public async Task<IActionResult> NovaEntrada()
        {
            ViewBag.AdminSection = "financeiro";
            ViewBag.Title = "Nova Entrada";
            ViewBag.Ministerios = await _db.Ministerios.Where(m => m.Ativo).OrderBy(m => m.Nome).ToListAsync();
            return View(new EntradaFinanceira { Data = DateTime.Today });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> NovaEntrada(EntradaFinanceira model)
        {
            if (model.Valor <= 0)
                ModelState.AddModelError(nameof(model.Valor), "O valor deve ser maior que zero.");

            if (!ModelState.IsValid)
            {
                ViewBag.AdminSection = "financeiro";
                ViewBag.Title = "Nova Entrada";
                ViewBag.Ministerios = await _db.Ministerios.Where(m => m.Ativo).OrderBy(m => m.Nome).ToListAsync();
                return View(model);
            }

            model.RegistradoPor = User.Identity?.Name ?? "admin";
            model.CriadoEm = DateTime.UtcNow;

            await _fin.RegistrarEntradaAsync(model);
            TempData["SuccessMessage"] = "Entrada registrada com sucesso!";
            return RedirectToAction(nameof(Entradas));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ExcluirEntrada(int id)
        {
            await _fin.ExcluirEntradaAsync(id);
            TempData["SuccessMessage"] = "Entrada excluída.";
            return RedirectToAction(nameof(Entradas));
        }

        // ─── Saídas ──────────────────────────────────────────────────────────────

        public async Task<IActionResult> Saidas(int? mes, int? ano, string? tipo)
        {
            ViewBag.AdminSection = "financeiro";
            ViewBag.Title = "Saídas Financeiras";

            TipoSaida? tipoEnum = Enum.TryParse<TipoSaida>(tipo, out var t) ? t : null;
            var (itens, total) = await _fin.ListarSaidasAsync(mes, ano, tipoEnum);

            ViewBag.Total = total;
            ViewBag.MesSel = mes;
            ViewBag.AnoSel = ano ?? DateTime.Today.Year;
            ViewBag.TipoSel = tipo;
            ViewBag.Anos = Enumerable.Range(DateTime.Today.Year - 3, 5).Reverse().ToList();
            return View(itens);
        }

        [HttpGet]
        public IActionResult NovaSaida()
        {
            ViewBag.AdminSection = "financeiro";
            ViewBag.Title = "Nova Saída";
            return View(new SaidaFinanceira { Data = DateTime.Today });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> NovaSaida(SaidaFinanceira model)
        {
            if (model.Valor <= 0)
                ModelState.AddModelError(nameof(model.Valor), "O valor deve ser maior que zero.");

            if (!ModelState.IsValid)
            {
                ViewBag.AdminSection = "financeiro";
                ViewBag.Title = "Nova Saída";
                return View(model);
            }

            model.RegistradoPor = User.Identity?.Name ?? "admin";
            model.CriadoEm = DateTime.UtcNow;

            await _fin.RegistrarSaidaAsync(model);
            TempData["SuccessMessage"] = "Saída registrada com sucesso!";
            return RedirectToAction(nameof(Saidas));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ExcluirSaida(int id)
        {
            await _fin.ExcluirSaidaAsync(id);
            TempData["SuccessMessage"] = "Saída excluída.";
            return RedirectToAction(nameof(Saidas));
        }

        // ─── Relatório ───────────────────────────────────────────────────────────

        public async Task<IActionResult> Relatorio(int? mes, int? ano)
        {
            ViewBag.AdminSection = "financeiro";
            ViewBag.Title = "Relatório Mensal";

            var hoje = DateTime.Today;
            var m = mes ?? hoje.Month;
            var a = ano ?? hoje.Year;

            var dto = await _fin.GetDashboardAsync(m, a);
            var (entradas, _) = await _fin.ListarEntradasAsync(m, a, null, null);
            var (saidas, _) = await _fin.ListarSaidasAsync(m, a, null);

            ViewBag.Entradas = entradas;
            ViewBag.Saidas = saidas;
            ViewBag.Anos = Enumerable.Range(hoje.Year - 3, 5).Reverse().ToList();
            return View(dto);
        }
    }
}
