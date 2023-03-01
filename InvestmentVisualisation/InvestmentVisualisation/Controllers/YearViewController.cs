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

        public async Task<IActionResult> Index(int year = 0)
        {
            _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} YearViewController GET Deals called, year={year}");

            int currentYear = DateTime.Now.Year;

            if (year >= _minimumYear && year < currentYear)
            {
                await _repository.RecalculateYearView(year);
            }
            else
            {
                // add recalc money ???????? обычно деньги я сам считаю, чтобы убедиться что всё совпадает
                await _secVolumeRepository.RecalculateSecVolumeForYear(currentYear);
                await _repository.RecalculateYearView(currentYear);
                year = currentYear;
            }


            //if (year == 0)
            //{
            //    // add recalc money ???????? обычно деньги я сам считаю, чтобы убедиться что всё совпадает
            //    await _secVolumeRepository.RecalculateSecVolumeForYear(currentYear);
            //    await _repository.RecalculateYearView(currentYear);
            //    year = currentYear;
            //}
            //else
            //{
            //    if (year >= _minimumYear && year <= currentYear)
            //    {
            //        await _repository.RecalculateYearView(year);
            //    }
            //    else
            //    {
            //        // add recalc money ???????? обычно деньги я сам считаю, чтобы убедиться что всё совпадает
            //        await _secVolumeRepository.RecalculateSecVolumeForYear(currentYear);
            //        await _repository.RecalculateYearView(currentYear);
            //        year = currentYear;
            //    }
            //}

            List<YearViewModel> yearViews = await _repository.GetYearViewPage();

            List<int> objSt = new List<int>();
            for (int i = _minimumYear; i <= currentYear; i++)
            {
                objSt.Add(i);
            }
            ViewData["Navigation"] = objSt;
            ViewBag.year = year;

            return View(yearViews);
        }
    }
}
