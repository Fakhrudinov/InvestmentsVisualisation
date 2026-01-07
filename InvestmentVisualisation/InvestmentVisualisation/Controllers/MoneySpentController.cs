using DataAbstraction.Interfaces;
using DataAbstraction.Models.Settings;
using DataAbstraction.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using DataAbstraction.Models.MoneyByMonth;
using Newtonsoft.Json;
using DataAbstraction.Models.Deals;
using DataAbstraction.Models.MoneySpent;
using System.Globalization;

namespace InvestmentVisualisation.Controllers
{
	public class MoneySpentController : Controller
	{
		private readonly ILogger<MoneySpentController> _logger;
		private IMySqlMoneySpentRepository _repository;
		private int _itemsAtPage;

		public MoneySpentController(
			ILogger<MoneySpentController> logger,
			IMySqlMoneySpentRepository repository,
			IOptions<PaginationSettings> paginationSettings)
		{
			_logger = logger;
			_repository = repository;
			_itemsAtPage = paginationSettings.Value.PageItemsCount;
		}

		[Authorize]
		public async Task<IActionResult> Index(CancellationToken cancellationToken, int page = 1)
		{
			_logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MoneySpentController GET " +
				$"Index called, page={page}");

			int count = await _repository.GetMoneySpentCount(cancellationToken);
			_logger.LogDebug($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MoneySpentController MoneySpent table size={count}");

			MoneySpentWithPaginations items = new MoneySpentWithPaginations();

			items.PageViewModel = new PaginationPageViewModel(
			count,
			page,
			_itemsAtPage);

			items.DataByMonths = await _repository
					.GetPageFromMoneySpent(cancellationToken, _itemsAtPage, (page - 1) * _itemsAtPage);

			return View(items);
		}

		[Authorize(Roles = "Admin")]
		public async Task<IActionResult> Edit(
			CancellationToken cancellationToken, 
			DateTime date)
		{
			_logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} DealsController GET Edit date={date} called");

			MoneySpentByMonthModel? editedItem = await _repository.GetSingleRowByDateTime(cancellationToken, date);
			if (editedItem is null)
			{
				TempData["Message"] = $"Данные с датой {date} не найдены!";
				return RedirectToAction("Index");
			}

			return View(editedItem);
		}

		[Authorize(Roles = "Admin")]
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Edit(
			CancellationToken cancellationToken, 
			MoneySpentByMonthModel model)
		{
			_logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} DealsController HttpPost Edit called");

			model = ReplaceAllCommasToDots(model);

			string result = await _repository.EditMoneySpentItem(cancellationToken, model);
			if (!result.Equals("1"))
			{
				ViewData["Message"] = $"Редактирование не удалось.\r\n{result}";
				return View(model);
			}

			return RedirectToAction("Index");
		}

		private MoneySpentByMonthModel ReplaceAllCommasToDots(MoneySpentByMonthModel model)
		{
			if (model.Total is not null)
			{
				model.Total = model.Total.Replace(',', '.');
			}
			if (model.Appartment is not null)
			{
				model.Appartment = model.Appartment.Replace(',', '.');
			}
			if (model.Electricity is not null)
			{
				model.Electricity = model.Electricity.Replace(',', '.');
			}
			if (model.Phone is not null)
			{
				model.Phone = model.Phone.Replace(',', '.');
			}
			if (model.Internet is not null)
			{
				model.Internet = model.Internet.Replace(',', '.');
			}
			if (model.Transport is not null)
			{
				model.Transport = model.Transport.Replace(',', '.');
			}
			if (model.SuperMarkets is not null)
			{
				model.SuperMarkets = model.SuperMarkets.Replace(',', '.');
			}
			if (model.MarketPlaces is not null)
			{
				model.MarketPlaces = model.MarketPlaces.Replace(',', '.');
			}

			return model;
		}

		[Authorize(Roles = "Admin")]
		public async Task<IActionResult> Create (CancellationToken cancellationToken)
		{
			_logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} DealsController GET Create called");

			// get data from last record from DB: 1 row
			// add 1 month to date from last record
			// create new prefilled record
			// return to last page

			MoneySpentByMonthModel? lastItem = await _repository.GetLastRow(cancellationToken);
			if (lastItem is null)
			{
				TempData["Message"] = $"Добавление не удалось. Не получена последняя запись - образец";
				return RedirectToAction("Index");
			}

			DateTime newDate = DateTime.Parse(lastItem.Date, new CultureInfo("ru-RU", true)).AddMonths(1);
			lastItem.Date = newDate.ToString("yyyy-MM-dd");

			lastItem = ReplaceAllCommasToDots(lastItem);

			string result = await _repository.CreateNewItem(cancellationToken, lastItem);
			if (!result.Equals("1"))
			{
				TempData["Message"] = $"Добавление не удалось. Ошибка вставки новой строки:\r\n{result}";
				return RedirectToAction("Index");
			}

			return RedirectToAction("Index");
		}

		[AllowAnonymous]
		public async Task<IActionResult> MoneySpentAndIncomeOfLast12MonthChart(CancellationToken cancellationToken)
		{
			_logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MoneySpentController " +
				$"MoneySpentAndIncomeOfLast12MonthChart called");

			List<MoneySpentAndIncomeModel>? moneySpentAndIncomeDBModelItems = await _repository
				.GetMoneySpentAndIncomeModelChartData(cancellationToken);

			if (moneySpentAndIncomeDBModelItems is not null)
			{
				ChartItemModel[] chartItems = new ChartItemModel[3];
				List<DataPointsOfChartItemModel> dataPointsDivident = new List<DataPointsOfChartItemModel>();
				List<DataPointsOfChartItemModel> dataPointsAverageDivident = new List<DataPointsOfChartItemModel>();
				List<DataPointsOfChartItemModel> dataPointsMoneySpent = new List<DataPointsOfChartItemModel>();
				int maximumOfDividents = 0;
				int maximumOfMonthly = 0;
				for (int i = 0; i < moneySpentAndIncomeDBModelItems.Count; i++)
				{
					// for scaleBreaks high value
					if (moneySpentAndIncomeDBModelItems[i].Divident > maximumOfDividents)
					{
						maximumOfDividents = moneySpentAndIncomeDBModelItems[i].Divident;
					}

					// for scaleBreaks low value
					if (moneySpentAndIncomeDBModelItems[i].AverageDivident > maximumOfMonthly)
					{
						maximumOfMonthly = moneySpentAndIncomeDBModelItems[i].AverageDivident;
					}
					if (moneySpentAndIncomeDBModelItems[i].MoneySpent > maximumOfMonthly)
					{
						maximumOfMonthly = moneySpentAndIncomeDBModelItems[i].MoneySpent;
					}

					DataPointsOfChartItemModel dataPointDivident = new DataPointsOfChartItemModel(
						moneySpentAndIncomeDBModelItems[i].Date.ToUnixTimeSeconds() * 1000,
						moneySpentAndIncomeDBModelItems[i].Divident);
					dataPointsDivident.Add(dataPointDivident);

					DataPointsOfChartItemModel dataPointAverageDivident = new DataPointsOfChartItemModel(
						moneySpentAndIncomeDBModelItems[i].Date.ToUnixTimeSeconds() * 1000,
						moneySpentAndIncomeDBModelItems[i].AverageDivident);
					dataPointsAverageDivident.Add(dataPointAverageDivident);

					DataPointsOfChartItemModel dataPointMoneySpent = new DataPointsOfChartItemModel(
						moneySpentAndIncomeDBModelItems[i].Date.ToUnixTimeSeconds() * 1000,
						moneySpentAndIncomeDBModelItems[i].MoneySpent);
					dataPointsMoneySpent.Add(dataPointMoneySpent);
				}

				//
				chartItems[0] = new ChartItemModel(
							"average dividents income",
							"rgba(10,200,10,0.5)",
							dataPointsAverageDivident,
							5,
							"area",
							"circle"
						);
				chartItems[1] = new ChartItemModel(
							"money spent",
							"rgba(200,40,40,0.5)",
							dataPointsMoneySpent,
							1,
							"area",
							"circle"
						);
				chartItems[2] = new ChartItemModel(
							"exact dividents at month",
							"rgba(10,10,250,0.9)",
							dataPointsDivident,
							1,
							"line",
							"circle"
						);

				ViewBag.ScaleBreaksHigh = maximumOfDividents;
				ViewBag.ScaleBreaksLow = maximumOfMonthly;
				ViewBag.ChartItemArray = JsonConvert.SerializeObject(chartItems);
				ViewBag.ChartItemsCount = chartItems.Length;// layout load script: @if (ViewBag.ChartItemsCount is not null)
			}

			return View("MoneySpentAndIncomeChart");
		}
	}
}
