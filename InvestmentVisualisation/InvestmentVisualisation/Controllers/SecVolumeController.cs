using DataAbstraction.Interfaces;
using DataAbstraction.Models;
using DataAbstraction.Models.Settings;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using DataAbstraction.Models.SecVolume;
using DataAbstraction.Models.BaseModels;

namespace InvestmentVisualisation.Controllers
{
    public class SecVolumeController : Controller
    {
        private readonly ILogger<SecVolumeController> _logger;
        private IMySqlSecVolumeRepository _repository;
        private int _itemsAtPage;
        private int _minimumYear;
        private IWebDividents _webRepository;

        private enum WebSites
        {
            SmartLab = 0,
            InvLab = 1,
            Dohod = 2,
            Vsdelke = 3
        }

        public SecVolumeController(
            ILogger<SecVolumeController> logger, 
            IMySqlSecVolumeRepository repository, 
            IOptions<PaginationSettings> paginationSettings,
            IWebDividents webRepository)
        {
            _logger = logger;
            _repository = repository;
            _itemsAtPage = paginationSettings.Value.PageItemsCount;
            _minimumYear = paginationSettings.Value.SecVolumeMinimumYear;
            _webRepository = webRepository;
        }

        public async Task<IActionResult> Index(int page = 1, int year = 0)
        {
            _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} SecVolumeController GET Index called, page={page} year={year}");

            if (year == 0 || year < _minimumYear || year > DateTime.Now.Year)
            {
                year = DateTime.Now.Year;
            }
            
            int count = await _repository.GetSecVolumeCountForYear(year);

            _logger.LogDebug($"{DateTime.Now.ToString("HH:mm:ss:fffff")} SecVolumeController SecVolumes table for year={year} size={count}");

            SecVolumesWithPaginations secVolumesWithPaginations = new SecVolumesWithPaginations();

            secVolumesWithPaginations.PageViewModel = new PaginationPageViewModel(
                count,
                page,
                _itemsAtPage);

            secVolumesWithPaginations.SecVolumes = await _repository.GetSecVolumePageForYear(_itemsAtPage, (page - 1) * _itemsAtPage, year);
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

        public async Task<IActionResult> Recalculate(int year = 0)
        {
            _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} SecVolumeController GET Recalculate called, " +
                $"year={year}");

            if (year >= _minimumYear && year <= DateTime.Now.Year)
            {
                await _repository.RecalculateSecVolumeForYear(year);
            }
            else
            {
                year = DateTime.Now.Year; //для корректного RedirectToAction 
            }

            return RedirectToAction("Index", new { year = year });
        }

        public async Task<IActionResult> SecVolumeLast3YearsDynamic()
        {
            _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} SecVolumeController GET SecVolumeLast3YearsDynamic called");

            List<SecVolumeLast2YearsDynamicModel> model = await _repository.GetSecVolumeLast3YearsDynamic(DateTime.Now.Year);

            CalculateChangesPercentsForList(model);

            DohodDivsAndDatesModel? dohodDivs = _webRepository.GetDividentsTableFromDohod();            
            if (dohodDivs is not null && dohodDivs.DohodDivs.Count > 0)
            {
                SetDividentsToModel(dohodDivs.DohodDivs, model, WebSites.Dohod);
                SetDividentDatesToModel(dohodDivs.DohodDates, model);
                ViewBag.DohodDivs = true;
            }

            List<SecCodeAndDividentModel>? invLabDivs = _webRepository.GetDividentsTableFromInvLab();
            SetDividentsToModel(invLabDivs, model, WebSites.InvLab);
            if (invLabDivs is not null && invLabDivs.Count > 0)
            {
                ViewBag.InvLabDivs = true;
            }

            List<SecCodeAndDividentModel>? smartLabDivs = _webRepository.GetDividentsTableFromSmartLab();
            SetDividentsToModel(smartLabDivs, model, WebSites.SmartLab);
            if (smartLabDivs is not null && smartLabDivs.Count > 0)
            {
                ViewBag.SmartLab = true;
            }

            List<SecCodeAndDividentModel>? vsdelkeDivs = _webRepository.GetDividentsTableFromVsdelke();
            SetDividentsToModel(vsdelkeDivs, model, WebSites.Vsdelke);
            if (vsdelkeDivs is not null && vsdelkeDivs.Count > 0)
            {
                ViewBag.Vsdelke = true;
            }

            // clean model from (not mine) +-0 div pozitions
            RemoveEmptyPositionWithLowDivs(model);

            ViewBag.year = DateTime.Now.Year;

            return View(model);
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
                bool invLabDividents = IsItBiggerThenFive(model[i].InvLabDividents);

                if (!smartLabDividents && !dohodDividents && !vsdelkeDividents && !invLabDividents)
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

        private void SetDividentDatesToModel(List<SecCodeAndTimeToCutOffModel>? dohodDates, List<SecVolumeLast2YearsDynamicModel> model)
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
                    SecVolumeLast2YearsDynamicModel newModel = new SecVolumeLast2YearsDynamicModel { SecCode = smDivDate.SecCode };
                    newModel.NextDivDate = smDivDate.Date;

                    model.Add(newModel);
                }
            }
        }

        private void SetDividentsToModel(List<SecCodeAndDividentModel> ? dividents, List<SecVolumeLast2YearsDynamicModel> model, WebSites webSite)
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
                        else if (webSite == WebSites.InvLab)
                        {
                            model[index].InvLabDividents = smDiv.Divident;
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
                        SecVolumeLast2YearsDynamicModel newModel = new SecVolumeLast2YearsDynamicModel { SecCode = smDiv.SecCode };
                        if (webSite == WebSites.SmartLab)
                        {
                            newModel.SmartLabDividents = smDiv.Divident;
                        }
                        else if(webSite == WebSites.InvLab)
                        {
                            newModel.InvLabDividents = smDiv.Divident;
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
                    record.LastYearChanges = CalcPercent(record.LastYearPieces, (int)record.PreviousYearPieces);
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
