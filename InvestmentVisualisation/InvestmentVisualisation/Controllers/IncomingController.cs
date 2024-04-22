using DataAbstraction.Interfaces;
using DataAbstraction.Models;
using DataAbstraction.Models.Incoming;
using DataAbstraction.Models.Settings;
using InvestmentVisualisation.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using UserInputService;


namespace InvestmentVisualisation.Controllers
{
    public class IncomingController : Controller
    {
        private readonly ILogger<IncomingController> _logger;
        private IMySqlIncomingRepository _repository;
        private int _itemsAtPage;
        private IMySqlSecCodesRepository _secCodesRepo;
        private InputHelper _helper;

        public IncomingController(
            ILogger<IncomingController> logger,
            IMySqlIncomingRepository repository,
            IOptions<PaginationSettings> paginationSettings,
            IMySqlSecCodesRepository secCodesRepo,
            InputHelper helper)
        {
            _logger = logger;
            _repository = repository;
            _itemsAtPage = paginationSettings.Value.PageItemsCount;
            _secCodesRepo = secCodesRepo;
            _helper = helper;
        }

        public async Task<IActionResult> Incoming(CancellationToken cancellationToken, int page = 1, string secCode = "")
        {
            _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} IncomingController GET " +
                $"Incoming called, page={page} secCode={secCode}");
            //https://localhost:7226/Incoming/Incoming?page=3

            int count = 0;
            if (secCode.Length > 0)
            {
                count = await _repository.GetIncomingSpecificSecCodeCount(cancellationToken, secCode);
            }
            else
            {
                count = await _repository.GetIncomingCount(cancellationToken);
            }

            _logger.LogDebug($"{DateTime.Now.ToString("HH:mm:ss:fffff")} IncomingController Incoming table size={count}");

            IncomingWithPaginations incomingWithPaginations = new IncomingWithPaginations();

            incomingWithPaginations.PageViewModel = new PaginationPageViewModel(
                count,
                page,
                _itemsAtPage);

            if (secCode.Length > 0)
            {
                ViewBag.secCode = secCode;
                incomingWithPaginations.Incomings = await _repository
                    .GetPageFromIncomingSpecificSecCode(cancellationToken, secCode, _itemsAtPage, (page - 1) * _itemsAtPage);
            }
            else
            {
                incomingWithPaginations.Incomings = await _repository
                    .GetPageFromIncoming(cancellationToken, _itemsAtPage, (page - 1) * _itemsAtPage);
            }

            return View(incomingWithPaginations);
        }

        public IActionResult CreateIncoming()
        {
            _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} IncomingController GET CreateIncoming called");

            CreateIncomingModel model = new CreateIncomingModel();
            model.Date = DateTime.Now.AddDays(-1);// обычно вношу за вчера
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
        public async Task<IActionResult> CreateFromText(CancellationToken cancellationToken, string text)
        {
            _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} IncomingController HttpPost " +
                $"CreateFromText called, text={text}");
            //"Выплата процентного дохода (эмитированы после 01.01.17)\t\t\t\t30.01.2024\t706,8\t0\t\tПРОМОМЕД ДМ оббП02; ISIN-RU000A103G91;\t\t"
            //\t\t\t\t
            //"Выплата дивидендов\t30.01.2024\t3,484.10\t0.00\t\tНК Роснефть, ПАО ао01; ISIN-RU000A0J2Q06;"
            //"Выплата дивидендов\t30.01.2024\t3484,1\t0\t\tНК Роснефть, ПАО ао01; ISIN-RU000A0J2Q06;\t\t"
            //new//"Депозитарная комиссия - за хранение ЦБ\t\t\t\t29.02.2024\t0\t1588,84\tДепо Базовый (№2, ФЛ+абон. 30р)"

            // /t = 5 штук . может быть и 6. isin искать в 5й или 6й
            // Тип / Дата / Зачислено / Списано / Примеч / Примеч

            //"Выплата дивидендов\t16.02.2023\t4,482.80\t0.00\t\tТМК, ПАО ао01; ISIN-RU000A0B6NK6;"
            //"Выплата процентного дохода (эмитированы после 01.01.17)\t\t\t\t03.04.2024\t   741,83\t   0,00\t \tБрусника оббП02; ISIN-RU000A102Y58; "
            //"Выплата процентного дохода (эмитированы после 01.01.17)\t16.02.2023\t16.96\t0.00\t\tИС петролеум ОббП01; ISIN-RU000A1013C9;"
            //"Поступление ДС клиента (безналичное)\t22.02.2023\t60,009.00\t0.00\t\tПеревод средств для участия в торгах по договору на брокерское обслуживание BP19195 от 11.10.2017 #ACC# BP19195-MS-01 #SBP89998#^НДС не облагается"
            // Оборот по погашению ЦБ  14.02.2023  120.00  0.00 === никогда НЕТ isin!

            //old LK
            //"11573042\tПогашение ЦБ / Аннулирование ЦБ\t01.04.2024 00:00:00\tБрусника оббП02, 4B02-02-00492-R-001P
            //\tRU000A102Y58\t31.00\t1,000.\tRUB\t31,000.00\t\t0.00\t0.00\t0.00\t03.04.2024\t01.04.2024"
            //new from OpenOfice
            //"11573042\tПогашение ЦБ / Аннулирование ЦБ\t\t01.04.2024 00:00:00\tБрусника оббП02, 4B02-02-00492-R-001P
            //\tRU000A102Y58\t31\t1000\tRUB\t31000\t\t0\t0\t0\t03.04.2024\t01.04.2024\tRU000A102Y58 Брусника оббП02\tНКО АО НРД"
            if (text is null || !text.Contains("\t"))
            {
                ViewData["Message"] = "Чтение строки не удалось, строка пустая или не содержит табуляций-разделителей: " + text;
                return View("CreateIncoming");
            }

            string[]? textSplitted = _helper.ReturnSplittedArray(text);

            if (textSplitted is null || textSplitted.Length < 2)
            {
                ViewData["Message"] = "Чтение строки не удалось, " +
                    "получено менее 3 значимых элементов (2х табуляций-разделителей) в строке: " + text;
                return View("CreateIncoming");
            }

            CreateIncomingModel model = new CreateIncomingModel();
            // тип операции
            if (textSplitted[0].Contains("Выплата дивидендов") || textSplitted[0].Contains("Выплата процентного дохода"))
            {
                _logger.LogDebug($"{DateTime.Now.ToString("HH:mm:ss:fffff")} IncomingController HttpPost " +
                    $"CreateFromText set category=1 by 'Выплата дивидендов' ");
                model.Category = 1;
            }
            else if (textSplitted[0].Contains("Поступление ДС клиента"))
            {
                _logger.LogDebug($"{DateTime.Now.ToString("HH:mm:ss:fffff")} IncomingController HttpPost " +
                    $"CreateFromText set category=0 seccode=0 by 'Поступление ДС клиента' ");
                model.Category = 0;
                model.SecCode = "0";
            }
            else if (textSplitted[0].Contains("Депозитарная комиссия"))
            {
                _logger.LogDebug($"{DateTime.Now.ToString("HH:mm:ss:fffff")} IncomingController HttpPost " +
                    $"CreateFromText set category=3 seccode=0 by 'Депозитарная комиссия' ");
                model.Category = 3;
                model.SecCode = "0";
            }
            else if (textSplitted[0].Contains("Оборот по погашению"))
            {
                _logger.LogDebug($"{DateTime.Now.ToString("HH:mm:ss:fffff")} IncomingController HttpPost " +
                    $"CreateFromText set category=2 by 'Оборот по погашению' ");
                model.Category = 2;
            }
            else if ( textSplitted[0].Contains("Погашение ЦБ") || textSplitted[1].Contains("Погашение ЦБ") )
            {
                _logger.LogDebug($"{DateTime.Now.ToString("HH:mm:ss:fffff")} IncomingController HttpPost " +
                    $"CreateFromText set category=4 by 'Погашение ЦБ' ");
                model.Category = 2;

                // 1 - date
                if (textSplitted.Length >=12 && _helper.IsDataFormatCorrect(textSplitted[11]))
                {
                    model.Date = DateTime.Parse(textSplitted[11]);
                }
                else if (textSplitted.Length >= 13 && _helper.IsDataFormatCorrect(textSplitted[12]))
                {
                    model.Date = DateTime.Parse(textSplitted[12]);
                }
                else
                {
                    bool isDataFormatCorrect = _helper.IsDataFormatCorrect(textSplitted[1]);
                    bool isDataFormatCorrect2 = _helper.IsDataFormatCorrect(textSplitted[2]);
                    if (isDataFormatCorrect)
                    {
                        model.Date = DateTime.Parse(textSplitted[1]);
                    }
                    else if (isDataFormatCorrect2)
                    {
                        model.Date = DateTime.Parse(textSplitted[2]);
                    }
                    else
                    {
                        _logger.LogWarning($"{DateTime.Now.ToString("HH:mm:ss:fffff")} IncomingController HttpPost " +
                            $"CreateFromText Date recognizing error for '{textSplitted[2]}', lenght of cell less then 12");
                        ViewData["Message"] = $"DATE '{textSplitted[2]}' not recognized, lenght of cell less then 12";

                        // just set yesterday
                        model.Date = DateTime.Now.AddDays(-1);
                    }
                }

                // 2 - money volume
                if (textSplitted.Length >= 8 && _helper.IsDecimal(textSplitted[7]))
                {
                    model.Value = _helper.CleanPossibleNumber(textSplitted[7]);
                }
                else if (textSplitted.Length >= 9 && _helper.IsDecimal(textSplitted[8]))
                {
                    model.Value = _helper.CleanPossibleNumber(textSplitted[8]);
                }
                else
                {
                    // no idea ... multyply textSplitted[6] * textSplitted[7] ?????
                    ViewData["Message"] = $"Money data not found" + ViewData["Message"];
                }

                // 4 - ISIN == textSplitted 3 or 4, contain directly ISIN
                if (textSplitted.Length >=4 && textSplitted[3].Length > 0 && 
                    StaticData.SecCodes.Any(x => x.SecCode == textSplitted[3]))
                {
                    _logger.LogDebug($"{DateTime.Now.ToString("HH:mm:ss:fffff")} IncomingController HttpPost " +
                        $"CreateFromText try set SecCode by {textSplitted[3]}");
                    model.SecCode = StaticData.SecCodes.Find(x => x.SecCode == textSplitted[3]).SecCode;
                }
                if (textSplitted.Length >=5 && textSplitted[4].Length > 0 &&
                    StaticData.SecCodes.Any(x => x.SecCode == textSplitted[4]))
                {
                    _logger.LogDebug($"{DateTime.Now.ToString("HH:mm:ss:fffff")} IncomingController HttpPost " +
                        $"CreateFromText try set SecCode by {textSplitted[4]}");
                    model.SecCode = StaticData.SecCodes.Find(x => x.SecCode == textSplitted[4]).SecCode;
                }

                if (model.Category == 2 && (model.SecCode is null || model.SecCode.Equals("0")))
                {
                    ViewData["Message"] = $"Check/Input tiker manually! " + ViewData["Message"];
                }

                return View("CreateIncoming", model);
            }
            else
            {
                // some unrecognizable shit
                ViewData["Message"] = $"Input data not recognized";
                return View("CreateIncoming", model);
            }



            bool isDataFormatCorrectCommon = _helper.IsDataFormatCorrect(textSplitted[1]);
            if (isDataFormatCorrectCommon)
            {
                model.Date = DateTime.Parse(textSplitted[1]);
            }
            else
            {
                _logger.LogWarning($"{DateTime.Now.ToString("HH:mm:ss:fffff")} IncomingController HttpPost " +
                    $"CreateFromText Date recognizing error for '{textSplitted[1]}'");
                ViewData["Message"] = $"DATE '{textSplitted[1]}' not recognized";

                // just set yesterday
                model.Date = DateTime.Now.AddDays(-1);
            }
            _logger.LogDebug($"{DateTime.Now.ToString("HH:mm:ss:fffff")} IncomingController HttpPost " +
                $"CreateFromText set Date as: {model.Date}");


            //деньги \t3,484.10  \t3484,1 // 0 in new
            if (textSplitted.Length >= 3 && 
                !textSplitted[2].Equals("0.00") &&
                !textSplitted[2].Equals("0,00") &&
                !textSplitted[2].Equals("0") && 
                _helper.IsDecimal(textSplitted[2]))
            {
                model.Value = _helper.CleanPossibleNumber(textSplitted[2]);
            }
            else if (textSplitted.Length >= 4 && 
                !textSplitted[3].Equals("0.00") &&
                !textSplitted[3].Equals("0,00") &&
                !textSplitted[3].Equals("0") && 
                _helper.IsDecimal(textSplitted[3]))
            {
                model.Value = _helper.CleanPossibleNumber(textSplitted[3]);
            }
            else
            {
                _logger.LogWarning($"{DateTime.Now.ToString("HH:mm:ss:fffff")} IncomingController HttpPost " +
                    $"CreateFromText number recognizing error for cells 2 and 3: '{textSplitted[2]}' or '{textSplitted[3]}'");
                ViewData["Message"] = $"cell 2='{textSplitted[2]}' or cell 3='{textSplitted[3]}' not recognized as number";

                model.Value = "0";
            }


            // try calculate comission
            if (model.Category == 1 && !model.Value.Equals("0"))
            {
                decimal comission = _helper.GetDecimalFromString(model.Value) / 100;
                decimal comissionRounded = Math.Round(comission, 2);

                model.Comission = comissionRounded.ToString();

                ViewData["ComissionMessage"] = "Comission calculated manually! Recheck!";
            }



            //ISIN to seccode
            if (model.Category != 0 && model.Category != 3) // это не зачисленные мной деньги или не списанная брок комиссия
            {
                if (model.Category == 2) // тут нет ISIN, надо запрашивать последний добавленный incoming и брать seccode оттуда 
                {
                    _logger.LogDebug($"{DateTime.Now.ToString("HH:mm:ss:fffff")} IncomingController HttpPost " +
                        $"CreateFromText request last incoming. Reason: Category == 2");
                    model.SecCode = await _repository.GetSecCodeFromLastRecord(cancellationToken);
                }
                else // попробуем найти заполненный ISIN 
                {
                    if (textSplitted.Length >=4 && textSplitted[3].Length > 0 && textSplitted[3].Contains("ISIN-"))
                    {
                        _logger.LogDebug($"{DateTime.Now.ToString("HH:mm:ss:fffff")} IncomingController HttpPost " +
                            $"CreateFromText try set SecCode by {textSplitted[3]}");
                        model.SecCode = await _helper.GetSecCodeByISIN(cancellationToken, textSplitted[3]);
                    }
                    if (textSplitted.Length >=5 && textSplitted[4].Length > 0 && textSplitted[4].Contains("ISIN-"))
                    {
                        _logger.LogDebug($"{DateTime.Now.ToString("HH:mm:ss:fffff")} IncomingController HttpPost " +
                            $"CreateFromText try set SecCode by {textSplitted[4]}");
                        model.SecCode = await _helper.GetSecCodeByISIN(cancellationToken, textSplitted[4]);
                    }
                }
            }

            if (model.SecCode is null)
            {
                ViewData["Message"] = $"Check/Input tiker manually! " + ViewData["Message"];
            }
            //проверить, что нам прислали действительно seccode а не ошибку
            else if (!StaticData.SecCodes.Any(x => x.SecCode == model.SecCode))// если нет
            {
                //отправим найденное в ошибки
                ViewData["Message"] = model.SecCode;
                model.SecCode = "0";//сбросим присвоенную ошибку
                // return here to show error
                return View("CreateIncoming", model);
            }

            return View("CreateIncoming", model);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateIncoming(CancellationToken cancellationToken, CreateIncomingModel newIncoming)
        {
            _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} IncomingController POST " +
                $"CreateIncoming called");

            if (!newIncoming.SecCode.Equals("0") && newIncoming.Category == 0)
            {
                _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} IncomingController CreateIncoming error " +
                    $"- 'Зачисление денег' невозможно для secCode {newIncoming.SecCode}");

                ViewData["Message"] = $"'Зачисление денег' невозможно для {newIncoming.SecCode}. " +
                    $"Используй Дивиденты или досрочное погашение";

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

                ViewData["Message"] = $"'Дивиденды' недопустимы для денег. " +
                    $"Для начисления денег от оброкера на ваши деньги - используйте досрочное погашение.";

                return View();
            }

            newIncoming.SecBoard = StaticData
                .SecCodes[StaticData.SecCodes.FindIndex(x => x.SecCode == newIncoming.SecCode)].SecBoard;

            newIncoming.Value = newIncoming.Value.Replace(',', '.');
            if (newIncoming.Comission is not null)
            {
                newIncoming.Comission = newIncoming.Comission.Replace(',', '.');
            }

            _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} IncomingController POST " +
                $"CreateIncoming validation complete, try create at repository");

            string result = await _repository.CreateNewIncoming(cancellationToken, newIncoming);
            if (!result.Equals("1"))
            {
                ViewData["Message"] = $"Добавление не удалось. \r\n{result}";
                return View();
            }

            return RedirectToAction("Incoming");
        }

        public async Task<IActionResult> Edit(CancellationToken cancellationToken, int id)
        {
            _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} IncomingController GET Edit id={id} called");

            IncomingModel editedItem = await _repository.GetSingleIncomingById(cancellationToken, id);

            return View(editedItem);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(CancellationToken cancellationToken, IncomingModel newIncoming)
        {
            _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} IncomingController HttpPost Edit called");

            newIncoming.Value = newIncoming.Value.Replace(',', '.');
            if (newIncoming.Comission is not null)
            {
                newIncoming.Comission = newIncoming.Comission.Replace(',', '.');
            }

            string result = await _repository.EditSingleIncoming(cancellationToken, newIncoming);
            if (!result.Equals("1"))
            {
                ViewData["Message"] = $"Редактирование не удалось.\r\n{result}";
                return View(newIncoming);
            }

            return RedirectToAction("Incoming");
        }
        [HttpGet]
        public async Task<IActionResult> Delete(CancellationToken cancellationToken, int id)
        {
            _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} IncomingController " +
                $"GET Delete id={id} called");

            IncomingModel deleteItem = await _repository.GetSingleIncomingById(cancellationToken, id);

            ViewData["SecBoard"] = @StaticData
                .SecBoards[StaticData.SecBoards.FindIndex(secb => secb.Id == deleteItem.SecBoard)].Name;
            ViewData["Category"] = @StaticData
                .Categories[StaticData.Categories.FindIndex(secb => secb.Id == deleteItem.Category)].Name;

            return View(deleteItem);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAction(CancellationToken cancellationToken, int id)
        {
            _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} IncomingController " +
                $"HttpPost DeleteAction id={id} called");

            string result = await _repository.DeleteSingleIncoming(cancellationToken, id);
            if (!result.Equals("1"))
            {
                TempData["Message"] = $"Удаление id={id} не удалось.\r\n{result}";
                return RedirectToAction("Delete", new { id = id });
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