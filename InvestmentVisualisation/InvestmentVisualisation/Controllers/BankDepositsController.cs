using DataAbstraction.Interfaces;
using DataAbstraction.Models;
using DataAbstraction.Models.BankDeposits;
using DataAbstraction.Models.MoneyByMonth;
using DataAbstraction.Models.Settings;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using UserInputService;

namespace InvestmentVisualisation.Controllers
{
	public class BankDepositsController : Controller
	{
		private readonly ILogger<BankDepositsController> _logger;
		private IMySqlBankDepositsRepository _repository;
		private int _itemsAtPage;
		private InputHelper _helper;

		public BankDepositsController(
			ILogger<BankDepositsController> logger,
			IMySqlBankDepositsRepository repository,
			IOptions<PaginationSettings> paginationSettings,
			InputHelper helper
			)
		{
			_logger = logger;
			_repository = repository;
			_itemsAtPage = paginationSettings.Value.PageItemsCount;
			_helper = helper;
		}

		public async Task<IActionResult> Index(
			CancellationToken cancellationToken, 
			int page = 1, 
			bool showOnlyActive = true)
		{
			_logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} BankDepositsController GET " +
				$"Index called, page={page}");
			ViewBag.ShowOnlyActive = showOnlyActive;

			int count = 0;
			if (showOnlyActive)
			{
				count = await _repository.GetActiveBankDepositsCount(cancellationToken);
			}
			else
			{
				count = await _repository.GetAnyBankDepositsCount(cancellationToken);
			}
			_logger.LogDebug($"{DateTime.Now.ToString("HH:mm:ss:fffff")} BankDepositsController Index table size={count}");
			if (count == 0)
			{
				return View();
			}


			BankDepositsWithPaginations bankDepositsWithPaginations = new BankDepositsWithPaginations();

			bankDepositsWithPaginations.PageViewModel = new PaginationPageViewModel(
			count,
			page,
			_itemsAtPage);

			if (showOnlyActive)
			{
				bankDepositsWithPaginations.BankDeposits = await _repository
					.GetPageWithActiveBankDeposits(_itemsAtPage, (page - 1) * _itemsAtPage, cancellationToken);
			}
			else
			{
				bankDepositsWithPaginations.BankDeposits = await _repository
					.GetPageWithAnyBankDeposits(_itemsAtPage, (page - 1) * _itemsAtPage, cancellationToken);
			}

			return View(bankDepositsWithPaginations);
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Close(
			string summIncome,
			int id,
			int page,
			bool showOnlyActive,
			CancellationToken cancellationToken)
		{
			_logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} BankDepositsController POST " +
				$"Close called with summIncome={summIncome} id={id} page={page} showOnlyActive={showOnlyActive}");
			if (!_helper.IsDecimal(summIncome))
			{
				// return error
				TempData["Error"] = $"Введённое значение {summIncome} невозможно преобразовать в число!";
				return RedirectToAction("Index", new { page = page, showOnlyActive = showOnlyActive });
			}

			decimal decimalSummImcome = _helper.GetDecimalFromString(summIncome);
			if (decimalSummImcome <= 0)
			{
				// return error
				TempData["Error"] = $"Введённое значение {summIncome} меньше или равно нулю! Такой доход нам не нужен!";
				return RedirectToAction("Index", new { page = page, showOnlyActive = showOnlyActive });
			}

			CloseBankDepositModel closeBankDeposit = new CloseBankDepositModel
			{
				Id = id,
				SummIncome = decimalSummImcome.ToString().Replace(',', '.')
			};

			string result = await _repository.Close(closeBankDeposit, cancellationToken);
			if (!result.Equals("1"))
			{
				if (result.Equals("0"))
				{
					result = "Ни одной строки в БД не изменилось. Ищи проблему";
				}

				TempData["Error"] = $"Закрытие вклада не удалось: {result}";
			}

			return RedirectToAction("Index", new { page = page, showOnlyActive = showOnlyActive });
		}


		public async Task<IActionResult> Edit(
			int id,
			int page,
			bool showOnlyActive,
			CancellationToken cancellationToken)
		{
			_logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} BankDepositsController " +
				$"Edit called with id={id} page={page} showOnlyActive={showOnlyActive}");

			ViewBag.ShowOnlyActive = showOnlyActive;
			ViewBag.PageNumber = page;

			// try get model, if not or error - return Index page 
			BankDepositModel? editedItem = await _repository.GetBankDepositById(id, cancellationToken);
			if (editedItem is null)
			{
				// return error
				TempData["Error"] = $"Не удалось получить банковский депозит с ID={id}. Ищи проблему";
				return RedirectToAction("Index", new { page = page, showOnlyActive = showOnlyActive });
			}

			return View(editedItem);
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Edit(
			BankDepositModel model,
			int page,
			bool showOnlyActive,
			CancellationToken cancellationToken)
		{
			_logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} BankDepositsController " +
				$"Edit called with id={model.Id} Name={model.Name} PlaceNameSign={model.PlaceNameSign} " +
				$"Summ={model.Summ} Percent={model.Percent} DateOpen={model.DateOpen} DateClose={model.DateClose}" +
				$"IsOpen={model.IsOpen} SummIncome='{model.SummIncome}'");

			ViewBag.ShowOnlyActive = showOnlyActive;
			ViewBag.PageNumber = page;

			// if is open = 0 ---------------------------> check income_summ is not null
			if (!model.IsOpen && (model.SummIncome is null || model.SummIncome.Equals("") || model.SummIncome.Equals(0)))
			{
				ViewData["Message"] = $"Недопустимо для закрытого вклада не указывать сумму полученной прибыли!";
				return View(model);
			}
			if (model.IsOpen && model.SummIncome is not null)
			{
				ViewData["Message"] = $"Недопустимо для открытого вклада указывать сумму полученной прибыли! " +
					$"Ничего ещё не получено!";
				return View(model);
			}
			// check date close > date open
			if (model.DateClose <= model.DateOpen)
			{
				ViewData["Message"] = $"Дата закрытия должна быть больше даты открытия!";
				return View(model);
			}
			//check numbers
			if (!_helper.IsDecimal(model.Summ))
			{
				ViewData["Message"] = $"Объем вклада не распознан как число!";
				return View(model);
			}
			else if (_helper.GetDecimalFromString(model.Summ) <= 10000)
			{
				ViewData["Message"] = $"Объем вклада слишком маленький! Меньше 10 тысяч - что за слёзки?";
				return View(model);
			}

			if (!_helper.IsDecimal(model.Percent))
			{
				ViewData["Message"] = $"Процентная ставка вклада не распознана как число!";
				return View(model);
			}
			else if (_helper.GetDecimalFromString(model.Percent) <= 1)
			{
				ViewData["Message"] = $"Процентная ставка вклада слишком маленькая! Меньше процента - зачем такое вообще?";
				return View(model);
			}

			if (model.SummIncome is not null && !_helper.IsDecimal(model.SummIncome))
			{
				ViewData["Message"] = $"Доход по вкладу не распознан как число!";
				return View(model);
			}
			else if (model.SummIncome is not null && _helper.GetDecimalFromString(model.SummIncome) <= 100)
			{
				ViewData["Message"] = $"Доход по вкладу слишком маленький! Меньше 100руб - что за слёзки?";
				return View(model);
			}

			// fix delimeters
			model.Summ = model.Summ.Replace(',', '.');
			model.Percent = model.Percent.Replace(',', '.');
			if (model.SummIncome is not null)
			{
				model.SummIncome = model.SummIncome.Replace(',', '.');
			}

			string result = await _repository.Edit(model, cancellationToken);
			if (!result.Equals("1"))
			{
				if (result.Equals("0"))
				{
					result = "Ни одной строки в БД не изменилось. Ищи проблему";
				}

				ViewData["Message"] = $"Редактирование не удалось: {result}";
			}

			return View(model);
		}

		public ActionResult Create(
			NewBankDepositModel model,
			int page,
			bool showOnlyActive)
		{
			_logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} BankDepositsController GET Create called");

			//CreateDealsModel model = new CreateDealsModel();
			model.PlaceNameSign = 1;
			model.Summ = "100000";
			model.Percent = "10";
			if (model.DateOpen == DateTime.MinValue)
			{
				model.DateOpen = DateTime.Parse(DateOnly.FromDateTime(DateTime.Now).ToShortDateString());
			}

			if (model.DateClose == DateTime.MinValue)
			{
				model.DateClose = DateTime.Parse(DateOnly.FromDateTime(DateTime.Now.AddMonths(1)).ToShortDateString());
			}

			ViewBag.ShowOnlyActive = showOnlyActive;
			ViewBag.PageNumber = page;

			return View(model);
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Create(
			NewBankDepositModel model,
			bool showOnlyActive,
			CancellationToken cancellationToken)
		{
			_logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} BankDepositsController " +
				$"Create POST called with Name={model.Name} PlaceNameSign={model.PlaceNameSign} " +
				$"Summ={model.Summ} Percent={model.Percent} DateOpen={model.DateOpen} DateClose={model.DateClose}");

			ViewBag.ShowOnlyActive = showOnlyActive;


			// check date close > date open
			if (model.DateClose <= model.DateOpen)
			{
				ViewData["Message"] = $"Дата закрытия должна быть больше даты открытия!";
				return View(model);
			}
			//check numbers
			if (!_helper.IsDecimal(model.Summ))
			{
				ViewData["Message"] = $"Объем вклада не распознан как число!";
				return View(model);
			}
			else if (_helper.GetDecimalFromString(model.Summ) <= 10000)
			{
				ViewData["Message"] = $"Объем вклада слишком маленький! Меньше 10 тысяч - что за слёзки?";
				return View(model);
			}

			if (!_helper.IsDecimal(model.Percent))
			{
				ViewData["Message"] = $"Процентная ставка вклада не распознана как число!";
				return View(model);
			}
			else if (_helper.GetDecimalFromString(model.Percent) <= 1)
			{
				ViewData["Message"] = $"Процентная ставка вклада слишком маленькая! Меньше процента - зачем такое вообще?";
				return View(model);
			}


			// fix delimeters
			model.Summ = model.Summ.Replace(',', '.');
			model.Percent = model.Percent.Replace(',', '.');

			string result = await _repository.Create(model, cancellationToken);
			if (!result.Equals("1"))
			{
				if (result.Equals("0"))
				{
					result = "Ни одной строки в БД не изменилось. Ищи проблему";
				}

				ViewData["Message"] = $"Редактирование не удалось: {result}";
				return View(model);
			}

			return RedirectToAction("Index", new { page = 1, showOnlyActive = showOnlyActive });
		}



		public async Task<IActionResult> Chart(CancellationToken cancellationToken)
		{
			_logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} BankDepositsController Chart called");
			int backSpaceAtChart = -25;
			List<BankDepoDBModel>? bankDepoDBModelItems = await _repository.GetBankDepoChartData(cancellationToken);

			if (bankDepoDBModelItems is not null)
			{
				List<DateTimeOffset> collectionOfAllDate = CreateAndSortCollectionOfAllDates(bankDepoDBModelItems);

				decimal totalSumm = 0;

				ChartItemModel[] сhartItems = new ChartItemModel[bankDepoDBModelItems.Count];
				for (int i = 0; i < bankDepoDBModelItems.Count; i++)
				{
					List<DataPointsOfChartItemModel> dataPoints = new List<DataPointsOfChartItemModel>();
					DataPointsOfChartItemModel dataPointStart = new DataPointsOfChartItemModel(
						bankDepoDBModelItems[i].DateOpen.ToUnixTimeSeconds() * 1000,
						i + 1,
						bankDepoDBModelItems[i].Percent.ToString() + "% " + bankDepoDBModelItems[i].SummAmount + "руб; " +
						bankDepoDBModelItems[i].Name);
					dataPoints.Add(dataPointStart);

					foreach (DateTimeOffset dateFromCollection in collectionOfAllDate)
					{
						if (dateFromCollection > bankDepoDBModelItems[i].DateOpen &&
							dateFromCollection < bankDepoDBModelItems[i].DateClose)
						{
							DataPointsOfChartItemModel dataPointTest = new DataPointsOfChartItemModel(
								dateFromCollection.ToUnixTimeSeconds() * 1000,
											   i + 1,
											   "");
							dataPoints.Add(dataPointTest);
						}

						if (dateFromCollection >= bankDepoDBModelItems[i].DateClose)
						{
							break;
						}
					}

					DataPointsOfChartItemModel dataPointEnd = new DataPointsOfChartItemModel(
						bankDepoDBModelItems[i].DateClose.ToUnixTimeSeconds() * 1000,
						i + 1,
						bankDepoDBModelItems[i].DateClose.ToString("до dd MMM yyyy"));
					dataPoints.Add(dataPointEnd);


					string color = "#F08080"; // type 1  = FinUslugi
					if (bankDepoDBModelItems[i].PlaceName == 2)
					{
						color = "#ffff00"; // T-bank
					}
					else if (bankDepoDBModelItems[i].PlaceName == 3)
					{
						color = "#00FF00";// Sber
					}


					ChartItemModel newChartItem = new ChartItemModel(
						bankDepoDBModelItems[i].Name + " " + bankDepoDBModelItems[i].SummAmount + "руб.",
						color,
						dataPoints,
						(int)bankDepoDBModelItems[i].SummAmount / 20000,
						"line"
						);

					totalSumm += bankDepoDBModelItems[i].SummAmount;

					сhartItems[i] = newChartItem;
				}

				// second chart - money income from bank
				List<BankDepoDBPaymentData>? bankDepoDBPayments = await _repository.GetBankDepositsEndedAfterDate(
					cancellationToken,
					collectionOfAllDate[0].AddDays(backSpaceAtChart).ToString("yyyy-MM-dd")
					);
				if (bankDepoDBPayments is not null)
				{
					List<ExtendedDataPointsOfChartItemModel> payments = new List<ExtendedDataPointsOfChartItemModel>();
					foreach (BankDepoDBPaymentData paymentData in bankDepoDBPayments)
					{
						decimal summToPayWithTax =
							(
								(paymentData.SummAmount * paymentData.Percent * paymentData.DaysOfDeposit)
								/365
							)
							/100;

						ExtendedDataPointsOfChartItemModel newChartDataPoint = new ExtendedDataPointsOfChartItemModel(
							paymentData.DateClose.ToUnixTimeSeconds() * 1000,
							summToPayWithTax,
							summToPayWithTax,
							paymentData.Name + " (" + paymentData.SummAmount + ")"
							);

						if (!paymentData.IsOpen && paymentData.IncomeSummAmount is not null)
						{
							newChartDataPoint.y = (decimal)paymentData.IncomeSummAmount;
							newChartDataPoint.z = (decimal)paymentData.IncomeSummAmount;
						}

						payments.Add(newChartDataPoint);
					}
					ViewBag.PaymentsChartItemArray = JsonConvert.SerializeObject(payments);
				}

				ViewBag.ShowChartFrom = collectionOfAllDate[0].AddDays(backSpaceAtChart).ToUnixTimeSeconds() * 1000;
				ViewBag.ShowChartTo = collectionOfAllDate[collectionOfAllDate.Count - 1].AddDays(15).ToUnixTimeSeconds() * 1000;
				ViewBag.TotalSumm = totalSumm;
				ViewBag.ChartItemArray = JsonConvert.SerializeObject(сhartItems);

				ViewBag.ChartItemsCount = сhartItems.Length;// layout load script: @if (ViewBag.ChartItemsCount is not null)
			}

			return View();
		}
		private List<DateTimeOffset> CreateAndSortCollectionOfAllDates(List<BankDepoDBModel> bankDepoDBModelItems)
		{
			List<DateTimeOffset> dates = new List<DateTimeOffset>();
			foreach (BankDepoDBModel item in bankDepoDBModelItems)
			{
				if (!dates.Contains(item.DateOpen))
				{
					dates.Add(item.DateOpen);
				}

				if (!dates.Contains(item.DateClose))
				{
					dates.Add(item.DateClose);
				}
			}

			dates.Sort();
			return dates;
		}
	}
}
