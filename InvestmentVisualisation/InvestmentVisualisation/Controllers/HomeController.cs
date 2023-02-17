using DataAbstraction.Interfaces;
using DataAbstraction.Models;
using InvestmentVisualisation.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Diagnostics;

namespace InvestmentVisualisation.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private IDataBaseRepository _repository;
        private int _itemsAtPage;

        public HomeController(ILogger<HomeController> logger, IDataBaseRepository repository, IOptions<PaginationSettings> paginationSettings)
        {
            _logger = logger;
            _repository = repository;
            _itemsAtPage = paginationSettings.Value.PageItemsCount;
        }

        public async Task<IActionResult> Incoming(int page = 1)
        {
            //https://localhost:7226/Home/Index?page=3

            int count = await _repository.GetIncomingCount();

            IncomingWithPaginations incomingWithPaginations = new IncomingWithPaginations();

            incomingWithPaginations.PageViewModel = new PaginationPageViewModel(
                count, 
                page,
                _itemsAtPage);
            
            incomingWithPaginations.Incomings = await _repository.GetPageFromIncoming(_itemsAtPage, page - 1);
            
            return View(incomingWithPaginations);
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