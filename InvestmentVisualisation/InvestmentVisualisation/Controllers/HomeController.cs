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
            //https://localhost:7226/Home/Incoming?page=3
            
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

            CreateIncomingModel model = new CreateIncomingModel();
            model.Date = DateTime.Today.AddDays( -1 );// обычно вношу за вчера
            model.Category = 1;

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateIncoming(CreateIncomingModel newIncoming)
        {
            _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} HomeController POST CreateIncoming called");

            if (!newIncoming.SecCode.Equals("0") && newIncoming.Category == 0)
            {
                _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} HomeController CreateIncoming error " +
                    $"- 'Зачисление денег' невозможно для secCode {newIncoming.SecCode}");

                ViewData["Message"] = $"'Зачисление денег' невозможно для {newIncoming.SecCode}. Используй Дивиденты или досрочное погашение";

                return View();
            }

            if (!newIncoming.SecCode.Equals("0") && newIncoming.Category == 3)
            {
                _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} HomeController CreateIncoming error " +
                    $"- 'Комиссия брокера' невозможна для secCode {newIncoming.SecCode}");

                ViewData["Message"] = $"'Комиссия брокера' невозможна для {newIncoming.SecCode}";

                return View();
            }

            if (newIncoming.SecCode.Equals("0") && newIncoming.Category == 1)
            {
                _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} HomeController CreateIncoming error " +
                    $"- 'Дивиденды' недопустимы для денег");

                ViewData["Message"] = $"'Дивиденды' недопустимы для денег. Для начисления денег от оброкера на ваши деньги - используйте досрочное погашение.";

                return View();
            }

            newIncoming.SecBoard = StaticData.SecCodes[StaticData.SecCodes.FindIndex(x => x.SecCode == newIncoming.SecCode)].SecBoard;

            newIncoming.Value = newIncoming.Value.Replace(',', '.');
            if (newIncoming.Comission is not null)
            {
                newIncoming.Comission = newIncoming.Comission.Replace(',', '.');
            }

            _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} HomeController POST CreateIncoming validation complete, " +
                $"try create at repository");

            string result = await _repository.CreateNewIncoming(newIncoming);
            if (!result.Equals("1"))
            {
                ViewData["Message"] = $"Добавление не удалось. \r\n{result}";
                return View();
            }

            return RedirectToAction("Incoming");
        }

        public async Task<IActionResult> Edit(int id)
        {
            _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} HomeController GET Edit id={id} called");

            IncomingModel editedItem = await _repository.GetSingleIncomingById(id);

            return View(editedItem);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(IncomingModel newIncoming)
        {
            _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} HomeController HttpPost Edit called");

            newIncoming.Value = newIncoming.Value.Replace(',', '.');
            if (newIncoming.Comission is not null)
            {
                newIncoming.Comission = newIncoming.Comission.Replace(',', '.');
            }

            string result = await _repository.EditSingleIncoming(newIncoming);
            if (!result.Equals("1"))
            {
                ViewData["Message"] = $"Редактирование не удалось.\r\n{result}";
                return View(newIncoming);
            }

            return RedirectToAction("Incoming");
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} HomeController GET Error called");
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}