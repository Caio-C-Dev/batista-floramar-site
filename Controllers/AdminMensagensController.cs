using BatistaFloramar.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BatistaFloramar.Controllers
{
    [Authorize(AuthenticationSchemes = "AdminCookie")]
    public class AdminMensagensController : Controller
    {
        private readonly BatistaFloramarDbContext _db;
        public AdminMensagensController(BatistaFloramarDbContext db) => _db = db;

        public async Task<IActionResult> Index()
        {
            ViewBag.AdminSection = "mensagens";
            ViewBag.Title = "Mensagens Recebidas";
            var mensagens = await _db.PerguntasPastor.OrderByDescending(m => m.DataEnvio).ToListAsync();
            return View(mensagens);
        }

        public async Task<IActionResult> Detalhes(int id)
        {
            ViewBag.AdminSection = "mensagens";
            ViewBag.Title = "Detalhe da Mensagem";
            var mensagem = await _db.PerguntasPastor.FindAsync(id);
            if (mensagem == null) return NotFound();
            return View(mensagem);
        }
    }
}
