using DataAbstraction.Interfaces;
using DataAbstraction.Models;
using DataAbstraction.Models.Settings;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using DataAbstraction.Models.SecCodes;

namespace InvestmentVisualisation.Controllers
{
    public class SecCodesController : Controller
    {
        private readonly ILogger<SecCodesController> _logger;
        private IMySqlSecCodesRepository _repository;
        private int _itemsAtPage;

        public SecCodesController(ILogger<SecCodesController> logger, IMySqlSecCodesRepository repository, IOptions<PaginationSettings> paginationSettings)
        {
            _logger = logger;
            _repository = repository;
            _itemsAtPage = paginationSettings.Value.PageItemsCount;
        }

        public async Task<IActionResult> Index(int page = 1)
        {
            _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} SecCodesController GET Index called, page={page}");

            int count = await _repository.GetSecCodesCount();
            _logger.LogDebug($"{DateTime.Now.ToString("HH:mm:ss:fffff")} SecCodesController SecCodes table size={count}");

            SecCodesWithPaginations dealsWithPaginations = new SecCodesWithPaginations();

            dealsWithPaginations.PageViewModel = new PaginationPageViewModel(
                count,
                page,
                _itemsAtPage);

            dealsWithPaginations.SecCodes = await _repository.GetPageFromSecCodes(_itemsAtPage, (page - 1) * _itemsAtPage);

            return View(dealsWithPaginations);
        }

        public ActionResult Create()
        {
            _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} SecCodesController GET Create called");

            SecCodeInfo model = new SecCodeInfo();
            return View(model);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(SecCodeInfo model)
        {
            _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} SecCodesController POST Create called");

            string result = await _repository.CreateNewSecCode(model);
            if (!result.Equals("1"))
            {
                ViewData["Message"] = $"Добавление не удалось. \r\n{result}";
                return View();
            }

            return RedirectToAction("Index");
        }

        public async Task<IActionResult> Edit(string secCode)
        {
            _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} SecCodesController GET Edit secCode={secCode} called");

            SecCodeInfo editedItem = await _repository.GetSingleSecCodeBySecCode(secCode);

            return View(editedItem);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(SecCodeInfo model)
        {
            _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} SecCodesController HttpPost Edit called");

            string result = await _repository.EditSingleSecCode(model);
            if (!result.Equals("1"))
            {
                ViewData["Message"] = $"Редактирование не удалось.\r\n{result}";
                return View(model);
            }

            return RedirectToAction("Index");
        }
    }
}
