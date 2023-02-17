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
            _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} HomeController GET Incoming called, page={page}");
            //https://localhost:7226/Home/Index?page=3

            int count = await _repository.GetIncomingCount();
            _logger.LogDebug($"{DateTime.Now.ToString("HH:mm:ss:fffff")} HomeController Incoming table size={count}");

            IncomingWithPaginations incomingWithPaginations = new IncomingWithPaginations();

            incomingWithPaginations.PageViewModel = new PaginationPageViewModel(
                count, 
                page,
                _itemsAtPage);
            
            incomingWithPaginations.Incomings = await _repository.GetPageFromIncoming(_itemsAtPage, (page - 1) * _itemsAtPage);
            
            return View(incomingWithPaginations);
        }

        public IActionResult CreateIncoming()
        {
            _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} HomeController GET CreateIncoming called");

            var aa = StaticData.SecCodes;

            CreateIncomingModel model = new CreateIncomingModel();
            model.Date = DateTime.Today.AddDays( - 1);
            model.Category = 1;

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateIncoming(CreateIncomingModel newIncoming)
        {
            _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} HomeController POST CreateIncoming called");

            ViewData["Message"] = null;
            ViewData["Error"] = null;

            if (ModelState.IsValid)
            {
                _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} HomeController");



                ViewData["message_color"] = "green";
                ViewData["Message"] = "*Your Message Has Been Sent.";
            }
            else
            {
                _logger.LogInformation($"");

                ViewData["message_color"] = "red";
                ViewData["Message"] = "*Please complete the required fields";
            }

            ModelState.Clear();
            return View();
        }


        public IActionResult Privacy()
        {
            _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} HomeController GET Privacy called");
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} HomeController GET Error called");
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}