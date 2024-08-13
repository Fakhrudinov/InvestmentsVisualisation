using DataAbstraction.Interfaces;
using DataAbstraction.Models;
using DataAbstraction.Models.BaseModels;
using DataAbstraction.Models.WishList;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace InvestmentVisualisation.Controllers
{
    public class WishListController : Controller
    {
        private readonly ILogger<WishListController> _logger;
        private IMySqlWishListRepository _repository;
        private WishListSettings _wishListSettings;

        public WishListController(
            ILogger<WishListController> logger, 
            IMySqlWishListRepository repository,
            IOptions<WishListSettings> wishListSettings)
        {
            _logger = logger;
            _repository = repository;
            _wishListSettings=wishListSettings.Value;
        }

        public async Task<IActionResult> WishList(CancellationToken cancellationToken)
        {
            _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} WishListController WishList called");

            List<WishListItemModel> result = await _repository.GetFullWishList(cancellationToken);

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
            ViewBag.WishLevels = _wishListSettings.LevelsWeight;

            return View(result);
        }

        public async Task<IActionResult> Delete(CancellationToken cancellationToken, string seccode)
        {
            _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} WishListController Delete " +
                $"seccode={seccode} called");

            await _repository.DeleteWishBySecCode(cancellationToken, seccode);

            return RedirectToAction("WishList");
        }

        [HttpPost]
        public async Task<IActionResult> CreateNewWish(
            CancellationToken cancellationToken, 
            string seccode, 
            int level,
            string description)
        {
            _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} WishListController CreateNewWish " +
                $"seccode={seccode} level={level} called");

            await _repository.AddNewWish(cancellationToken, seccode, level, description);

            return RedirectToAction("WishList");
        }

        [HttpPost]
        public async Task<IActionResult> EditWishLevel(
            CancellationToken cancellationToken, 
            string seccode, 
            int level, 
            string description)
        {
            _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} WishListController EditWishLevel " +
                $"seccode={seccode} level={level} called");

            await _repository.EditWishLevel(cancellationToken, seccode, level, description);

            return RedirectToAction("WishList");
        }
    }
}
