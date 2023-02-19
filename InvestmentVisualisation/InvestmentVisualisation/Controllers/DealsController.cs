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

        // GET: DealsController
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


        // GET: DealsController/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: DealsController/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(IFormCollection collection)
        {
            try
            {
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }

        // GET: DealsController/Edit/5
        public ActionResult Edit(int id)
        {
            return View();
        }

        // POST: DealsController/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(int id, IFormCollection collection)
        {
            try
            {
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }

        // GET: DealsController/Delete/5
        public ActionResult Delete(int id)
        {
            return View();
        }

        // POST: DealsController/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int id, IFormCollection collection)
        {
            try
            {
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }
    }
}
