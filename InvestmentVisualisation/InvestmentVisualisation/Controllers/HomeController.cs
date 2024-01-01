using Microsoft.AspNetCore.Mvc;

namespace InvestmentVisualisation.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
