using DataAbstraction.Models.Settings;
using DataAbstraction.Interfaces;
using Microsoft.AspNetCore.Mvc;
using DataAbstraction.Models.MoneyByMonth;
using Microsoft.Extensions.Options;
using DataAbstraction.Models;
using DataAbstraction.Models.BaseModels;
using System.Globalization;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Authorization;

namespace InvestmentVisualisation.Controllers
{
    public class MoneyController : Controller
    {
        private readonly ILogger<MoneyController> _logger;
        private IMySqlMoneyRepository _repository;
        private int _minimumYear;
        private IWebData _webRepository;

        public MoneyController(
            ILogger<MoneyController> logger,
            IMySqlMoneyRepository repository,
            IWebData webRepository,
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

		[Authorize]
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
		[Authorize(Roles = "Admin")]
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


        [AllowAnonymous]
        public async Task<IActionResult> ExpectedDividentsChart(CancellationToken cancellationToken)
        {
            _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MoneyController " +
                $"ExpectedDividentsFromDohodChart called");

            // get web site data
            List<ExpectedDividentsFromWebModel>? dohodDivs = await _webRepository.GetFutureDividentsTableFromDohod(cancellationToken);
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

        [AllowAnonymous]
        public async Task<IActionResult> ExpectedMonthBondDividentsChart(CancellationToken cancellationToken)
        {
            _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MoneyController " +
                $"ExpectedMonthBondDividentsChart called");

            ViewData["ExpectedDividentsType"] = "облигациям";


			List<DayAndVolumeAndNameModel>? daysExpected = await _repository.GetExpectedMonthBondDividentsChart(cancellationToken);
            if (daysExpected is null)
            {
                ViewData["ExpectedDividentsErrors"] = "Data from DB not received.";
                return View();
            }


            // get coupondate from moex api
            await _webRepository.GetMoexApiCouponDatesForList(daysExpected, cancellationToken);



			List<RangeDataPointsOfChartItemModel> dataPoints = new List<RangeDataPointsOfChartItemModel>();
            foreach (DayAndVolumeAndNameModel dividendEvent in daysExpected)
            {
                if (dividendEvent.CouponDate is null)
                {
                    continue;
                }

                long dateOfDiv = ReturnDateAsLongWithBackofficeLag((DateTime)dividendEvent.CouponDate);
				int[] volume = ReturnNewArrayOfVolumeStartAndEnd(dataPoints, dividendEvent.Volume, dateOfDiv);
                RangeDataPointsOfChartItemModel dp = new RangeDataPointsOfChartItemModel(dateOfDiv, volume, dividendEvent.Name);
				dataPoints.Add(dp);
			}

			ViewBag.ChartItemsCount = 1;// layout load script: @if (ViewBag.ChartItemsCount is not null)
			ViewBag.ChartItemArray = JsonConvert.SerializeObject(dataPoints);


			return View("ExpectedDatesOfDividentsChart");
		}

		[AllowAnonymous]
		public async Task<IActionResult> ExpectedFutureStockDividentsChart(CancellationToken cancellationToken)
		{
			_logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} MoneyController " +
				$"ExpectedFutureStockDividentsChart called");

			ViewData["ExpectedDividentsType"] = "акциям";

			List<DateAndVolumeAndNameModel>? divsExpected = await _repository.ExpectedFutureStockDividentsChart(cancellationToken);
			if (divsExpected is null)
			{
				ViewData["ExpectedDividentsErrors"] = "Data from DB not received.";
				return View("ExpectedDatesOfDividentsChart");
			}

			List<RangeDataPointsOfChartItemModel> dataPoints = new List<RangeDataPointsOfChartItemModel>();
			foreach (DateAndVolumeAndNameModel dividendEvent in divsExpected)
			{
                DateTime divDate = dividendEvent.Date.AddYears(1);
                divDate = ReturnNewDateNotInHolydays(divDate);
                long dateOfDiv = new DateTimeOffset(divDate).ToUnixTimeSeconds() * 1000;
                int[] volume = ReturnNewArrayOfVolumeStartAndEnd(dataPoints, dividendEvent.Volume, dateOfDiv);

                RangeDataPointsOfChartItemModel dp = new RangeDataPointsOfChartItemModel(dateOfDiv, volume, dividendEvent.Name);
                dataPoints.Add(dp);
            }

			ViewBag.ChartItemsCount = 1;// layout load script: @if (ViewBag.ChartItemsCount is not null)
			ViewBag.ChartItemArray = JsonConvert.SerializeObject(dataPoints);


			return View("ExpectedDatesOfDividentsChart");
		}

		private int[] ReturnNewArrayOfVolumeStartAndEnd(
            List<RangeDataPointsOfChartItemModel> dataPoints, 
            decimal dividendEvent,
			long dateOfDiv)
		{
			int[] volume = new int[2];

			int search = dataPoints.FindLastIndex(dp => dp.x == dateOfDiv);
			if (search > 0)
			{
				volume[0] = dataPoints[search].y[1];
				volume[1] = (int)dividendEvent + dataPoints[search].y[1];
			}
			else
			{
				volume[0] = 0;
				volume[1] = (int)dividendEvent;
			}

            return volume;
		}

		private long ReturnDateAsLongWithBackofficeLag(DateTime couponDate)
		{
			/// check is it holidays?
			///     yes = add days till monday
			/// add day (1) as backoffice lag
			/// check is it holidays?
			///     yes = add days till monday
            /// 
			/// return date as long

			DateTime newDate = ReturnNewDateNotInHolydays(couponDate);
			newDate = couponDate.AddDays(1);
			newDate = ReturnNewDateNotInHolydays(newDate);

			long result = new DateTimeOffset(newDate).ToUnixTimeSeconds() * 1000;

            return result;
        }

		private DateTime ReturnNewDateNotInHolydays(DateTime divDate)
		{
			DayOfWeek dayOfWeek = divDate.DayOfWeek;
			if (dayOfWeek == DayOfWeek.Saturday)
			{
				return divDate.AddDays(2);
			}

			if (dayOfWeek == DayOfWeek.Sunday)
			{
				divDate = divDate.AddDays(1);
			}

            return divDate;
		}
	}
}


