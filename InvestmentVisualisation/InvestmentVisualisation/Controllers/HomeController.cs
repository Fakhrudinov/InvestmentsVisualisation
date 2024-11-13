using DataAbstraction.Interfaces;
using DataAbstraction.Models;
using Microsoft.AspNetCore.Mvc;

namespace InvestmentVisualisation.Controllers
{
    public class HomeController : Controller
    {
        private IMySqlCommonRepository _commonRepo;
        public HomeController(IMySqlCommonRepository commonRepo)
        {
            _commonRepo = commonRepo;

            if (StaticData.FreeMoney is null)
            {
                _commonRepo.FillFreeMoney();
            }
        }

        public IActionResult Index()
        {
            return View();
        }
    }
}
