using Microsoft.AspNetCore.Mvc;

namespace BatistaFloramar.Controllers
{
    public class VisitaController : Controller
    {
        public IActionResult Index()
        {
            ViewBag.Title = "Planeje sua Visita | Igreja Batista Floramar BH";
            ViewBag.MetaDescription = "Planeje sua visita à Comunidade Batista Floramar em Floramar, Belo Horizonte. Saiba o que esperar, horários de culto, como chegar e muito mais. Igreja batista Bíblica em BH.";
            return View();
        }
    }
}
