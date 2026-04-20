using Microsoft.AspNetCore.Mvc;

namespace BatistaFloramar.Controllers
{
    public class ErroController : Controller
    {
        [Route("erro/{statusCode}")]
        public IActionResult Index(int statusCode)
        {
            Response.StatusCode = statusCode;

            if (statusCode == 404)
            {
                return View("~/Views/Shared/NotFound.cshtml");
            }

            // Para outros erros, retorna uma mensagem genérica
            ViewBag.Title = "Erro " + statusCode;
            ViewBag.StatusCode = statusCode;
            return View("~/Views/Shared/NotFound.cshtml");
        }
    }
}
