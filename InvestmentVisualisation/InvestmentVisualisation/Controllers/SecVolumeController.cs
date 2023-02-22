using DataAbstraction.Interfaces;
using DataAbstraction.Models;
using DataAbstraction.Models.Settings;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using DataAbstraction.Models.SecVolume;
using DataAbstraction.Models.MoneyByMonth;

namespace InvestmentVisualisation.Controllers
{
    public class SecVolumeController : Controller
    {
        private readonly ILogger<SecVolumeController> _logger;
        private IMySqlSecVolumeRepository _repository;
        private int _itemsAtPage;
        private int _minimumYear;

        public SecVolumeController(
            ILogger<SecVolumeController> logger, 
            IMySqlSecVolumeRepository repository, 
            IOptions<PaginationSettings> paginationSettings)
        {
            _logger = logger;
            _repository = repository;
            _itemsAtPage = paginationSettings.Value.PageItemsCount;
            _minimumYear = paginationSettings.Value.SecVolumeMinimumYear;
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
            secVolumesWithPaginations.ShownYear = year;

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

            return RedirectToAction("Index?year=" + year);
        }
    }
}
