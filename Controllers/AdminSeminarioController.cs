using BatistaFloramar.Domain.Entities;
using BatistaFloramar.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BatistaFloramar.Controllers
{
    [Authorize(AuthenticationSchemes = "AdminCookie")]
    public class AdminSeminarioController : Controller
    {
        private readonly BatistaFloramarDbContext _db;

        public AdminSeminarioController(BatistaFloramarDbContext db)
        {
            _db = db;
        }

        // ── Lista ──────────────────────────────────────────────────────────────
        public async Task<IActionResult> Index()
        {
            ViewBag.Title = "Seminário Teológico";
            ViewBag.AdminSection = "seminario";

            var materias = await _db.MateriasSeminario
                .OrderBy(m => m.Ano)
                .ThenBy(m => m.Semestre)
                .ThenBy(m => m.Ordem)
                .ToListAsync();

            return View(materias);
        }

        // ── Nova ───────────────────────────────────────────────────────────────
        [HttpGet]
        public IActionResult Nova()
        {
            ViewBag.Title = "Nova Matéria";
            ViewBag.AdminSection = "seminario";
            return View(new MateriaSeminario { Ativo = true, Ano = 1, Semestre = 1 });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Nova(MateriaSeminario model)
        {
            ViewBag.Title = "Nova Matéria";
            ViewBag.AdminSection = "seminario";

            Validar(model);
            if (!ModelState.IsValid) return View(model);

            model.CriadoEm = DateTime.UtcNow;
            _db.MateriasSeminario.Add(model);
            await _db.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Matéria \"{model.Nome}\" criada!";
            return RedirectToAction(nameof(Index));
        }

        // ── Editar ─────────────────────────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> Editar(int id)
        {
            ViewBag.Title = "Editar Matéria";
            ViewBag.AdminSection = "seminario";

            var materia = await _db.MateriasSeminario.FindAsync(id);
            if (materia == null) return NotFound();
            return View(materia);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Editar(int id, MateriaSeminario model)
        {
            ViewBag.Title = "Editar Matéria";
            ViewBag.AdminSection = "seminario";

            if (id != model.Id) return BadRequest();

            Validar(model);
            if (!ModelState.IsValid) return View(model);

            var materia = await _db.MateriasSeminario.FindAsync(id);
            if (materia == null) return NotFound();

            materia.Nome = model.Nome;
            materia.Descricao = model.Descricao;
            materia.Ano = model.Ano;
            materia.Semestre = model.Semestre;
            materia.Professor = model.Professor;
            materia.CargaHoraria = model.CargaHoraria;
            materia.Ordem = model.Ordem;
            materia.Ativo = model.Ativo;

            await _db.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Matéria \"{materia.Nome}\" atualizada!";
            return RedirectToAction(nameof(Index));
        }

        // ── Toggle Ativo ───────────────────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleAtivo(int id)
        {
            var materia = await _db.MateriasSeminario.FindAsync(id);
            if (materia == null) return NotFound();

            materia.Ativo = !materia.Ativo;
            await _db.SaveChangesAsync();

            TempData["SuccessMessage"] = $"\"{materia.Nome}\" {(materia.Ativo ? "ativada" : "desativada")}.";
            return RedirectToAction(nameof(Index));
        }

        // ── Excluir ────────────────────────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Excluir(int id)
        {
            var materia = await _db.MateriasSeminario.FindAsync(id);
            if (materia != null)
            {
                _db.MateriasSeminario.Remove(materia);
                await _db.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Matéria \"{materia.Nome}\" removida.";
            }
            return RedirectToAction(nameof(Index));
        }

        private void Validar(MateriaSeminario model)
        {
            if (string.IsNullOrWhiteSpace(model.Nome))
                ModelState.AddModelError("Nome", "Informe o nome da matéria.");
            if (model.Ano is not (1 or 2))
                ModelState.AddModelError("Ano", "O ano deve ser 1 ou 2.");
            if (model.Semestre is not (1 or 2))
                ModelState.AddModelError("Semestre", "O semestre deve ser 1 ou 2.");
            if (model.CargaHoraria < 0)
                ModelState.AddModelError("CargaHoraria", "Carga horária não pode ser negativa.");
        }
    }
}
