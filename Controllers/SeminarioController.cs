using BatistaFloramar.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BatistaFloramar.Controllers
{
    public class SeminarioController : Controller
    {
        private readonly BatistaFloramarDbContext _db;
        public SeminarioController(BatistaFloramarDbContext db) => _db = db;

        public async Task<IActionResult> Index()
        {
            ViewBag.Title = "Seminário Teológico em BH | Curso de Teologia | Batista Floramar";
            ViewBag.MetaDescription = "Seminário Teológico da Comunidade Batista Floramar: curso de teologia de 2 anos com certificado, toda segunda-feira às 19h30, presencial em Belo Horizonte ou online com aulas gravadas. Nova turma todo início de ano.";
            ViewBag.MetaKeywords = "seminário teológico belo horizonte, curso de teologia BH, curso teologia online gravado, escola bíblica belo horizonte, seminário batista BH, curso teológico com certificado";

            var materias = await _db.MateriasSeminario
                .Where(m => m.Ativo)
                .OrderBy(m => m.Ano)
                .ThenBy(m => m.Semestre)
                .ThenBy(m => m.Ordem)
                .ToListAsync();

            return View(materias);
        }
    }
}
