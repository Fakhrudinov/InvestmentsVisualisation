using DataAbstraction.Models.Settings;
using DataAbstraction.Interfaces;
using Microsoft.AspNetCore.Mvc;
using DataAbstraction.Models.MoneyByMonth;
using Microsoft.Extensions.Options;
using DataAbstraction.Models;
using DataAbstraction.Models.BaseModels;
using System.Globalization;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;

namespace InvestmentVisualisation.Controllers
{
    public class MoneyController : Controller
    {
        private readonly ILogger<MoneyController> _logger;
        private IMySqlMoneyRepository _repository;
        private int _minimumYear;

        public MoneyController(
            ILogger<MoneyController> logger, 
            IMySqlMoneyRepository repository, 
            IOptions<PaginationSettings> paginationSettings)
        {
            _logger = logger;
            _repository = repository;
            _minimumYear = paginationSettings.Value.MoneyMinimumYear;

            if (StaticData.MonthNames.Count == 0)
            {
                for (int i = 1; i <= 12; i++)
                {
                    MonthName newMonth = new MonthName();
                    newMonth.Id = i;
                    newMonth.Name = new DateTime(2023, i, 1).ToString("MMMM", CultureInfo.CreateSpecificCulture("ru"));

                    StaticData.MonthNames.Add(newMonth);
                }
            }            
        }


        public async Task<IActionResult> Index(CancellationToken cancellationToken, int year = 0)
        {
            _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} DealsController GET Index called, year={year}");

            int currentYear = DateTime.Now.Year;

            List<MoneyModel> moneyList = new List<MoneyModel>();
            if (year == 0)
            {
                moneyList = await _repository.GetMoneyLastYearPage(cancellationToken);
            }
            else
            {
                moneyList = await _repository.GetMoneyYearPage(cancellationToken, year);
            }

            List<int> objSt = new List<int>();
            for (int i = _minimumYear; i <= currentYear; i++)
            {
                objSt.Add(i);
            }
            ViewData["Navigation"] = objSt;
            ViewBag.year = year;

            return View(moneyList);
        }
        public async Task<IActionResult> Recalculate(CancellationToken cancellationToken, int year = 0, int month = 0)
        {
            _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} DealsController GET Recalculate called, " +
                $"year={year} month={month}");

            if (year >= _minimumYear && (month >= 1 && month <= 12))
            {
                await _repository.RecalculateMoney(cancellationToken, $"{year}-{month}-01");
            }

            // если пересчет за последний месяц - обновить значение в layout.
            if (year == DateTime.Now.Year && month == DateTime.Now.Month)
            {
                _repository.FillFreeMoney(cancellationToken);
            }

            return RedirectToAction("Index");
        }

        public async Task<IActionResult> BankDepoChart(CancellationToken cancellationToken)
        {
			_logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} DealsController BankDepoChart called");

            List<BankDepoDataBaseModel> ? bankDepoDBModelItems = await _repository.GetBankDepoChartData(cancellationToken);

            if (bankDepoDBModelItems is not null)
            {
                List<DateTimeOffset> collectionOfAllDate = CreateAndSortCollectionOfAllDates(bankDepoDBModelItems);

                decimal totalSumm = 0;

                BankDepoChartItemModel[] bankDepoChartItems = new BankDepoChartItemModel[bankDepoDBModelItems.Count];
                for (int i = 0; i < bankDepoDBModelItems.Count; i++)
                {
                    List<DataPointsOfBankDepoChartItemModel> dataPoints = new List<DataPointsOfBankDepoChartItemModel>();
					DataPointsOfBankDepoChartItemModel dataPointStart = new DataPointsOfBankDepoChartItemModel(
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
                            DataPointsOfBankDepoChartItemModel dataPointTest = new DataPointsOfBankDepoChartItemModel(
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

					DataPointsOfBankDepoChartItemModel dataPointEnd = new DataPointsOfBankDepoChartItemModel(
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


					BankDepoChartItemModel newChartItem = new BankDepoChartItemModel(
						bankDepoDBModelItems[i].Name + " " + bankDepoDBModelItems[i].SummAmount + "руб.",
						color,
						dataPoints,
						(int)bankDepoDBModelItems[i].SummAmount / 20000
						);

                    totalSumm += bankDepoDBModelItems[i].SummAmount;

					bankDepoChartItems[i] = newChartItem;
				}
				ViewBag.ShowChartFrom = collectionOfAllDate[0].AddDays(-25).ToUnixTimeSeconds() * 1000;
				ViewBag.ShowChartTo = collectionOfAllDate[collectionOfAllDate.Count - 1].AddDays(15).ToUnixTimeSeconds() * 1000;
				ViewBag.TotalSumm = totalSumm;
				ViewBag.BankDepoChartItemArray = JsonConvert.SerializeObject(bankDepoChartItems);
                ViewBag.ChartItemsCount = bankDepoChartItems.Length;// layout load script: @if (ViewBag.ChartItemsCount is not null)
			}

			return View();
        }

		private List<DateTimeOffset> CreateAndSortCollectionOfAllDates(List<BankDepoDataBaseModel> bankDepoDBModelItems)
		{
			List<DateTimeOffset> dates = new List<DateTimeOffset>();
			foreach (BankDepoDataBaseModel item in bankDepoDBModelItems)
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
