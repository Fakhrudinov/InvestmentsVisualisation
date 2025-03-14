using DataAbstraction.Models.Settings;
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
            _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MoneyController GET Index called, year={year}");

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

			decimal averageDivident = 0;
			decimal averageBrokComission = 0;
            int counter = 0;
			DateTime dateNowFirstDay = new DateTime(currentYear, DateTime.Now.Month, 1);

			foreach (MoneyModel money in moneyList)
            {
				DateTime dateFromBD = money.Date;
                if (dateNowFirstDay <= dateFromBD)
                {
                    continue;
                }

				if (money.Divident is not null)
                {
					averageDivident += (decimal)money.Divident;
				}

				if (money.BrokComission is not null)
				{
					averageBrokComission += (decimal)money.BrokComission;
				}
                counter++;
			}

            if (counter > 0)
            {
                averageDivident = averageDivident/counter;
                averageBrokComission = averageBrokComission/counter;
			}

            ViewBag.AverageDivident = averageDivident.ToString("### ### ###.00");
            ViewBag.AverageBrokComission = averageBrokComission.ToString("### ### ###.00");

			return View(moneyList);
        }
        public async Task<IActionResult> Recalculate(CancellationToken cancellationToken, int year = 0, int month = 0)
        {
            _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MoneyController GET Recalculate called, " +
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

        public async Task<IActionResult> MoneySpentAndIncomeOfLast12MonthChart(CancellationToken cancellationToken)
        {
            _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MoneyController " +
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
            _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MoneyController " +
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


			string[] pseudoRandomColorTable = new string[]
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
			int couterColor = 0;

			// Create list of chart items models
			List<ExtendedDataPointsOfChartItemModel> approovedDivsExtendedDataPoints = new List<ExtendedDataPointsOfChartItemModel>();
            List<DataPointsOfChartItemModel> approovedDivsVolumeDataPoints = new List<DataPointsOfChartItemModel>();
            int summApprooved = 0;
			int summPossible = 0;
            List<ExtendedDataPointsOfChartItemModel> possibleDivsExtendedDataPoints = new List<ExtendedDataPointsOfChartItemModel>();
			List<DataPointsOfChartItemModel> possibleDivsVolumeDataPoints = new List<DataPointsOfChartItemModel>();

			foreach (ExpectedDividentsFromWebModel webDiv in dohodDivs) 
            {
				long dateOfDiv = new DateTimeOffset(webDiv.Date.AddDays(18)).ToUnixTimeSeconds() * 1000;
				decimal onePercent = (webDiv.DividentToOnePiece * webDiv.Pieces) / 100;
				decimal amountMinusTax = (webDiv.DividentToOnePiece * webDiv.Pieces) - (onePercent * 15);

				ExtendedDataPointsOfChartItemModel newDataPoint = new ExtendedDataPointsOfChartItemModel(
					dateOfDiv,
					webDiv.DividentInPercents,
					amountMinusTax,
					webDiv.SecCode,
					pseudoRandomColorTable[couterColor]
					);
				couterColor++;
				if (couterColor == pseudoRandomColorTable.Length)
				{
					couterColor = 0;
				}

                if (webDiv.IsConfirmed)
                {
					approovedDivsExtendedDataPoints.Add(newDataPoint);

					summApprooved = summApprooved + (int)amountMinusTax;
					DataPointsOfChartItemModel newVolumeDataPoint = new DataPointsOfChartItemModel(
	                    dateOfDiv,
	                    summApprooved);
					approovedDivsVolumeDataPoints.Add(newVolumeDataPoint);
				}
                else
                {
                    possibleDivsExtendedDataPoints.Add(newDataPoint);

					summPossible = summPossible + (int)amountMinusTax;
					DataPointsOfChartItemModel newVolumeDataPoint = new DataPointsOfChartItemModel(
	                    dateOfDiv,
	                    summPossible);
					possibleDivsVolumeDataPoints.Add(newVolumeDataPoint);
				}
            }

			ViewBag.ChartDataPoints = JsonConvert.SerializeObject(approovedDivsExtendedDataPoints);
			ViewBag.ChartVolumeDataPoints = JsonConvert.SerializeObject(approovedDivsVolumeDataPoints);

			ViewBag.PossibleDivsChartDataPoints = JsonConvert.SerializeObject(possibleDivsExtendedDataPoints);
			ViewBag.PossibleDivsChartVolumeDataPoints = JsonConvert.SerializeObject(possibleDivsVolumeDataPoints);

			ViewBag.ChartItemsCount = approovedDivsExtendedDataPoints.Count;// layout load script: @if(ViewBag.ChartItemsCount != null)

			return View();
        }
    }
}


