using DataAbstraction.Interfaces;
using DataAbstraction.Models.Settings;
using DataAbstraction.Models.YearView;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace InvestmentVisualisation.Controllers
{
    public class YearViewController : Controller
    {
        private readonly ILogger<YearViewController> _logger;
        private IMySqlYearViewRepository _repository;
        private int _minimumYear;
        private IMySqlSecVolumeRepository _secVolumeRepository;

        public YearViewController(
            ILogger<YearViewController> logger, 
            IMySqlYearViewRepository repository, 
            IOptions<PaginationSettings> paginationSettings,            
            IMySqlSecVolumeRepository secVolumeRepository)
        {
            _logger = logger;
            _repository = repository;
            _minimumYear = paginationSettings.Value.YearViewMinimumYear;
            _secVolumeRepository = secVolumeRepository;
        }

        public async Task<IActionResult> Index(CancellationToken cancellationToken, int year = 0, bool sortedByVolume = false)
        {
            _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} YearViewController GET Index called, year={year}");

            int currentYear = DateTime.Now.Year;

            if (year >= _minimumYear && year < currentYear)
            {
                await _repository.RecalculateYearView(year, sortedByVolume);
            }
            else
            {
                // add recalc money ???????? обычно деньги я сам считаю, чтобы убедиться что всё совпадает
                await _secVolumeRepository.RecalculateSecVolumeForYear(cancellationToken, currentYear);
                await _repository.RecalculateYearView(currentYear, sortedByVolume);
                year = currentYear;
            }

            List<YearViewModel> yearViews = await _repository.GetYearViewPage();

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

        public async Task<IActionResult> Last12Month()
        {
            _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} YearViewController Last12Month");

            // create view
            await _repository.CallFillViewShowLast12Month();
            
            // get view
            List<YearViewModel> last12MonthView = await _repository.GetLast12MonthViewPage();

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
            await _repository.DropTableLast12MonthView();


            return View(last12MonthView);
        }        
    }
}
