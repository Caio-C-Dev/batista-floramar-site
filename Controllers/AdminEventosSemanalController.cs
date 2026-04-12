using BatistaFloramar.Domain.Entities;
using BatistaFloramar.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BatistaFloramar.Controllers
{
    [Authorize(AuthenticationSchemes = "AdminCookie")]
    public class AdminEventosSemanalController : Controller
    {
        private readonly BatistaFloramarDbContext _db;

        private static readonly string[] DiasOrdem =
            ["Segunda", "Terça", "Quarta", "Quinta", "Sexta", "Sábado", "Domingo"];

        public AdminEventosSemanalController(BatistaFloramarDbContext db)
        {
            _db = db;
        }

        // ── Lista ──────────────────────────────────────────────────────────────
        public async Task<IActionResult> Index()
        {
            ViewBag.Title = "Agenda Semanal";
            ViewBag.AdminSection = "eventos-semanais";

            var todos = await _db.EventosSemanais
                .OrderBy(e => e.Ordem)
                .ToListAsync();

            // Agrupar na ordem dos dias da semana
            var agrupados = DiasOrdem
                .Select(d => new
                {
                    Dia = d,
                    Eventos = todos.Where(e => e.DiaSemana == d).OrderBy(e => e.Ordem).ToList()
                })
                .Where(g => g.Eventos.Count > 0)
                .ToList();

            ViewBag.Agrupados = agrupados;
            return View(todos);
        }

        // ── Novo ───────────────────────────────────────────────────────────────
        [HttpGet]
        public IActionResult Novo()
        {
            ViewBag.Title = "Novo Evento Semanal";
            ViewBag.AdminSection = "eventos-semanais";
            ViewBag.Dias = DiasOrdem;
            return View(new EventoSemanal { Ativo = true, Ordem = 0 });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Novo(EventoSemanal model)
        {
            ViewBag.Title = "Novo Evento Semanal";
            ViewBag.AdminSection = "eventos-semanais";
            ViewBag.Dias = DiasOrdem;

            if (string.IsNullOrWhiteSpace(model.Titulo))
                ModelState.AddModelError("Titulo", "Informe o título.");
            if (string.IsNullOrWhiteSpace(model.DiaSemana))
                ModelState.AddModelError("DiaSemana", "Selecione o dia da semana.");
            if (string.IsNullOrWhiteSpace(model.Horario))
                ModelState.AddModelError("Horario", "Informe o horário.");

            if (!ModelState.IsValid) return View(model);

            model.DataCriacao = DateTime.UtcNow;
            _db.EventosSemanais.Add(model);
            await _db.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Evento \"{model.Titulo}\" criado com sucesso!";
            return RedirectToAction(nameof(Index));
        }

        // ── Editar ─────────────────────────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> Editar(int id)
        {
            ViewBag.Title = "Editar Evento Semanal";
            ViewBag.AdminSection = "eventos-semanais";
            ViewBag.Dias = DiasOrdem;

            var ev = await _db.EventosSemanais.FindAsync(id);
            if (ev == null) return NotFound();
            return View(ev);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Editar(int id, EventoSemanal model)
        {
            ViewBag.Title = "Editar Evento Semanal";
            ViewBag.AdminSection = "eventos-semanais";
            ViewBag.Dias = DiasOrdem;

            if (id != model.Id) return BadRequest();

            if (string.IsNullOrWhiteSpace(model.Titulo))
                ModelState.AddModelError("Titulo", "Informe o título.");
            if (string.IsNullOrWhiteSpace(model.DiaSemana))
                ModelState.AddModelError("DiaSemana", "Selecione o dia da semana.");
            if (string.IsNullOrWhiteSpace(model.Horario))
                ModelState.AddModelError("Horario", "Informe o horário.");

            if (!ModelState.IsValid) return View(model);

            var ev = await _db.EventosSemanais.FindAsync(id);
            if (ev == null) return NotFound();

            ev.Titulo = model.Titulo;
            ev.DiaSemana = model.DiaSemana;
            ev.Horario = model.Horario;
            ev.Descricao = model.Descricao;
            ev.Ativo = model.Ativo;
            ev.Ordem = model.Ordem;

            await _db.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Evento \"{ev.Titulo}\" atualizado!";
            return RedirectToAction(nameof(Index));
        }

        // ── Toggle Ativo ───────────────────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleAtivo(int id)
        {
            var ev = await _db.EventosSemanais.FindAsync(id);
            if (ev == null) return NotFound();

            ev.Ativo = !ev.Ativo;
            await _db.SaveChangesAsync();

            TempData["SuccessMessage"] = $"\"{ev.Titulo}\" {(ev.Ativo ? "ativado" : "desativado")}.";
            return RedirectToAction(nameof(Index));
        }

        // ── Excluir ────────────────────────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Excluir(int id)
        {
            var ev = await _db.EventosSemanais.FindAsync(id);
            if (ev != null)
            {
                _db.EventosSemanais.Remove(ev);
                await _db.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Evento \"{ev.Titulo}\" removido.";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
