using DataAbstraction.Interfaces;
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
        private IMySqlSecCodesRepository _secCodesRepo;

        public IncomingController(
            ILogger<IncomingController> logger, 
            IMySqlIncomingRepository repository, 
            IOptions<PaginationSettings> paginationSettings,
            IMySqlSecCodesRepository secCodesRepo)
        {
            _logger = logger;
            _repository = repository;
            _itemsAtPage = paginationSettings.Value.PageItemsCount;
            _secCodesRepo = secCodesRepo;
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
        public async Task<IActionResult> CreateFromText(string text)
        {
            _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} IncomingController HttpPost CreateFromText called, text={text}");

            // /t = 5 штук . может быть и 6. isin искать в 5й или 6й
            // Тип / Дата / Зачислено / Списано / Примеч / Примеч

            //"Выплата дивидендов\t16.02.2023\t4,482.80\t0.00\t\tТМК, ПАО ао01; ISIN-RU000A0B6NK6;"
            //"Выплата процентного дохода (эмитированы после 01.01.17)\t16.02.2023\t16.96\t0.00\t\tИС петролеум ОббП01; ISIN-RU000A1013C9;"
            //"Поступление ДС клиента (безналичное)\t22.02.2023\t60,009.00\t0.00\t\tПеревод средств для участия в торгах по договору на брокерское обслуживание BP19195 от 11.10.2017 #ACC# BP19195-MS-01 #SBP89998#^НДС не облагается"
            // Депозитарная комиссия - за хранение ЦБ	31.01.2023	0.00	533.52	Депо Базовый (№2, ФЛ+абон. 30р)
            // Оборот по погашению ЦБ  14.02.2023  120.00  0.00 === никогда НЕТ isin!

            CreateIncomingModel model = new CreateIncomingModel();
            if (text is null || !text.Contains("\\t"))
            {
                ViewData["Message"] = "Чтение строки не удалось, строка пустая или не содержит табуляций-разделителей: " + text;
                return View("CreateIncoming");
            }


            string[] textSplitted = text.Split("\t");
            if (textSplitted.Length < 3)
            {
                ViewData["Message"] = "Чтение строки не удалось, получено менее 3 элементов (2х табуляций-разделителей) в строке: " + text;
                return View("CreateIncoming");
            }

            // тип операции
            if (textSplitted[0].Contains("Выплата дивидендов") || textSplitted[0].Contains("Выплата процентного дохода"))
            {
                _logger.LogDebug($"{DateTime.Now.ToString("HH:mm:ss:fffff")} IncomingController HttpPost CreateFromText set category=1 by 'Выплата дивидендов' ");
                model.Category = 1;
            }
            else if (textSplitted[0].Contains("Оборот по погашению"))
            {
                _logger.LogDebug($"{DateTime.Now.ToString("HH:mm:ss:fffff")} IncomingController HttpPost CreateFromText set category=2 by 'Оборот по погашению' ");
                model.Category = 2;
            }
            else if (textSplitted[0].Contains("Поступление ДС клиента"))
            {
                _logger.LogDebug($"{DateTime.Now.ToString("HH:mm:ss:fffff")} IncomingController HttpPost CreateFromText set category=0 seccode=0 by 'Поступление ДС клиента' ");
                model.Category = 0;
                model.SecCode = "0";
            }
            else if (textSplitted[0].Contains("Депозитарная комиссия"))
            {
                _logger.LogDebug($"{DateTime.Now.ToString("HH:mm:ss:fffff")} IncomingController HttpPost CreateFromText set category=3 seccode=0 by 'Депозитарная комиссия' ");
                model.Category = 3;
                model.SecCode = "0";
            }

            // дата 
            string[] dataSplitted = textSplitted[1].Split('.');
            string dateString = $"{dataSplitted[2]}-{dataSplitted[1]}-{dataSplitted[0]} {DateTime.Now.Hour}:{DateTime.Now.Minute}:{DateTime.Now.Second}";
            model.Date = DateTime.Parse(dateString);
            _logger.LogDebug($"{DateTime.Now.ToString("HH:mm:ss:fffff")} IncomingController HttpPost CreateFromText set {model.Date}");

            //деньги
            if (textSplitted[2].Length > 0 && !textSplitted[2].Equals("0.00"))
            {
                model.Value = textSplitted[2];
                _logger.LogDebug($"{DateTime.Now.ToString("HH:mm:ss:fffff")} IncomingController HttpPost CreateFromText set {model.Value} by textSplitted[2]");
            }
            else if (textSplitted[3].Length > 0 && !textSplitted[3].Equals("0.00"))
            {
                model.Value = textSplitted[3];
                _logger.LogDebug($"{DateTime.Now.ToString("HH:mm:ss:fffff")} IncomingController HttpPost CreateFromText set {model.Value} by textSplitted[3]");
            }
            model.Value = model.Value.Replace(",", "") ;

            //ISIN to seccode
            if (model.Category != 0) // это или деньги или не распознали ничего
            {
                if (model.Category == 2) // тут нет ISIN, надо запрашивать последний добавленный incoming и брать seccode оттуда 
                {
                    _logger.LogDebug($"{DateTime.Now.ToString("HH:mm:ss:fffff")} IncomingController HttpPost CreateFromText request last incoming. Reason: Category == 2");
                    model.SecCode = await _repository.GetSecCodeFromLastRecord();
                }
                else // попробуем найти заполненный ISIN 
                {
                    if (textSplitted.Length >=4 && textSplitted[4].Length > 0 && textSplitted[4].Contains("ISIN-"))
                    {
                        _logger.LogDebug($"{DateTime.Now.ToString("HH:mm:ss:fffff")} IncomingController HttpPost CreateFromText try set SecCode by {textSplitted[4]}");
                        model.SecCode = await GetSecCodeByISIN(textSplitted[4]);
                    }
                    if (textSplitted.Length >=5 && textSplitted[5].Length > 0 && textSplitted[5].Contains("ISIN-"))
                    {
                        _logger.LogDebug($"{DateTime.Now.ToString("HH:mm:ss:fffff")} IncomingController HttpPost CreateFromText try set SecCode by {textSplitted[5]}");
                        model.SecCode = await GetSecCodeByISIN(textSplitted[5]);
                    }

                    //проверить, что нам прислали действительно seccode а не ошибку
                    if (!StaticData.SecCodes.Any(x => x.SecCode == model.SecCode))// если нет
                    {
                        //отправим найденное в ошибки
                        ViewData["Message"] = model.SecCode;
                        model.SecCode = "0";//сбросим присвоенную ошибку
                    }
                }
            }

            return View("CreateIncoming", model);
        }

        private async Task<string> GetSecCodeByISIN(string text)
        {
            //ТМК, ПАО ао01; ISIN-RU000A0B6NK6;
            string isin = text.Split("ISIN-")[1];
            if (isin.Length < 12)
            {
                return "Ошибка. Длинна ISIN слишком мала: " + isin;
            }
            isin = isin.Substring(0, 12);

            string secCode = await _secCodesRepo.GetSecCodeByISIN(isin);
            _logger.LogDebug($"{DateTime.Now.ToString("HH:mm:ss:fffff")} IncomingController GetSecCodeByISIN получили из репозитория={secCode}");
            return secCode;
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

        public IActionResult HelpPage()
        {
            _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} IncomingController GET HelpPage called");

            return View();
        }
    }
}