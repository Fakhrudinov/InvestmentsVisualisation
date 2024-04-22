using DataAbstraction.Models;
using Microsoft.AspNetCore.Mvc;
using DataAbstraction.Interfaces;
using Microsoft.Extensions.Options;
using DataAbstraction.Models.Settings;
using DataAbstraction.Models.Deals;
using UserInputService;
using System.Text;

namespace InvestmentVisualisation.Controllers
{
    public class DealsController : Controller
    {
        private readonly ILogger<DealsController> _logger;
        private IMySqlDealsRepository _repository;
        private int _itemsAtPage;
        private IMySqlSecCodesRepository _secCodesRepo;
        private InputHelper _helper;

        public DealsController(
            ILogger<DealsController> logger, 
            IMySqlDealsRepository repository, 
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


        public async Task<IActionResult> Deals(CancellationToken cancellationToken, int page = 1, string secCode = "")
        {
            _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} DealsController GET " +
                $"Deals called, page={page} secCode={secCode}");

            int count = 0;
            if (secCode.Length > 0)
            {
                count = await _repository.GetDealsSpecificSecCodeCount(cancellationToken, secCode);
            }
            else
            {
                count = await _repository.GetDealsCount(cancellationToken);
            }
            _logger.LogDebug($"{DateTime.Now.ToString("HH:mm:ss:fffff")} DealsController Deals table size={count}");

            DealsWithPaginations dealsWithPaginations = new DealsWithPaginations();

            dealsWithPaginations.PageViewModel = new PaginationPageViewModel(
            count,
            page,
            _itemsAtPage);

            if (secCode.Length > 0)
            {
                ViewBag.secCode = secCode;
                dealsWithPaginations.Deals = await _repository
                    .GetPageFromDealsSpecificSecCode(cancellationToken, secCode, _itemsAtPage, (page - 1) * _itemsAtPage);
            }
            else
            {
                dealsWithPaginations.Deals = await _repository
                    .GetPageFromDeals(cancellationToken, _itemsAtPage, (page - 1) * _itemsAtPage);
            }            

            return View(dealsWithPaginations);
        }

        public ActionResult Create(CreateDealsModel model)
        {
            _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} DealsController GET Create called");

            //CreateDealsModel model = new CreateDealsModel();
            if (model.Date == DateTime.MinValue)
            {
                model.Date = DateTime.Now.AddDays(-1);// обычно вношу за вчера
            }
          

            return View(model);
        }
        public ActionResult CreateSpecific(DateTime data, string tiker, string price, int pieces)
        {
            _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} DealsController GET CreateSpecific called " +
                $"with parametres {data} {tiker} price={price} pieces={pieces}");

            CreateDealsModel model = new CreateDealsModel();
            model.Date = data;
            model.SecCode = tiker;
            model.AvPrice = price;
            model.Pieces = pieces;
            return View("Create", model);
        }
        [HttpPost]
        public async Task<IActionResult> CreateFromText(CancellationToken cancellationToken, string text)
        {
            _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} DealsController HttpPost " +
                $"CreateFromText called, text={text}");
            //"9466729404\t42847041579\tПокупка ЦБ на бирже\t\t25.01.2024 14:56:13\tРоссети Ленэнерго ап01, 2-01-00073-A
            //\tRU0009092134
            //\t1\t100
            //\t207,3\tRUB\t20730\t0\tRUB\t0\t3,52\t0\t26.01.2024\t26.01.2024\tПАО Московская Биржа\tНКО АО НРД"


            //"7318187850\t34614262505\tПокупка ЦБ на бирже\t15.02.2023 10:40:43\tБанк СПб, ПАО ао03, 10300436B\tRU0009100945\t1.00\t50.00\t112.07\tRUB\t5,603.50\t0.00\tRUB\t0.00\t0.95\t0.00\t17.02.2023\t17.02.2023\tПАО Московская Биржа"
            //"Покупка ЦБ на бирже\t15.02.2023 10:53:59\tСелигдар, ПАО ао01, 1-01-32694-F\tRU000A0JPR50\t1.00\t100.00\t46.83\tRUB\t4,683.00\t0.00\tRUB\t0.00\t0.80\t0.0"

            if (text is null || !text.Contains("\t"))
            {
                ViewData["Message"] = "Чтение строки не удалось, строка пустая или не содержит табуляций-разделителей: " + text;
                return View("Create", new CreateDealsModel());
            }

            string[]? textSplitted = _helper.ReturnSplittedArray(text);

            if (textSplitted is null || textSplitted.Length < 13)
            {
                ViewData["Message"] = "Чтение строки не удалось, " +
                    "получено менее 13 элементов (12х табуляций-разделителей) в строке: " + text;
                return View("Create", new CreateDealsModel());
            }

            // поищем, где расположен текст "Покупка ЦБ на бирже" - от этой точки будем считывать столбцы
            int startPointer = 0;
            for (int i = 0; i < textSplitted.Length; i++)
            {
                if (textSplitted[i].Contains("Покупка ЦБ на бирже"))
                {
                    startPointer = i;
                    break;
                }

                if (i > 5)
                {
                    ViewData["Message"] = "Не найдена точка входа 'Покупка ЦБ на бирже' в тексте: " + text;
                    return View("Create", new CreateDealsModel());
                }
            }

            // начинаем заполнение
            CreateDealsModel model = new CreateDealsModel();
            StringBuilder stringBuilderErrors = new StringBuilder();

            // дата // 15.02.2023 10:40:43
            if (_helper.IsDataFormatCorrect(textSplitted[startPointer + 1]))
            {
                model.Date = DateTime.Parse(textSplitted[startPointer + 1]);
                _logger.LogDebug($"{DateTime.Now.ToString("HH:mm:ss:fffff")} DealsController HttpPost " +
                    $"CreateFromText set date={model.Date} from {textSplitted[startPointer + 1]}");
            }
            else
            {
                stringBuilderErrors.Append("Date not recognized! ");
                _logger.LogWarning($"{DateTime.Now.ToString("HH:mm:ss:fffff")} DealsController HttpPost " +
                    $"CreateFromText Date not recognized from {textSplitted[startPointer + 1]}");

                // just set yesterday
                model.Date = DateTime.Now.AddDays(-1);
            }

            // количество // 60,000.00
            if (_helper.IsInt32Correct(textSplitted[startPointer + 5]))
            {
                model.Pieces = _helper.GetInt32FromString(textSplitted[startPointer + 5]);
                _logger.LogDebug($"{DateTime.Now.ToString("HH:mm:ss:fffff")} DealsController HttpPost " +
                    $"CreateFromText set Pieces={model.Pieces} from {textSplitted[startPointer + 5]}");
            }
            else
            {
                stringBuilderErrors.Append("Pieces not recognized! ");
                _logger.LogWarning($"{DateTime.Now.ToString("HH:mm:ss:fffff")} DealsController HttpPost " +
                    $"CreateFromText Pieces not recognized from {textSplitted[startPointer + 5]}");
            }

            // цена // 4,583.5 или 0.08708
            if (_helper.IsDecimal(textSplitted[startPointer + 6]))
            {
                model.AvPrice = _helper.CleanPossibleNumber(textSplitted[startPointer + 6]);
                _logger.LogDebug($"{DateTime.Now.ToString("HH:mm:ss:fffff")} DealsController HttpPost " +
                    $"CreateFromText set AvPrice={model.AvPrice} from {textSplitted[startPointer + 6]}");
            }
            else
            {
                stringBuilderErrors.Append("AvPrice not recognized! ");
                _logger.LogWarning($"{DateTime.Now.ToString("HH:mm:ss:fffff")} DealsController HttpPost " +
                    $"CreateFromText AvPrice not recognized from {textSplitted[startPointer + 6]}");
            }

            //комиссия = комиссия биржи + комиссия брокера 1.79	0.24
            if (_helper.IsDecimal(textSplitted[startPointer + 11]) && _helper.IsDecimal(textSplitted[startPointer + 12]))
            {
                decimal moexComiss = _helper.GetDecimalFromString(textSplitted[startPointer + 11]);
                decimal brokComiss = _helper.GetDecimalFromString(textSplitted[startPointer + 12]);
                model.Comission = (moexComiss + brokComiss).ToString();

                _logger.LogDebug($"{DateTime.Now.ToString("HH:mm:ss:fffff")} DealsController HttpPost " +
                    $"CreateFromText set Comission={model.Comission} from " +
                    $"{textSplitted[startPointer + 11]}({moexComiss}) + {textSplitted[startPointer + 12]}({brokComiss})");
            }
            else
            {
                stringBuilderErrors.Append("Comission not recognized! ");
                _logger.LogWarning($"{DateTime.Now.ToString("HH:mm:ss:fffff")} DealsController HttpPost " +
                    $"CreateFromText Comission not recognized from " +
                    $"{textSplitted[startPointer + 11]} or {textSplitted[startPointer + 12]}");
            }

            // nkd // 289.20
            string nkd = _helper.CleanPossibleNumber(textSplitted[startPointer + 9]);
            if (!nkd.Equals("0.00") && !nkd.Equals("0,00") && !nkd.Equals("0") && !nkd.Equals("RUB"))
            {
                if (_helper.IsDecimal(textSplitted[startPointer + 9]))
                {
                    model.NKD = nkd;
                    _logger.LogDebug($"{DateTime.Now.ToString("HH:mm:ss:fffff")} DealsController HttpPost " +
                        $"CreateFromText set NKD={model.NKD} from {textSplitted[startPointer + 9]}");
                }
                else
                {
                    stringBuilderErrors.Append("NKD not recognized! ");
                    _logger.LogWarning($"{DateTime.Now.ToString("HH:mm:ss:fffff")} DealsController HttpPost " +
                        $"CreateFromText NKD is zero value or not recognized from {textSplitted[startPointer + 9]}");
                }
            }


            // тикер
            string rawSecCode = await _secCodesRepo.GetSecCodeByISIN(cancellationToken, textSplitted[startPointer + 3]);
            _logger.LogDebug($"{DateTime.Now.ToString("HH:mm:ss:fffff")} DealsController получили из репозитория={rawSecCode}");
            //проверить, что нам прислали действительно seccode а не ошибку
            if (!StaticData.SecCodes.Any(x => x.SecCode == rawSecCode))// если нет
            {
                _logger.LogWarning($"{DateTime.Now.ToString("HH:mm:ss:fffff")} DealsController HttpPost " +
                    $"CreateFromText ISIN not recognized from {textSplitted[startPointer + 3]}");

                if (rawSecCode is null)
                {
                    rawSecCode = "ISIN not recognized! ";
                }

                //отправим найденное в ошибки
                stringBuilderErrors.Append(rawSecCode);
                model.SecCode = "0";//сбросим присвоенную ошибку
            }
            else
            {
                model.SecCode = rawSecCode;
            }
            _logger.LogDebug($"{DateTime.Now.ToString("HH:mm:ss:fffff")} DealsController HttpPost " +
                $"CreateFromText set SecCode={model.SecCode}");

            ViewData["Message"] = stringBuilderErrors.ToString();
            return View("Create", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult RecalculateComission(CreateDealsModel model)
        {
            _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} DealsController POST " +
                $"RecalculateComission called with Pieces={model.Pieces} AvPrice={model.AvPrice}");
            decimal decimalAvPrice = _helper.GetDecimalFromString(model.AvPrice);
            decimal posValue = (decimalAvPrice * model.Pieces);

            model.Comission = Math.Round((posValue/100000)*17, 2).ToString();

            _logger.LogDebug($"{DateTime.Now.ToString("HH:mm:ss:fffff")} DealsController POST " +
                $"RecalculateComission decimalAvPrice={decimalAvPrice}, posValue={posValue}, finally " +
                $"model.Comission={model.Comission}");

            return RedirectToAction("Create", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CancellationToken cancellationToken, CreateDealsModel model)
        {
            _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} DealsController POST Create called");

            model.SecBoard = StaticData.SecCodes[StaticData.SecCodes.FindIndex(x => x.SecCode == model.SecCode)].SecBoard;

            if (model.SecCode.Equals("0"))
            {
                _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} DealsController Create error " +
                    $"- Сделка по secCode Деньги не возможна!");
                ViewData["Message"] = $"Сделка по тикеру Деньги не возможна!";
                return View(model);
            }
            if (model.SecBoard == 1 && model.NKD is not null)
            {
                _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} DealsController Create error " +
                    $"- NKD на акции недопустим");
                ViewData["Message"] = $"NKD на акции недопустим";
                return View(model);
            }

            model.AvPrice = model.AvPrice.Replace(',', '.');
            if (model.Comission is not null)
            {
                model.Comission = model.Comission.Replace(',', '.');
            }
            if (model.NKD is not null)
            {
                model.NKD = model.NKD.Replace(',', '.');
            }

            _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} DealsController POST Create validation complete, " +
                $"try create at repository");

            string result = await _repository.CreateNewDeal(cancellationToken, model);
            if (!result.Equals("1"))
            {
                ViewData["Message"] = $"Добавление не удалось. \r\n{result}";
                return View(model);
            }

            return RedirectToAction("Deals");
        }

        public async Task<IActionResult> Edit(CancellationToken cancellationToken, int id)
        {
            _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} DealsController GET Edit id={id} called");

            DealModel editedItem = await _repository.GetSingleDealById(cancellationToken, id);

            return View(editedItem);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(CancellationToken cancellationToken, DealModel model)
        {
            _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} DealsController HttpPost Edit called");

            model.AvPrice = model.AvPrice.Replace(',', '.');
            if (model.Comission is not null)
            {
                model.Comission = model.Comission.Replace(',', '.');
            }
            if (model.NKD is not null)
            {
                model.NKD = model.NKD.Replace(',', '.');
            }

            string result = await _repository.EditSingleDeal(cancellationToken, model);
            if (!result.Equals("1"))
            {
                ViewData["Message"] = $"Редактирование не удалось.\r\n{result}";
                return View(model);
            }

            return RedirectToAction("Deals");
        }

        public async Task<IActionResult> Delete(CancellationToken cancellationToken, int id)
        {
            _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} DealsController GET " +
                $"Delete id={id} called");

            DealModel deleteItem = await _repository.GetSingleDealById(cancellationToken, id);

            ViewData["SecBoard"] = @StaticData.SecBoards[StaticData.SecBoards
                .FindIndex(secb => secb.Id == deleteItem.SecBoard)].Name;

            return View(deleteItem);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAction(CancellationToken cancellationToken, int id)
        {
            _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} DealsController " +
                $"HttpPost DeleteAction id={id} called");

            string result = await _repository.DeleteSingleDeal(cancellationToken, id);
            if (!result.Equals("1"))
            {
                TempData["Message"] = $"Удаление id={id} не удалось.\r\n{result}";
                return RedirectToAction("Delete", new { id = id });
            }

            return RedirectToAction("Deals");
        }
    }
}
