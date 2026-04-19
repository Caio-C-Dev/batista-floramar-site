using BatistaFloramar.Domain.Entities;
using BatistaFloramar.Infrastructure.Data;
using BatistaFloramar.Models;
using Microsoft.AspNetCore.Mvc;

namespace BatistaFloramar.Controllers
{
    public class ContatoController : Controller
    {
        private readonly BatistaFloramarDbContext _db;

        public ContatoController(BatistaFloramarDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        public IActionResult Batismo()
        {
            ViewBag.Title = "Batismo e Filiação | Igreja Batista Floramar BH";
            ViewBag.MetaDescription = "Interesse em batismo ou filiação na Comunidade Batista Floramar, em Floramar, Belo Horizonte. Preencha o formulário e nossa equipe irá entrar em contato com você.";
            return View(new BatismoViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Batismo(BatismoViewModel model)
        {
            ViewBag.Title = "Batismo e Filiação | Igreja Batista Floramar BH";
            ViewBag.MetaDescription = "Interesse em batismo ou filiação na Comunidade Batista Floramar em Belo Horizonte.";

            if (!ModelState.IsValid)
                return View(model);

            _db.SolicitacoesBatismo.Add(new SolicitacaoBatismo
            {
                Nome = model.Nome.Trim(),
                WhatsApp = model.WhatsApp.Trim(),
                Email = model.Email.Trim(),
                Tipo = model.Tipo,
                Mensagem = model.Mensagem?.Trim(),
                DataEnvio = DateTime.UtcNow,
                Atendido = false
            });
            await _db.SaveChangesAsync();

            TempData["SuccessMessage"] = "Recebemos sua solicitação! Nossa equipe entrará em contato em breve. Que Deus abençoe você!";
            return RedirectToAction(nameof(Batismo));
        }
    }
}
