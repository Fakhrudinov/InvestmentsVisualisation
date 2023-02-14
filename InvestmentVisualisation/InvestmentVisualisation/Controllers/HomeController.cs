using DataAbstraction.Interfaces;
using InvestmentVisualisation.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace InvestmentVisualisation.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private IDataBaseRepository _repository;

        public HomeController(ILogger<HomeController> logger, IDataBaseRepository repository)
        {
            _logger = logger;
            _repository = repository;
        }

        public async Task<IActionResult> IndexAsync()
        {
            await _repository.GetTest();
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}