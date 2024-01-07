using DataAbstraction.Interfaces;
using DataAbstraction.Models;
using DataAbstraction.Models.BaseModels;
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

            //create seccodes list for new wishes - remove all already used seccodes
            List<StaticSecCode> secCodesList = new List<StaticSecCode>(StaticData.SecCodes
                .GetRange(1, StaticData.SecCodes.Count - 1));//1=SUR, skip it
            for (int i = secCodesList.Count - 1; i >= 0; i--)
            {
                if (result.Find(x => x.SecCode == secCodesList[i].SecCode) is not null)
                {
                    secCodesList.RemoveAt(i);
                }
            }
            ViewBag.WishList = secCodesList;

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
