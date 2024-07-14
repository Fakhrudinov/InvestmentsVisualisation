using DataAbstraction.Interfaces;
using DataAbstraction.Models;
using DataAbstraction.Models.Settings;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using DataAbstraction.Models.SecVolume;
using DataAbstraction.Models.BaseModels;
using Newtonsoft.Json;
using DataAbstraction.Models.WishList;
using UserInputService;

namespace InvestmentVisualisation.Controllers
{
    public class SecVolumeController : Controller
    {
        private readonly ILogger<SecVolumeController> _logger;
        private IMySqlSecVolumeRepository _repository;
        private int _itemsAtPage;
        private int _minimumYear;
        private IWebDividents _webRepository;
        private IMySqlWishListRepository _wishListRepository;
        private InputHelper _helper;

        private enum WebSites
        {
            SmartLab = 0,
            Dohod = 2,
            Vsdelke = 3
        }

        public SecVolumeController(
            ILogger<SecVolumeController> logger,
            IMySqlSecVolumeRepository repository,
            IOptions<PaginationSettings> paginationSettings,
            IWebDividents webRepository,
            IMySqlWishListRepository wishListRepository,
            InputHelper helper)
        {
            _logger = logger;
            _repository = repository;
            _itemsAtPage = paginationSettings.Value.PageItemsCount;
            _minimumYear = paginationSettings.Value.SecVolumeMinimumYear;
            _webRepository = webRepository;
            _wishListRepository = wishListRepository;
            _helper = helper;
        }

        public async Task<IActionResult> Index(CancellationToken cancellationToken, int page = 1, int year = 0)
        {
            _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} SecVolumeController " +
                $"GET Index called, page={page} year={year}");

            if (year == 0 || year < _minimumYear || year > DateTime.Now.Year)
            {
                year = DateTime.Now.Year;
            }

            int count = await _repository.GetSecVolumeCountForYear(cancellationToken, year);

            _logger.LogDebug($"{DateTime.Now.ToString("HH:mm:ss:fffff")} SecVolumeController " +
                $"SecVolumes table for year={year} size={count}");

            SecVolumesWithPaginations secVolumesWithPaginations = new SecVolumesWithPaginations();

            secVolumesWithPaginations.PageViewModel = new PaginationPageViewModel(
                count,
                page,
                _itemsAtPage);

            secVolumesWithPaginations.SecVolumes = await _repository
                .GetSecVolumePageForYear(cancellationToken, _itemsAtPage, (page - 1) * _itemsAtPage, year);
            ViewBag.year = year;

            List<int> objSt = new List<int>();
            int currentYear = DateTime.Now.Year;
            for (int i = _minimumYear; i <= currentYear; i++)
            {
                objSt.Add(i);
            }
            ViewData["Navigation"] = objSt;

            return View(secVolumesWithPaginations);
        }

        public async Task<IActionResult> Recalculate(CancellationToken cancellationToken, int year = 0)
        {
            _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} SecVolumeController GET " +
                $"Recalculate called, year={year}");

            if (year >= _minimumYear && year <= DateTime.Now.Year)
            {
                await _repository.RecalculateSecVolumeForYear(cancellationToken, year);
            }
            else
            {
                year = DateTime.Now.Year; //для корректного RedirectToAction 
            }

            return RedirectToAction("Index", new { year = year });
        }

        public async Task<IActionResult> SecVolumeLast3YearsDynamic(
            CancellationToken cancellationToken, 
            string sortMode = "byTiker")
        {
            _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} SecVolumeController GET " +
                $"SecVolumeLast3YearsDynamic called, sortMode={sortMode}");

            ViewBag.SortMode = sortMode;
            ViewBag.year = DateTime.Now.Year;

            List<SecVolumeLast2YearsDynamicModel> model = new List<SecVolumeLast2YearsDynamicModel>();

            if (sortMode.Equals("byVolume"))
            {
                model = await _repository.GetSecVolumeLast3YearsDynamicSortedByVolume(cancellationToken, DateTime.Now.Year);
            }
            else
            {
                model = await _repository.GetSecVolumeLast3YearsDynamic(cancellationToken, DateTime.Now.Year);
            }

            CalculateChangesPercentsForList(model);

            // get wish List<WishListItemModel> 
            var wishValues = await _wishListRepository.GetFullWishList(cancellationToken);
            ViewBag.WishList = wishValues;

            // get web site data
            DohodDivsAndDatesModel? dohodDivs = _webRepository.GetDividentsTableFromDohod(cancellationToken);
            if (dohodDivs is not null && dohodDivs.DohodDivs.Count > 0)
            {
                SetDividentsToModel(dohodDivs.DohodDivs, model, WebSites.Dohod);
                SetDividentDatesToModel(dohodDivs.DohodDates, model);
                ViewBag.DohodDivs = true;
            }

            List<SecCodeAndDividentModel>? smartLabDivs = _webRepository.GetDividentsTableFromSmartLab(cancellationToken);
            SetDividentsToModel(smartLabDivs, model, WebSites.SmartLab);
            if (smartLabDivs is not null && smartLabDivs.Count > 0)
            {
                ViewBag.SmartLab = true;
            }

            List<SecCodeAndDividentModel>? vsdelkeDivs = _webRepository.GetDividentsTableFromVsdelke(cancellationToken);
            SetDividentsToModel(vsdelkeDivs, model, WebSites.Vsdelke);
            if (vsdelkeDivs is not null && vsdelkeDivs.Count > 0)
            {
                ViewBag.Vsdelke = true;
            }

            // clean model from (not mine) +-0 div pozitions
            RemoveEmptyPositionWithLowDivs(model);

            if (sortMode.Equals("byTime"))
            {
                // move empty DateTime items in temp list
                List<SecVolumeLast2YearsDynamicModel> temporary = new List<SecVolumeLast2YearsDynamicModel>();
                for (int i = model.Count - 1; i >= 0; i--)
                {
                    if (model[i].NextDivDate == DateTime.MinValue)
                    {
                        temporary.Add(model[i]);
                        model.RemoveAt(i);
                    }
                }
                // do sort by SecCode
                temporary = temporary.OrderBy(e => e.SecCode).ToList();

                // do sort by DateTime
                model = model.OrderBy(e => e.NextDivDate).ToList();

                model.AddRange(temporary);
            }

            CalculateColorDependingByDivsValues(model, wishValues);

            return View(model);
        }

        public async Task<IActionResult> VolumeChart(CancellationToken cancellationToken)
        {
			// get dat from DB
			// (summ all volumes) / 100 = 1%
			// calculate % in each position
			// calculate rows number - for chart height calculation
			// summ AO and AP
			// sort data

			List<ChartItemModel> chartItemsRaw = await _repository.GetVolumeChartData(cancellationToken);

            // calculate 1%
            decimal totalVolume = 0;
            foreach (ChartItemModel chartItem in chartItemsRaw)
            {
                totalVolume = totalVolume + chartItem.Y;
			}
            decimal onePercent = totalVolume/ 100;

            // any AP items save to separare list
			List<ChartItemModel> chartItems = new List<ChartItemModel>();
			List<ChartItemModel> chartItemsTemporaryForAP = new List<ChartItemModel>();
			foreach (ChartItemModel chartItem in chartItemsRaw)
			{
                if (chartItem.Label.Length == 5 && chartItem.Label.EndsWith("P")) // for AP list
                {
					chartItemsTemporaryForAP.Add
	                    (new ChartItemModel
		                    (
								chartItem.Label,
			                    Math.Round(chartItem.Y / onePercent, 2)
		                    )
	                    );
				}
                else // for AO list
                {
					chartItems.Add
	                    (new ChartItemModel
		                    (
								//chartItem.Label,
                                chartItem.Label.Length > 10 ? chartItem.Label.Substring(0,10) : chartItem.Label,
								Math.Round(chartItem.Y / onePercent, 2)
		                    )
	                    );
				}
            }

			// add AP items to AO list summing with exist AO values
            foreach (ChartItemModel chartItemAP in chartItemsTemporaryForAP)
            {
                bool isExist = false;

				foreach (ChartItemModel chartItem in chartItems)
                {
                    if (chartItem.Label.Length == 4 && chartItemAP.Label.Substring(0,4).Equals(chartItem.Label))
                    {
						// summ with exist position in AO list
						chartItem.Label = chartItem.Label + "+" + chartItemAP.Label;
                        chartItem.Y = chartItem.Y + chartItemAP.Y;

						isExist = true;
						break;
                    }
                }

                // add new position to AO list
				if (!isExist)
				{
					chartItems.Add
						(new ChartItemModel
							(
								chartItemAP.Label,
								chartItemAP.Y
							)
						);
				}
			}

			// sort
			chartItems = chartItems.OrderBy( chartItem => chartItem.Y ).ToList();

			ViewBag.ChartData = JsonConvert.SerializeObject(chartItems);
			ViewBag.ChartItemsCount = chartItems.Count;
            ViewBag.MaximumOnChart = Decimal.ToInt16(chartItems[chartItems.Count - 1].Y) + 1;

            return View();
		}

        private void CalculateColorDependingByDivsValues(
            List<SecVolumeLast2YearsDynamicModel> model,
            List<WishListItemModel> wishValues)
        {
            /// coloring plan
            /// get wish value as is
            ///     5 -------max
            ///     min value = -5 ------------ min
            /// get past value
            ///     from 0 to 4 = 0
            ///     from 5 to 6 = 1
            ///     from 6 to 8 = 2
            ///     from 9 = 3 --------max
            /// get future value from Dohod, 
            ///     from 0 to 3 = 0
            ///         4 = 1
            ///         5=2
            ///         6=3
            ///         7=4
            ///         8=5
            ///         9=6
            ///         10+ = 7 -------max
            /// summ: 5 + 3 + 7 = 15 ---------total max
            ///     if wish value below zero = use only wish!
            /// min value = -5 ---------------total min

            foreach (SecVolumeLast2YearsDynamicModel item in model)
            {
                int wishValue = 0;

                if (wishValues is not null)
                {
                    WishListItemModel? wish = wishValues.Find(x => x.SecCode.Equals(item.SecCode));
                    if (wish is not null)
                    {
                        wishValue = wish.Level;
                    }
                }
                if (wishValue < 0)
                {
                    item.LineColor = GetColorByValue(wishValue);
                    continue;
                }

                //int summ = 0;
                int pastValue = 0;

                if (item.SmartLabDividents is not null)
                {
                    if (_helper.IsInt32Correct(item.SmartLabDividents))
                    {
                        /// get past value
                        ///     from 0 to 4 = 0
                        ///     from 5 to 6 = 1
                        ///     from 6 to 8 = 2
                        ///     from 9 = 3 --------max

                        int rawPastValue = _helper.GetInt32FromString(item.SmartLabDividents);
                        if (rawPastValue > 0) // -1 returned in case of parsing error. And zero we dont calculate
                        {
                            switch (rawPastValue)
                            {
                                case >= 9:
                                    pastValue = 3;
                                    break;
                                case >=6:
                                    pastValue = 2;
                                    break;
                                case >=5:
                                    pastValue = 1;
                                    break;
                                    //case >=1:
                                    //    pastValue = 0;
                                    //    break;
                            }
                        }
                    }
                }

                int futureValue = 0;
                if (item.DohodDividents is not null)
                {
                    /// get future value from Dohod, 
                    ///     from 0 to 3 = 0
                    ///         4 = 1
                    ///         5=2
                    ///         6=3
                    ///         7=4
                    ///         8=5
                    ///         9=6
                    ///         10+ = 7 -------max
                    if (_helper.IsInt32Correct(item.DohodDividents))
                    {
                        int rawfutureValue = _helper.GetInt32FromString(item.DohodDividents);
                        if (rawfutureValue > 0) // -1 returned in case of parsing error. And zero we dont calculate
                        {
                            switch (rawfutureValue)
                            {
                                case >= 10:
                                    futureValue = 7;
                                    break;
                                case >=9:
                                    futureValue = 6;
                                    break;
                                case >=8:
                                    futureValue = 5;
                                    break;
                                case >=7:
                                    futureValue = 4;
                                    break;
                                case >=6:
                                    futureValue = 3;
                                    break;
                                case >=5:
                                    futureValue = 2;
                                    break;
                                case >=4:
                                    futureValue = 1;
                                    break;
                            }
                        }
                    }
                }

                item.LineColor = GetColorByValue(wishValue + pastValue + futureValue);
            }
        }

        private string GetColorByValue(int wishValue)
        {
            string color = "white; color:black;";

            switch (wishValue)
            {
                case -5:
                    color = "#db0000;color:white;";
                    break;
                case -4:
                    color = "#e23333;color:white;";
                    break;
                case -3:
                    color = "#e96666;color:white;";
                    break;
                case -2:
                    color = "#f19999; color:black;";
                    break;
                case -1:
                    color = "#f8cccc; color:black;";
                    break;

                case 0:
                    break;

                case 1:
                    color = "#eef4ee; color:black;";
                    break;
                case 2:
                    color = "#dde9de; color:black;";
                    break;
                case 3:
                    color = "#ccddcd; color:black;";
                    break;
                case 4:
                    color = "#bbd2bd; color:black;";
                    break;
                case 5:
                    color = "#aac7ac; color:black;";
                    break;
                case 6:
                    color = "#99bc9b; color:black;";
                    break;
                case 7:
                    color = "#88b18b; color:black;";
                    break;
                case 8:
                    color = "#77a57a; color:white;";
                    break;
                case 9:
                    color = "#669a6a; color:white;";
                    break;
                case 10:
                    color = "#558f59; color:white;";
                    break;
                case 11:
                    color = "#448448; color:white;";
                    break;
                case 12:
                    color = "#337938; color:white;";
                    break;
                case 13:
                    color = "#226d27; color:white;";
                    break;
                case 14:
                    color = "#116217; color:white;";
                    break;
                case >= 15:
                    color = "#005706; color:white;";
                    break;
            }

            return color;
        }

        private void RemoveEmptyPositionWithLowDivs(List<SecVolumeLast2YearsDynamicModel> model)
        {
            for (int i = model.Count -1; i>0; i--)
            {
                //var item = model[i];
                //Console.WriteLine(model[i].SecCode);

                // exit point - model item has values
                if (model[i].Name is not null)
                {
                    break;
                }

                //check items divs. if any divs > 5 then skip. else remove
                bool smartLabDividents = IsItBiggerThenFive(model[i].SmartLabDividents);
                bool dohodDividents = IsItBiggerThenFive(model[i].DohodDividents);
                bool vsdelkeDividents = IsItBiggerThenFive(model[i].VsdelkeDividents);

                if (!smartLabDividents && !dohodDividents && !vsdelkeDividents)
                {
                    model.RemoveAt(i);
                }
            }
        }

        private bool IsItBiggerThenFive(string? dividentString)
        {
            if (dividentString is null)
            {
                return false;
            }
            int divident;
            int.TryParse(dividentString.Split(',').First(), out divident);

            if(divident < 5)
            {
                return false;
            }

            return true;
        }

        private void SetDividentDatesToModel(
            List<SecCodeAndTimeToCutOffModel>? dohodDates, 
            List<SecVolumeLast2YearsDynamicModel> model)
        {
            foreach (SecCodeAndTimeToCutOffModel smDivDate in dohodDates)
            {
                //if (smDivDate.SecCode.Contains("NLMK"))
                //{
                //    Console.WriteLine();
                //}

                int index = model.FindIndex(sv => sv.SecCode == smDivDate.SecCode);
                if (index >= 0)
                {
                    model[index].NextDivDate = smDivDate.Date;
                }
                else
                {
                    // добавим в модель недостающий seccode
                    SecVolumeLast2YearsDynamicModel newModel = new SecVolumeLast2YearsDynamicModel 
                    { 
                        SecCode = smDivDate.SecCode 
                    };
                    newModel.NextDivDate = smDivDate.Date;

                    model.Add(newModel);
                }
            }
        }

        private void SetDividentsToModel(
            List<SecCodeAndDividentModel> ? dividents, 
            List<SecVolumeLast2YearsDynamicModel> model, 
            WebSites webSite)
        {
            if (dividents is not null)
            {
                foreach (SecCodeAndDividentModel smDiv in dividents)
                {
                    //if (smDiv.SecCode.Contains("AKRN"))
                    //{
                    //    Console.WriteLine();
                    //}

                    int index = model.FindIndex(sv => sv.SecCode == smDiv.SecCode);
                    if (index >= 0)
                    {
                        if (webSite == WebSites.SmartLab)
                        {
                            model[index].SmartLabDividents = smDiv.Divident;
                        }
                        else if (webSite == WebSites.Dohod)
                        {
                            model[index].DohodDividents = smDiv.Divident;
                        }
                        else if (webSite == WebSites.Vsdelke)
                        {
                            model[index].VsdelkeDividents = smDiv.Divident;
                        }
                    }
                    else
                    {
                        // добавим в модель недостающий seccode
                        SecVolumeLast2YearsDynamicModel newModel = new SecVolumeLast2YearsDynamicModel 
                        { 
                            SecCode = smDiv.SecCode 
                        };
                        if (webSite == WebSites.SmartLab)
                        {
                            newModel.SmartLabDividents = smDiv.Divident;
                        }
                        else if(webSite == WebSites.Dohod)
                        {
                            newModel.DohodDividents = smDiv.Divident;
                        }
                        else if (webSite == WebSites.Vsdelke)
                        {
                            newModel.VsdelkeDividents = smDiv.Divident;
                        }

                        model.Add(newModel);
                    }
                }
            }
        }

        private void CalculateChangesPercentsForList(List<SecVolumeLast2YearsDynamicModel> model)
        {
            foreach (SecVolumeLast2YearsDynamicModel record in model)
            {
                if (record.PreviousYearPieces is null)
                {
                    record.LastYearChanges = 100;
                }

                if(record.PreviousYearPieces is not null)
                {
                    record.LastYearChanges = CalcPercent((int)record.LastYearPieces, (int)record.PreviousYearPieces);
                }
            }
        }

        private decimal CalcPercent(int last, int previous)
        {
            int differ = last - previous;
            decimal change = (decimal)((decimal)differ/previous);
            return change * 100;
        }
    }
}
