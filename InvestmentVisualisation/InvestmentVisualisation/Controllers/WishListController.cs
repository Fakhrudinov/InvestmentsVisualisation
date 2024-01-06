using DataAbstraction.Interfaces;
using DataAbstraction.Models.WishList;
using Microsoft.AspNetCore.Mvc;

namespace InvestmentVisualisation.Controllers
{
    public class WishListController : Controller
    {
        private readonly ILogger<WishListController> _logger;
        private IMySqlWishListRepository _repository;

        public WishListController(ILogger<WishListController> logger, IMySqlWishListRepository repository)
        {
            _logger = logger;
            _repository = repository;
        }

        public async Task<IActionResult> WishList()
        {
            _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} WishListController WishList called");

            List<WishListItemModel> result = await _repository.GetFullWishList();

            return View(result);
        }

        public async Task<IActionResult> Delete(string seccode)
        {
            _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} WishListController Delete " +
                $"seccode={seccode} called");

            await _repository.DeleteWishBySecCode(seccode);

            return RedirectToAction("WishList");
        }

        [HttpPost]
        public async Task<IActionResult> CreateNewWish(string seccode, int level)
        {
            _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} WishListController CreateNewWish " +
                $"seccode={seccode} level={level} called");

            await _repository.AddNewWish(seccode, level);

            return RedirectToAction("WishList");
        }

        [HttpPost]
        public async Task<IActionResult> EditWishLevel(string seccode, int level)
        {
            _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} WishListController EditWishLevel " +
                $"seccode={seccode} level={level} called");

            await _repository.EditWishLevel(seccode, level);

            return RedirectToAction("WishList");
        }
    }
}
