﻿using DataAbstraction.Interfaces;
using DataAbstraction.Models;
using DataAbstraction.Models.Incoming;
using DataAbstraction.Models.Settings;
using InvestmentVisualisation.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Diagnostics;

namespace InvestmentVisualisation.Controllers
{
    public class IncomingController : Controller
    {
        private readonly ILogger<IncomingController> _logger;
        private IMySqlIncomingRepository _repository;
        private int _itemsAtPage;

        public IncomingController(ILogger<IncomingController> logger, IMySqlIncomingRepository repository, IOptions<PaginationSettings> paginationSettings)
        {
            _logger = logger;
            _repository = repository;
            _itemsAtPage = paginationSettings.Value.PageItemsCount;
        }

        public async Task<IActionResult> Incoming(int page = 1)
        {
            _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} IncomingController GET Incoming called, page={page}");
            //https://localhost:7226/Incoming/Incoming?page=3

            int count = await _repository.GetIncomingCount();
            _logger.LogDebug($"{DateTime.Now.ToString("HH:mm:ss:fffff")} IncomingController Incoming table size={count}");

            IncomingWithPaginations incomingWithPaginations = new IncomingWithPaginations();

            incomingWithPaginations.PageViewModel = new PaginationPageViewModel(
                count, 
                page,
                _itemsAtPage);
            
            incomingWithPaginations.Incomings = await _repository.GetPageFromIncoming(_itemsAtPage, (page - 1) * _itemsAtPage);

            return View(incomingWithPaginations);
        }

        public IActionResult CreateIncoming()
        {
            _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} IncomingController GET CreateIncoming called");

            CreateIncomingModel model = new CreateIncomingModel();
            model.Date = DateTime.Now.AddDays( -1 );// обычно вношу за вчера
            model.Category = 1;

            return View(model);
        }
        public IActionResult CreateSpecificIncoming(DateTime data, string tiker, int category)
        {
            _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} IncomingController GET CreateIncoming called " +
                $"with parameters {data} {tiker} category={category}");

            CreateIncomingModel model = new CreateIncomingModel();
            model.Date = data;// обычно вношу за вчера
            model.SecCode = tiker;
            model.Category = category;

            return View("CreateIncoming", model);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateIncoming(CreateIncomingModel newIncoming)
        {
            _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} IncomingController POST CreateIncoming called");

            if (!newIncoming.SecCode.Equals("0") && newIncoming.Category == 0)
            {
                _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} IncomingController CreateIncoming error " +
                    $"- 'Зачисление денег' невозможно для secCode {newIncoming.SecCode}");

                ViewData["Message"] = $"'Зачисление денег' невозможно для {newIncoming.SecCode}. Используй Дивиденты или досрочное погашение";

                return View();
            }

            if (!newIncoming.SecCode.Equals("0") && newIncoming.Category == 3)
            {
                _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} IncomingController CreateIncoming error " +
                    $"- 'Комиссия брокера' невозможна для secCode {newIncoming.SecCode}");

                ViewData["Message"] = $"'Комиссия брокера' невозможна для {newIncoming.SecCode}";

                return View();
            }

            if (newIncoming.SecCode.Equals("0") && newIncoming.Category == 1)
            {
                _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} IncomingController CreateIncoming error " +
                    $"- 'Дивиденды' недопустимы для денег");

                ViewData["Message"] = $"'Дивиденды' недопустимы для денег. Для начисления денег от оброкера на ваши деньги - используйте досрочное погашение.";

                return View();
            }

            newIncoming.SecBoard = StaticData.SecCodes[StaticData.SecCodes.FindIndex(x => x.SecCode == newIncoming.SecCode)].SecBoard;

            newIncoming.Value = newIncoming.Value.Replace(',', '.');
            if (newIncoming.Comission is not null)
            {
                newIncoming.Comission = newIncoming.Comission.Replace(',', '.');
            }

            _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} IncomingController POST CreateIncoming validation complete, " +
                $"try create at repository");

            string result = await _repository.CreateNewIncoming(newIncoming);
            if (!result.Equals("1"))
            {
                ViewData["Message"] = $"Добавление не удалось. \r\n{result}";
                return View();
            }

            return RedirectToAction("Incoming");
        }

        public async Task<IActionResult> Edit(int id)
        {
            _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} IncomingController GET Edit id={id} called");

            IncomingModel editedItem = await _repository.GetSingleIncomingById(id);

            return View(editedItem);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(IncomingModel newIncoming)
        {
            _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} IncomingController HttpPost Edit called");

            newIncoming.Value = newIncoming.Value.Replace(',', '.');
            if (newIncoming.Comission is not null)
            {
                newIncoming.Comission = newIncoming.Comission.Replace(',', '.');
            }

            string result = await _repository.EditSingleIncoming(newIncoming);
            if (!result.Equals("1"))
            {
                ViewData["Message"] = $"Редактирование не удалось.\r\n{result}";
                return View(newIncoming);
            }

            return RedirectToAction("Incoming");
        }
        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} IncomingController GET Delete id={id} called");

            IncomingModel deleteItem = await _repository.GetSingleIncomingById(id);

            ViewData["SecBoard"] = @StaticData.SecBoards[StaticData.SecBoards.FindIndex(secb => secb.Id == deleteItem.SecBoard)].Name;
            ViewData["Category"] = @StaticData.Categories[StaticData.Categories.FindIndex(secb => secb.Id == deleteItem.Category)].Name;

            return View(deleteItem);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(IncomingModel deleteIncoming)
        {
            _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} IncomingController HttpPost Delete called");

            string result = await _repository.DeleteSingleIncoming(deleteIncoming.Id);
            if (!result.Equals("1"))
            {
                ViewData["Message"] = $"Удаление не удалось.\r\n{result}";
                return View(deleteIncoming);
            }

            return RedirectToAction("Incoming");
        }


        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} IncomingController GET Error called");
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}