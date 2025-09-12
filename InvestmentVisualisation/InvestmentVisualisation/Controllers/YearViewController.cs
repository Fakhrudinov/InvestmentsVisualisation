using DataAbstraction.Interfaces;
using DataAbstraction.Models.BaseModels;
using DataAbstraction.Models.Settings;
using DataAbstraction.Models.YearView;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using UserInputService;

namespace InvestmentVisualisation.Controllers
{
    public class YearViewController : Controller
    {
        private readonly ILogger<YearViewController> _logger;
        private IMySqlYearViewRepository _repository;
        private int _minimumYear;
        private IMySqlSecVolumeRepository _secVolumeRepository;
		private InputHelper _helper;

		public YearViewController(
            ILogger<YearViewController> logger, 
            IMySqlYearViewRepository repository, 
            IOptions<PaginationSettings> paginationSettings,            
            IMySqlSecVolumeRepository secVolumeRepository,
			InputHelper helper)
        {
            _logger = logger;
            _repository = repository;
            _minimumYear = paginationSettings.Value.YearViewMinimumYear;
            _secVolumeRepository = secVolumeRepository;
            _helper = helper;
        }

		[Authorize]
		public async Task<IActionResult> Index(CancellationToken cancellationToken, int year = 0, bool sortedByVolume = false)
        {
            _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} YearViewController " +
                $"GET Index called, year={year}");

            int currentYear = DateTime.Now.Year;

            if (year >= _minimumYear && year < currentYear)
            {
                await _repository.RecalculateYearView(cancellationToken, year, sortedByVolume);
            }
            else
            {
                // add recalc money
                await _secVolumeRepository.RecalculateSecVolumeForYear(cancellationToken, currentYear);
                await _repository.RecalculateYearView(cancellationToken, currentYear, sortedByVolume);
                year = currentYear;
            }

            List<YearViewModel> yearViews = await _repository.GetYearViewPage(cancellationToken);

            List<int> objSt = new List<int>();
            for (int i = _minimumYear; i <= currentYear; i++)
            {
                objSt.Add(i);
            }
            ViewData["Navigation"] = objSt;
            ViewBag.year = year;
            ViewBag.SortedByVolume = sortedByVolume;

            return View(yearViews);
        }

		[Authorize]
		public async Task<IActionResult> Last12Month(CancellationToken cancellationToken)
        {
            _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} YearViewController Last12Month");

            // create view
            await _repository.CallFillViewShowLast12Month(cancellationToken);
            
            // get view
            List<YearViewModel> last12MonthView = await _repository.GetLast12MonthViewPage(cancellationToken);

            // fill summ
            for (int i = 0; i < last12MonthView.Count; i++)
            {
                decimal divSumm =
                    (last12MonthView[i].Jan is null ? 0 : (decimal)last12MonthView[i].Jan) +
                    (last12MonthView[i].Feb is null ? 0 : (decimal)last12MonthView[i].Feb) +
                    (last12MonthView[i].Mar is null ? 0 : (decimal)last12MonthView[i].Mar) +
                    (last12MonthView[i].Apr is null ? 0 : (decimal)last12MonthView[i].Apr) +
                    (last12MonthView[i].May is null ? 0 : (decimal)last12MonthView[i].May) +
                    (last12MonthView[i].Jun is null ? 0 : (decimal)last12MonthView[i].Jun) +
                    (last12MonthView[i].Jul is null ? 0 : (decimal)last12MonthView[i].Jul) +
                    (last12MonthView[i].Aug is null ? 0 : (decimal)last12MonthView[i].Aug) +
                    (last12MonthView[i].Sep is null ? 0 : (decimal)last12MonthView[i].Sep) +
                    (last12MonthView[i].Okt is null ? 0 : (decimal)last12MonthView[i].Okt) +
                    (last12MonthView[i].Nov is null ? 0 : (decimal)last12MonthView[i].Nov) +
                    (last12MonthView[i].Dec is null ? 0 : (decimal)last12MonthView[i].Dec);

                if (divSumm > 0)
                {
                    last12MonthView[i].Summ = divSumm;
                }
            }

            // fill % divs to volume
            for (int i = 0; i < last12MonthView.Count; i++)
            {
                if (last12MonthView[i].Summ is not null)
                {
                    decimal onePercent = last12MonthView[i].Volume / 100;
                    last12MonthView[i].DivPercent = (decimal)last12MonthView[i].Summ / onePercent;
                }
            }

            // DROP TABLE
            await _repository.DropTableLast12MonthView(cancellationToken);


            if (last12MonthView is not null && last12MonthView.Count > 0 && last12MonthView[0].Summ > 0)
            {
                // fill summ/12 = monthly average dividents
                ViewBag.MonthAverage = Math.Round((decimal)last12MonthView[0].Summ/12, 0);
            }


            return View(last12MonthView);
        }

		public async Task<IActionResult> Last12MonthBonds(CancellationToken cancellationToken)
		{
			_logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} YearViewController Last12MonthBonds");
			// get list of actual bonds + volume and pieces
			// get divs data for view
			// fill view
			// List<YearViewModel> last12MonthView 
			// fill % divs from pieces*1000, NOT from volume


			// get list of actual bonds + volume and pieces
			List<NameAndPiecesAndValueModel> ? bondList = await _repository.GetBondsWithNameAndValues(cancellationToken);
            if (bondList is null || bondList.Count == 0)
            {
                ViewData["Message"] = "Список облигаций не был получен.";
				return View();
			}

			// get divs data for view
			List<SecCodeAndDividentAndDateModel>? bondDivs = await _repository.GetBondsDividendsForLastYear(cancellationToken);
			if (bondDivs is null || bondDivs.Count == 0)
			{
				ViewData["Message"] = "Дивиденды облигаций не получены.";
				return View();
			}


            // fill view
            List<YearViewBondModel> last12MonthView = new List<YearViewBondModel>();
            
            foreach (NameAndPiecesAndValueModel bond in bondList)
            {
				YearViewBondModel newBond = new YearViewBondModel();
                newBond.Name = bond.Name;
				newBond.FullName = bond.FullName;
				newBond.Volume = bond.Volume;
                newBond.ISIN = bond.SecCode;
				newBond.SecCode = bond.SecCode;

				if (bond.PaysPerYear is not null && bond.PaysPerYear > 0)
				{
					newBond.PaymentCountInYear = (int)bond.PaysPerYear;
				}

				foreach (SecCodeAndDividentAndDateModel bondDiv in bondDivs)
				{
                    if (bondDiv.SecCode == bond.SecCode)
                    {
						int diff = (DateTime.Now.Year - bondDiv.EventDate.Year) 
                            * 12
                            + DateTime.Now.Month - bondDiv.EventDate.Month;
						int monthOfDiv = 12 - diff;

						decimal divValue = _helper.GetDecimalFromString(bondDiv.Divident);
						switch (monthOfDiv)
                        {
                            case 1:
								decimal ? isnull1 = newBond.Jan == null ? 0 : newBond.Jan;
								newBond.Jan = isnull1 + divValue;
								break;
                            case 2:
                                decimal? isnull2 = newBond.Feb == null ? 0 : newBond.Feb;
                                newBond.Feb = isnull2 + divValue;
                                break;
							case 3:
								decimal? isnull3 = newBond.Mar == null ? 0 : newBond.Mar;
								newBond.Mar = isnull3 + divValue;
								break;
							case 4:
								decimal? isnull4 = newBond.Apr == null ? 0 : newBond.Apr;
								newBond.Apr = isnull4 + divValue;
								break;
							case 5:
								decimal? isnull5 = newBond.May == null ? 0 : newBond.May;
								newBond.May = isnull5 + divValue;
								break;
							case 6:
                                decimal? isnull6 = newBond.Jun == null ? 0 : newBond.Jun;
                                newBond.Jun = isnull6 + divValue;
                                break;
							case 7:
								decimal? isnull7 = newBond.Jul == null ? 0 : newBond.Jul;
								newBond.Jul = isnull7 + divValue;
								break;
							case 8:
								decimal? isnull8 = newBond.Aug == null ? 0 : newBond.Aug;
								newBond.Aug = isnull8 + divValue;
								break;
							case 9:
								decimal? isnull9 = newBond.Sep == null ? 0 : newBond.Sep;
								newBond.Sep = isnull9 + divValue;
								break;
							case 10:
								decimal? isnull10 = newBond.Okt == null ? 0 : newBond.Jun;
								newBond.Okt = isnull10 + divValue;
								break;
							case 11:
								decimal? isnull11 = newBond.Nov == null ? 0 : newBond.Nov;
								newBond.Nov = isnull11 + divValue;
								break;
							case 12:
								decimal? isnull12 = newBond.Dec == null ? 0 : newBond.Dec;
								newBond.Dec = isnull12 + divValue;
								break;
						}
					}
                }

				last12MonthView.Add(newBond);
			}

			// fill summ and  actual %
			for (int i = 0; i < last12MonthView.Count; i++)
			{
				decimal divSumm =
					(last12MonthView[i].Jan is null ? 0 : (decimal)last12MonthView[i].Jan) +
					(last12MonthView[i].Feb is null ? 0 : (decimal)last12MonthView[i].Feb) +
					(last12MonthView[i].Mar is null ? 0 : (decimal)last12MonthView[i].Mar) +
					(last12MonthView[i].Apr is null ? 0 : (decimal)last12MonthView[i].Apr) +
					(last12MonthView[i].May is null ? 0 : (decimal)last12MonthView[i].May) +
					(last12MonthView[i].Jun is null ? 0 : (decimal)last12MonthView[i].Jun) +
					(last12MonthView[i].Jul is null ? 0 : (decimal)last12MonthView[i].Jul) +
					(last12MonthView[i].Aug is null ? 0 : (decimal)last12MonthView[i].Aug) +
					(last12MonthView[i].Sep is null ? 0 : (decimal)last12MonthView[i].Sep) +
					(last12MonthView[i].Okt is null ? 0 : (decimal)last12MonthView[i].Okt) +
					(last12MonthView[i].Nov is null ? 0 : (decimal)last12MonthView[i].Nov) +
					(last12MonthView[i].Dec is null ? 0 : (decimal)last12MonthView[i].Dec);

				if (divSumm > 0)
				{
					last12MonthView[i].Summ = divSumm;
				}

                last12MonthView[i].DivPercent = last12MonthView[i].Summ / (last12MonthView[i].Volume / 100);
			}


			// fill % divs from pieces*1000, NOT from volume
			decimal maxDivValue = 0;
			decimal minDivValue = 0;
			for (int i = 0; i < last12MonthView.Count; i++)
			{
                // get last value
                // ExpectedForYearDivSumm = calculate (last value)*PaymentCountInYear
                // ExpectedForYearDivPercent = calculate (last value)*PaymentCountInYear / 1%
                decimal? lastValue = null;

                while (lastValue is null)
                {
                    if (last12MonthView[i].Dec is not null)
                    {
						lastValue = last12MonthView[i].Dec;
						break;
                    }
                    else if (last12MonthView[i].Nov is not null)
                    {
						lastValue = last12MonthView[i].Nov;
                        break;
					}
					else if (last12MonthView[i].Okt is not null)
					{
						lastValue = last12MonthView[i].Okt;
						break;
					}
					else if (last12MonthView[i].Sep is not null)
					{
						lastValue = last12MonthView[i].Sep;
						break;
					}
					else if (last12MonthView[i].Aug is not null)
					{
						lastValue = last12MonthView[i].Aug;
						break;
					}
					else if (last12MonthView[i].Jul is not null)
					{
						lastValue = last12MonthView[i].Jul;
						break;
					}
					else if (last12MonthView[i].Jun is not null)
					{
						lastValue = last12MonthView[i].Jun;
						break;
					}
					else if (last12MonthView[i].May is not null)
					{
						lastValue = last12MonthView[i].May;
						break;
					}
					else if (last12MonthView[i].Apr is not null)
					{
						lastValue = last12MonthView[i].Apr;
						break;
					}
					else if (last12MonthView[i].Mar is not null)
					{
						lastValue = last12MonthView[i].Mar;
						break;
					}
					else if (last12MonthView[i].Feb is not null)
					{
						lastValue = last12MonthView[i].Feb;
						break;
					}
					else if (last12MonthView[i].Jan is not null)
					{
						lastValue = last12MonthView[i].Jan;
						break;
					}

					break;
				}

				if (lastValue is not null)
				{
					last12MonthView[i].ExpectedForYearDivSumm = (decimal)lastValue * last12MonthView[i].PaymentCountInYear;
					last12MonthView[i].ExpectedForYearDivPercent = ((decimal)lastValue * last12MonthView[i].PaymentCountInYear) 
						/ (last12MonthView[i].Volume / 100);
				}
			}


			// sort
			IEnumerable<YearViewBondModel> sorted = last12MonthView.OrderByDescending(x => x.ExpectedForYearDivPercent);

			return View(sorted);
		}
	}
}
