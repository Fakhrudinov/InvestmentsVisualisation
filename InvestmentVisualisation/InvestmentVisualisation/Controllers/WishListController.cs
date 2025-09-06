using DataAbstraction.Interfaces;
using DataAbstraction.Models;
using DataAbstraction.Models.BaseModels;
using DataAbstraction.Models.SecVolume;
using DataAbstraction.Models.WishList;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace InvestmentVisualisation.Controllers
{
    public class WishListController : Controller
    {
        private readonly ILogger<WishListController> _logger;
        private IMySqlWishListRepository _repository;

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

			int[]? wishListSettings = await _repository.GetWishLevelsWeight(cancellationToken);


			string sqlFileSelector = "GetFullWishList.sql";
            if (sortMode.Equals("byLevel"))
            {
                sqlFileSelector = "GetFullWishListOrderByLevel.sql";
            }

            List<WishListItemModel> result = await _repository.GetFullWishList(
                cancellationToken,
                sqlFileSelector);

            ViewBag.WishList = GetSecCodesListWithoutExistingInWish(result);
            if (wishListSettings is not null)
            {
                ViewBag.WishLevels = wishListSettings;
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


		[Authorize(Roles = "Admin")]
		[HttpGet]
		public async Task<IActionResult> EditWishLevelsWeight(CancellationToken cancellationToken)
		{
			_logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} WishListController EditWishLevelsWeight " +
				$"called");

			int[]? wishWeight = await _repository.GetWishLevelsWeight(cancellationToken);

			return View(wishWeight);
		}

		[Authorize(Roles = "Admin")]
		[HttpPost]
		public async Task<IActionResult> EditWishLevelWeight(int level, int weight, CancellationToken cancellationToken)
		{
			_logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} WishListController EditWishLevelWeight " +
				$"called with level={level} weight={weight}");

            // do action
            string ? result = await _repository.EditWishLevelWeight(level, weight, cancellationToken);
            if (result is not null)
            {
				ViewData["Message"] = result;
			}

			int[]? wishWeight = await _repository.GetWishLevelsWeight(cancellationToken);
			return View("EditWishLevelsWeight", wishWeight);
		}

		[AllowAnonymous]
		public async Task<IActionResult> VolumeChart(CancellationToken cancellationToken)
		{
			_logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} WishListController " +
				$"GET VolumeChart called");

			// get data
			List<WishListVolumeChartData>? instruments = await _repository.GetVolumeChartData(
				cancellationToken,
				DateTime.Now.Year);
			if (instruments is null || instruments.Count == 0)
			{
				ViewBag.Error = ViewBag.Error + "Не получены данные из базы данных";
				return View();
			}


			/// Remove ****P at separate list
			/// search **** similar to ****P
			///     if exist = add P to similar
			///     else add P
			/// Sort list

			// Remove ****P at separate list
			List<WishListVolumeChartData> dataPointsPref = new List<WishListVolumeChartData>();
			for (int i = instruments.Count - 1; i >= 0; i--)
			{
				if (instruments[i].SecCode.Length == 5 && instruments[i].SecCode.EndsWith("P"))
				{
					dataPointsPref.Add(instruments[i]);
					instruments.RemoveAt(i);
				}
			}
			// search **** similar to ****P
			foreach (WishListVolumeChartData pref in dataPointsPref)
			{
				bool isExist = false;

				foreach (WishListVolumeChartData ao in instruments)
				{
					//if exist = add P to similar
					if (ao.SecCode.Length == 4 && pref.SecCode.Substring(0, 4).Equals(ao.SecCode))
					{
						ao.SecCode = ao.SecCode + "+" + pref.SecCode;
						ao.RealVolume = ao.RealVolume + pref.RealVolume;

						isExist = true;
						break;
					}
				}

				// else add P
				if (!isExist)
				{
					instruments.Add(pref);
				}
			}



			List<PieChartItemModel> dataPointsReal = new List<PieChartItemModel>();
			List<PieChartItemModel> dataPointsWish = new List<PieChartItemModel>();
			dataPointsReal.Add(new PieChartItemModel("Others", 0));
			dataPointsWish.Add(new PieChartItemModel("Others", 0));


			decimal totalRealVolume = 0;
			decimal totalWishVolume = 0;
			decimal realValueNotNull = 0;
			decimal wishValueNotNull = 0;
			foreach (WishListVolumeChartData instr in instruments)
            {
                /// levels = only above 0. all other in 'Others' level.
                /// check - is this level exist at dataPointsSorted
                ///     if not - create new level
                ///     
                /// add volume to level
                ///     real and wish
                /// 
                /// add volume to totalRealVolume
                /// add volume to totalWishVolume
                /// 
                /// add instr to description
                /// 

                // check - is this level exist at dataPointsSorted
                //     if not - create new level
                string nameOfDataPoint = "Level ";
                if (instr.Level > 0 && !dataPointsReal.Any(dp => dp.name.Equals(nameOfDataPoint + instr.Level)))
                {
                    dataPointsReal.Add(new PieChartItemModel(nameOfDataPoint + instr.Level, 0));
                }
				if (instr.Level > 0 && !dataPointsWish.Any(dp => dp.name.Equals(nameOfDataPoint + instr.Level)))
				{
					dataPointsWish.Add(new PieChartItemModel(nameOfDataPoint + instr.Level, 0));
				}


				int dataPointIndex = dataPointsReal.FindIndex(dp => dp.name.Equals(nameOfDataPoint + instr.Level));
                if (dataPointIndex == -1)
                {
                    dataPointIndex = 0;
                }
				int dataPointIndexWish = dataPointsWish.FindIndex(dp => dp.name.Equals(nameOfDataPoint + instr.Level));
				if (dataPointIndexWish == -1)
				{
					dataPointIndexWish = 0;
				}

				// add volume to totalRealVolume                
                if (instr.RealVolume is not null)
                {
                    totalRealVolume = totalRealVolume + (decimal)instr.RealVolume;
                    dataPointsReal[dataPointIndex].y = dataPointsReal[dataPointIndex].y + (decimal)instr.RealVolume;
                    realValueNotNull = (decimal)instr.RealVolume;
                }
				// add volume to totalWishVolume
				if (instr.WishVolume > realValueNotNull)
                {
					totalWishVolume = totalWishVolume + (decimal)instr.WishVolume;
					dataPointsWish[dataPointIndexWish].y = dataPointsWish[dataPointIndexWish].y + (decimal)instr.WishVolume;
					wishValueNotNull = (decimal)instr.WishVolume;
				}
                else
                {
					totalWishVolume = totalWishVolume + realValueNotNull;
					dataPointsWish[dataPointIndexWish].y = dataPointsWish[dataPointIndexWish].y + realValueNotNull;
					wishValueNotNull = realValueNotNull;
				}

                // add instr to description
                dataPointsReal[dataPointIndex].description = dataPointsReal[dataPointIndex].description +
                    $"<br /><strong>{instr.SecCode}</strong> " +
                    $"{realValueNotNull.ToString("# ### ### ##0.00")}";

				dataPointsWish[dataPointIndexWish].description = dataPointsWish[dataPointIndexWish].description +
                    $"<br /><strong>{instr.SecCode}</strong> " +
                    $"{wishValueNotNull.ToString("# ### ### ##0.00")}";
			}


			// set precent
			decimal onePercentReal = totalRealVolume / 100;
			foreach (PieChartItemModel item in dataPointsReal)
			{
				item.percent = Math.Round(item.y / onePercentReal, 2);
			}

			decimal onePercentWish = totalWishVolume / 100;
			foreach (PieChartItemModel item in dataPointsWish)
			{
				item.percent = Math.Round(item.y / onePercentWish, 2);
			}

			// sort dataPoints
			IOrderedEnumerable<PieChartItemModel> dataPointsSortedReal = dataPointsReal.OrderByDescending(t => t.name);
			IOrderedEnumerable<PieChartItemModel> dataPointsSortedWish = dataPointsWish.OrderByDescending(t => t.name);


			ViewBag.ChartItemsCount = 2;// layout load script: @if (ViewBag.ChartItemsCount is not null)
			ViewBag.ChartItemArrayReal = JsonConvert.SerializeObject(dataPointsSortedReal);
			ViewBag.ChartItemArrayWish = JsonConvert.SerializeObject(dataPointsSortedWish);
			//ViewBag.Height = "height:850px;";
			return View();
		}
	}
}
