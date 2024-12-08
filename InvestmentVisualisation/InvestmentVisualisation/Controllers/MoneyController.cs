﻿using DataAbstraction.Models.Settings;
using DataAbstraction.Interfaces;
using Microsoft.AspNetCore.Mvc;
using DataAbstraction.Models.MoneyByMonth;
using Microsoft.Extensions.Options;
using DataAbstraction.Models;
using DataAbstraction.Models.BaseModels;
using System.Globalization;
using Newtonsoft.Json;

namespace InvestmentVisualisation.Controllers
{
    public class MoneyController : Controller
    {
        private readonly ILogger<MoneyController> _logger;
        private IMySqlMoneyRepository _repository;
        private int _minimumYear;
        private IWebDividents _webRepository;

        public MoneyController(
            ILogger<MoneyController> logger,
            IMySqlMoneyRepository repository,
            IWebDividents webRepository,
            IOptions<PaginationSettings> paginationSettings)
        {
            _logger = logger;
            _repository = repository;
            _minimumYear = paginationSettings.Value.MoneyMinimumYear;
            _webRepository = webRepository;

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
                List<BankDepoDBPaymentData> ? bankDepoDBPayments = await _repository.GetBankDepositsEndedAfterDate(
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

        public async Task<IActionResult> MoneySpentAndIncomeOfLast12MonthChart(CancellationToken cancellationToken)
        {
            _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} DealsController " +
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

        public async Task<IActionResult> ExpectedDividentsChart(CancellationToken cancellationToken)
        {
            _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} DealsController " +
                $"ExpectedDividentsFromDohodChart called");

            // get web site data
            List<ExpectedDividentsFromWebModel>? dohodDivs = _webRepository.GetFutureDividentsTableFromDohod(cancellationToken);
            if (dohodDivs is null ||
                dohodDivs.Count ==  0)
            {
                // show error at page --------------------------------------
                return View();
            }
            // get database securities list
            List<SecCodeAndNameAndPiecesModel>? secCodeAndPieces = await _repository.GetActualSecCodeAndNameAndPieces(
                    cancellationToken,
                    DateTime.Now.Year);
            if (secCodeAndPieces is null ||
                secCodeAndPieces.Count == 0)
            {
                // show error at page --------------------------------------
                return View();
            }


            // remove not my seccodes from web result
            for (int i = dohodDivs.Count - 1; i >= 0; i--)
            {
                bool isExist = false;

                foreach (SecCodeAndNameAndPiecesModel dbItem in secCodeAndPieces)
                {
                    if (dohodDivs[i].SecCode.Equals(dbItem.SecCode))
                    {
                        dohodDivs[i].Pieces = dbItem.Pieces;
                        isExist = true;
                        break;
                    }
                }

                if (!isExist)
                {
                    dohodDivs.RemoveAt(i);
                }
            }


            // sort dohodDivs by date
            IOrderedEnumerable<ExpectedDividentsFromWebModel> sortedDohodDivs = from p in dohodDivs
                                                                                orderby p.Date
                                                                                select p;
            dohodDivs = sortedDohodDivs.ToList();


			// Create list of chart items models
			List<ExtendedDataPointsOfChartItemModel> extendedDataPoints = new List<ExtendedDataPointsOfChartItemModel>();
            List<DataPointsOfChartItemModel> volumeDataPoints = new List<DataPointsOfChartItemModel>();
            int summ = 0;
			foreach (ExpectedDividentsFromWebModel webDiv in dohodDivs)
			{
                if (!webDiv.IsConfirmed)
                {
                    continue;
                }

                long dateOfDiv = new DateTimeOffset(webDiv.Date.AddDays(18)).ToUnixTimeSeconds() * 1000;
                decimal onePercent = (webDiv.DividentToOnePiece * webDiv.Pieces) / 100;
                decimal amountMinusTax = (webDiv.DividentToOnePiece * webDiv.Pieces) - (onePercent * 15);
				summ = summ + (int)amountMinusTax;

				ExtendedDataPointsOfChartItemModel newDataPoint = new ExtendedDataPointsOfChartItemModel(
					dateOfDiv,
					webDiv.DividentInPercents,
					amountMinusTax,
					webDiv.SecCode
					);

				extendedDataPoints.Add( newDataPoint );

                DataPointsOfChartItemModel newVolumeDataPoint = new DataPointsOfChartItemModel(
					dateOfDiv,
					summ);
				volumeDataPoints.Add ( newVolumeDataPoint );
			}

			List<ExtendedDataPointsOfChartItemModel> possibleDivsExtendedDataPoints = new List<ExtendedDataPointsOfChartItemModel>();
			List<DataPointsOfChartItemModel> possibleDivsVolumeDataPoints = new List<DataPointsOfChartItemModel>();
            int couterColor = 0;
			foreach (ExpectedDividentsFromWebModel webDiv in dohodDivs)
			{
				if (webDiv.IsConfirmed)
				{
					continue;
				}

                string[] pseudoRandomColorTable = new string [] 
                {
					"rgba(10,200,10,0.3)",
					"rgba(10,10,200,0.3)",
					"rgba(200,10,10,0.3)",
					"rgba(10,200,200,0.3)",
					"rgba(200,200,10,0.3)",
					"rgba(200,10,200,0.3)",
					"rgba(200,200,200,0.3)",
					"rgba(50,255,99,0.3)"
				};

				long dateOfDiv = new DateTimeOffset(webDiv.Date.AddDays(18)).ToUnixTimeSeconds() * 1000;
				decimal onePercent = (webDiv.DividentToOnePiece * webDiv.Pieces) / 100;
				decimal amountMinusTax = (webDiv.DividentToOnePiece * webDiv.Pieces) - (onePercent * 15);
				summ = summ + (int)amountMinusTax;

				ExtendedDataPointsOfChartItemModel newDataPoint = new ExtendedDataPointsOfChartItemModel(
					dateOfDiv,
					webDiv.DividentInPercents,
					amountMinusTax,
					webDiv.SecCode,
                    pseudoRandomColorTable[couterColor]
					);
                couterColor ++;
                if ( couterColor == pseudoRandomColorTable.Length)
                {
                    couterColor = 0;

				}
				possibleDivsExtendedDataPoints.Add(newDataPoint);

				DataPointsOfChartItemModel newVolumeDataPoint = new DataPointsOfChartItemModel(
					dateOfDiv,
					summ);
				possibleDivsVolumeDataPoints.Add(newVolumeDataPoint);
			}

			ViewBag.ChartDataPoints = JsonConvert.SerializeObject(extendedDataPoints);
			ViewBag.ChartVolumeDataPoints = JsonConvert.SerializeObject(volumeDataPoints);

			ViewBag.PossibleDivsChartDataPoints = JsonConvert.SerializeObject(possibleDivsExtendedDataPoints);
			ViewBag.PossibleDivsChartVolumeDataPoints = JsonConvert.SerializeObject(possibleDivsVolumeDataPoints);

			ViewBag.ChartItemsCount = extendedDataPoints.Count;// layout load script: @if(ViewBag.ChartItemsCount != null)

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


