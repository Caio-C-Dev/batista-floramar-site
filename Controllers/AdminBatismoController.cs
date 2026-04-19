using BatistaFloramar.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BatistaFloramar.Controllers
{
    [Authorize(AuthenticationSchemes = "AdminCookie")]
    public class AdminBatismoController : Controller
    {
        private readonly BatistaFloramarDbContext _db;

        public AdminBatismoController(BatistaFloramarDbContext db)
        {
            _db = db;
        }

        public async Task<IActionResult> Index()
        {
            ViewBag.AdminSection = "batismo";
            ViewBag.Title = "Solicitações de Batismo e Filiação";
            var solicitacoes = await _db.SolicitacoesBatismo
                .OrderByDescending(s => s.DataEnvio)
                .ToListAsync();
            return View(solicitacoes);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarcarAtendido(int id)
        {
            var s = await _db.SolicitacoesBatismo.FindAsync(id);
            if (s != null)
            {
                s.Atendido = true;
                await _db.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Solicitação de {s.Nome} marcada como atendida.";
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Excluir(int id)
        {
            var s = await _db.SolicitacoesBatismo.FindAsync(id);
            if (s != null)
            {
                _db.SolicitacoesBatismo.Remove(s);
                await _db.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Solicitação de {s.Nome} excluída.";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
