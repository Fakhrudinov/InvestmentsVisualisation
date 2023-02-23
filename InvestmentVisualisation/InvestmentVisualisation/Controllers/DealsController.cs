using DataAbstraction.Models;
using Microsoft.AspNetCore.Mvc;
using DataAbstraction.Interfaces;
using Microsoft.Extensions.Options;
using DataAbstraction.Models.Settings;
using DataAbstraction.Models.Deals;

namespace InvestmentVisualisation.Controllers
{
    public class DealsController : Controller
    {
        private readonly ILogger<DealsController> _logger;
        private IMySqlDealsRepository _repository;
        private int _itemsAtPage;

        public DealsController(ILogger<DealsController> logger, IMySqlDealsRepository repository, IOptions<PaginationSettings> paginationSettings)
        {
            _logger = logger;
            _repository = repository;
            _itemsAtPage = paginationSettings.Value.PageItemsCount;
        }


        public async Task<IActionResult> Deals(int page = 1)
        {
            _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} DealsController GET Deals called, page={page}");

            int count = await _repository.GetDealsCount();
            _logger.LogDebug($"{DateTime.Now.ToString("HH:mm:ss:fffff")} DealsController Deals table size={count}");

            DealsWithPaginations dealsWithPaginations = new DealsWithPaginations();

            dealsWithPaginations.PageViewModel = new PaginationPageViewModel(
            count,
            page,
            _itemsAtPage);

            dealsWithPaginations.Deals = await _repository.GetPageFromDeals(_itemsAtPage, (page - 1) * _itemsAtPage);

            return View(dealsWithPaginations);
        }

        public ActionResult Create()
        {
            _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} DealsController GET Create called");

            CreateDealsModel model = new CreateDealsModel();
            model.Date = DateTime.Now.AddDays(-1);// обычно вношу за вчера

            return View(model);
        }
        public ActionResult CreateSpecific(DateTime data, string tiker, string price, int pieces)
        {
            _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} DealsController GET CreateSpecific called " +
                $"with parametres {data} {tiker} price={price} pieces={pieces}");

            CreateDealsModel model = new CreateDealsModel();
            model.Date = data;
            model.SecCode = tiker;
            model.AvPrice = price;
            model.Pieces = pieces;
            return View("Create", model);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateDealsModel model)
        {
            _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} DealsController POST Create called");

            model.SecBoard = StaticData.SecCodes[StaticData.SecCodes.FindIndex(x => x.SecCode == model.SecCode)].SecBoard;

            if (model.SecCode.Equals("0"))
            {
                _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} DealsController Create error " +
                    $"- Сделка по secCode Деньги не возможна!");
                ViewData["Message"] = $"Сделка по тикеру Деньги не возможна!";
                return View();
            }
            if (model.SecBoard == 1 && model.NKD is not null)
            {
                _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} DealsController Create error " +
                    $"- NKD на акции недопустим");
                ViewData["Message"] = $"NKD на акции недопустим";
                return View();
            }

            model.AvPrice = model.AvPrice.Replace(',', '.');
            if (model.Comission is not null)
            {
                model.Comission = model.Comission.Replace(',', '.');
            }
            if (model.NKD is not null)
            {
                model.NKD = model.NKD.Replace(',', '.');
            }

            _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} DealsController POST Create validation complete, " +
                $"try create at repository");

            string result = await _repository.CreateNewDeal(model);
            if (!result.Equals("1"))
            {
                ViewData["Message"] = $"Добавление не удалось. \r\n{result}";
                return View();
            }

            return RedirectToAction("Deals");
        }

        public async Task<IActionResult> Edit(int id)
        {
            _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} DealsController GET Edit id={id} called");

            DealModel editedItem = await _repository.GetSingleDealById(id);

            return View(editedItem);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(DealModel model)
        {
            _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} DealsController HttpPost Edit called");

            model.AvPrice = model.AvPrice.Replace(',', '.');
            if (model.Comission is not null)
            {
                model.Comission = model.Comission.Replace(',', '.');
            }
            if (model.NKD is not null)
            {
                model.NKD = model.NKD.Replace(',', '.');
            }

            string result = await _repository.EditSingleDeal(model);
            if (!result.Equals("1"))
            {
                ViewData["Message"] = $"Редактирование не удалось.\r\n{result}";
                return View(model);
            }

            return RedirectToAction("Deals");
        }

        public async Task<IActionResult> Delete(int id)
        {
            _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} DealsController GET Delete id={id} called");

            DealModel deleteItem = await _repository.GetSingleDealById(id);

            ViewData["SecBoard"] = @StaticData.SecBoards[StaticData.SecBoards.FindIndex(secb => secb.Id == deleteItem.SecBoard)].Name;

            return View(deleteItem);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(DealModel model)
        {
            _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} DealsController HttpPost Delete called");

            string result = await _repository.DeleteSingleDeal(model.Id);
            if (!result.Equals("1"))
            {
                ViewData["Message"] = $"Удаление не удалось.\r\n{result}";
                return View(model);
            }

            return RedirectToAction("Deals");
        }
    }
}
