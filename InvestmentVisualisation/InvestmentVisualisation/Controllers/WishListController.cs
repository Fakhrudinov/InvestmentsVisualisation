using DataAbstraction.Interfaces;
using DataAbstraction.Models;
using DataAbstraction.Models.BaseModels;
using DataAbstraction.Models.WishList;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InvestmentVisualisation.Controllers
{
    public class WishListController : Controller
    {
        private readonly ILogger<WishListController> _logger;
        private IMySqlWishListRepository _repository;
        private WishListSettings ? _wishListSettings;

        public WishListController(
            ILogger<WishListController> logger, 
            IMySqlWishListRepository repository
            )
        {
            _logger = logger;
            _repository = repository;
        }

		[Authorize]
		public async Task<IActionResult> WishList(
            CancellationToken cancellationToken,
            string sortMode = "bySecCode")
        {
            _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} WishListController WishList called" +
                $"sortMode={sortMode}");

            IConfigurationRoot configuration = new ConfigurationBuilder()
                .AddJsonFile("wishsettings.json")
                .Build();
            _wishListSettings = configuration.GetSection("WishListSettings").Get<WishListSettings>();

            string sqlFileSelector = "GetFullWishList.sql";
            if (sortMode.Equals("byLevel"))
            {
                sqlFileSelector = "GetFullWishListOrderByLevel.sql";
            }

            List<WishListItemModel> result = await _repository.GetFullWishList(
                cancellationToken,
                sqlFileSelector);

            ViewBag.WishList = GetSecCodesListWithoutExistingInWish(result);
            if (_wishListSettings is not null && _wishListSettings.LevelsWeight is not null)
            {
                ViewBag.WishLevels = _wishListSettings.LevelsWeight;
            }            
            ViewBag.SortMode = sortMode;
            return View(result);
        }
        private List<StaticSecCode> GetSecCodesListWithoutExistingInWish(List<WishListItemModel> result)
        {

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

            return secCodesList;
        }

		[Authorize(Roles = "Admin")]
		public async Task<IActionResult> Delete(
            CancellationToken cancellationToken, 
            string seccode, 
            string sortMode = "bySecCode")
        {
            _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} WishListController Delete " +
                $"seccode={seccode} called");

            await _repository.DeleteWishBySecCode(cancellationToken, seccode);

            return RedirectToAction("WishList", new { sortMode = sortMode });
        }

		[Authorize(Roles = "Admin")]
		[HttpPost]
        public async Task<IActionResult> CreateNewWish(
            CancellationToken cancellationToken, 
            string seccode, 
            int level,
            string description,
            string sortMode = "bySecCode")
        {
            _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} WishListController CreateNewWish " +
                $"seccode={seccode} level={level} called");

            await _repository.AddNewWish(cancellationToken, seccode, level, description);

            return RedirectToAction("WishList", new { sortMode = sortMode });
        }

		[Authorize(Roles = "Admin")]
		[HttpPost]
        public async Task<IActionResult> EditWishLevel(
            CancellationToken cancellationToken, 
            string seccode, 
            int level, 
            string description, 
            string sortMode = "bySecCode")
        {
            _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} WishListController EditWishLevel " +
                $"seccode={seccode} level={level} called");

            await _repository.EditWishLevel(cancellationToken, seccode, level, description);

            return RedirectToAction("WishList", new { sortMode = sortMode });
        }
    }
}
