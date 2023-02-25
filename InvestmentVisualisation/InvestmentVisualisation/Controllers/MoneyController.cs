using DataAbstraction.Models.Settings;
using DataAbstraction.Interfaces;
using Microsoft.AspNetCore.Mvc;
using DataAbstraction.Models.MoneyByMonth;
using Microsoft.Extensions.Options;
using DataAbstraction.Models;
using DataAbstraction.Models.BaseModels;
using System.Globalization;

namespace InvestmentVisualisation.Controllers
{
    public class MoneyController : Controller
    {
        private readonly ILogger<MoneyController> _logger;
        private IMySqlMoneyRepository _repository;
        private int _minimumYear;

        public MoneyController(ILogger<MoneyController> logger, IMySqlMoneyRepository repository, IOptions<PaginationSettings> paginationSettings)
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


        public async Task<IActionResult> Index(int year = 0)
        {
            _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} DealsController GET Index called, year={year}");

            int currentYear = DateTime.Now.Year;

            List<MoneyModel> moneyList = new List<MoneyModel>();
            if (year == 0)
            {
                moneyList = await _repository.GetMoneyLastYearPage();
            }
            else
            {
                moneyList = await _repository.GetMoneyYearPage(year);
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
        public async Task<IActionResult> Recalculate(int year = 0, int month = 0)
        {
            _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} DealsController GET Recalculate called, " +
                $"year={year} month={month}");

            if (year >= _minimumYear && (month >= 1 && month <= 12))
            {
                await _repository.RecalculateMoney($"{year}-{month}-01");
            }

            // если пересчет за последний месяц - обновить значение в layout.
            if (year == DateTime.Now.Year && month == DateTime.Now.Month)
            {
                _repository.FillFreeMoney();
            }

            return RedirectToAction("Index");
        }        
    }
}
