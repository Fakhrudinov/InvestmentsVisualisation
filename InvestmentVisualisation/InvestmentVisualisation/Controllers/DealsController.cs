using DataAbstraction.Models;
using Microsoft.AspNetCore.Mvc;
using DataAbstraction.Interfaces;
using Microsoft.Extensions.Options;
using DataAbstraction.Models.Settings;
using DataAbstraction.Models.Deals;
using System.Threading;

namespace InvestmentVisualisation.Controllers
{
    public class DealsController : Controller
    {
        private readonly ILogger<DealsController> _logger;
        private IMySqlDealsRepository _repository;
        private int _itemsAtPage;
        private IMySqlSecCodesRepository _secCodesRepo;

        public DealsController(
            ILogger<DealsController> logger, 
            IMySqlDealsRepository repository, 
            IOptions<PaginationSettings> paginationSettings,
            IMySqlSecCodesRepository secCodesRepo)
        {
            _logger = logger;
            _repository = repository;
            _itemsAtPage = paginationSettings.Value.PageItemsCount;
            _secCodesRepo = secCodesRepo;
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

        public ActionResult Create()
        {
            _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} DealsController GET Create called");

            CreateDealsModel model = new CreateDealsModel();
            model.Date = DateTime.Now.AddDays(-1);// обычно вношу за вчера

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

            //"7318187850\t34614262505\tПокупка ЦБ на бирже\t15.02.2023 10:40:43\tБанк СПб, ПАО ао03, 10300436B\tRU0009100945\t1.00\t50.00\t112.07\tRUB\t5,603.50\t0.00\tRUB\t0.00\t0.95\t0.00\t17.02.2023\t17.02.2023\tПАО Московская Биржа"
            //"Покупка ЦБ на бирже\t15.02.2023 10:53:59\tСелигдар, ПАО ао01, 1-01-32694-F\tRU000A0JPR50\t1.00\t100.00\t46.83\tRUB\t4,683.00\t0.00\tRUB\t0.00\t0.80\t0.0"

            if (text is null || !text.Contains("\t"))
            {
                ViewData["Message"] = "Чтение строки не удалось, строка пустая или не содержит табуляций-разделителей: " + text;
                return View("Create");
            }

            string[] textSplitted = text.Split("\t");
            if (textSplitted.Length < 13)
            {
                ViewData["Message"] = "Чтение строки не удалось, " +
                    "получено менее 13 элементов (12х табуляций-разделителей) в строке: " + text;
                return View("Create");
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
                    return View("Create");
                }
            }

            // начинаем заполнение
            CreateDealsModel model = new CreateDealsModel();

            // дата // 15.02.2023 10:40:43
            //string rawData = textSplitted[startPointer + 1];
            //string[] dataSplitted = textSplitted[startPointer + 1].Split(' ').First().Split('.');
            //string dateString = $"{dataSplitted[2]}-{dataSplitted[1]}-{dataSplitted[0]} {DateTime.Now.Hour}:{DateTime.Now.Minute}:{DateTime.Now.Second}";
            //model.Date = DateTime.Parse(dateString);
            model.Date = DateTime.Parse(textSplitted[startPointer + 1]);
            _logger.LogDebug($"{DateTime.Now.ToString("HH:mm:ss:fffff")} DealsController HttpPost " +
                $"CreateFromText set date={model.Date}");

            // количество // 60,000.00
            //string rawPieces = textSplitted[startPointer + 5]; 
            string pieces = textSplitted[startPointer + 5].Split('.').First();
            model.Pieces = Int32.Parse(pieces.Replace(",", ""));
            _logger.LogDebug($"{DateTime.Now.ToString("HH:mm:ss:fffff")} DealsController HttpPost " +
                $"CreateFromText set Pieces={model.Pieces}");

            // цена // 4,583.5 или 0.08708
            //string rawPrice = textSplitted[startPointer + 6];
            model.AvPrice = CleanString(textSplitted[startPointer + 6]);
            _logger.LogDebug($"{DateTime.Now.ToString("HH:mm:ss:fffff")} DealsController HttpPost " +
                $"CreateFromText set AvPrice={model.AvPrice}");

            //комиссия = комиссия биржи + комиссия брокера 1.79	0.24
            //string rawMoexComiss = textSplitted[startPointer + 11];
            //string rawBrokComiss = textSplitted[startPointer + 12];
            string moexComiss = CleanString(textSplitted[startPointer + 11]);
            string brokComiss = CleanString(textSplitted[startPointer + 12]);
            model.Comission = (decimal.Parse(moexComiss) + decimal.Parse(brokComiss)).ToString();
            _logger.LogDebug($"{DateTime.Now.ToString("HH:mm:ss:fffff")} DealsController HttpPost " +
                $"CreateFromText set Comission={model.Comission} from {moexComiss} + {brokComiss}");

            // nkd // 289.20
            //string rawNkd = textSplitted[startPointer + 9];
            string nkd = CleanString(textSplitted[startPointer + 9]);
            if (!nkd.Equals("0,00"))
            {
                model.NKD = nkd;
            }
            _logger.LogDebug($"{DateTime.Now.ToString("HH:mm:ss:fffff")} DealsController HttpPost " +
                $"CreateFromText set NKD={model.NKD}");

            // тикер
            //string rawIsin = textSplitted[startPointer + 3];
            string rawSecCode = await _secCodesRepo.GetSecCodeByISIN(cancellationToken, textSplitted[startPointer + 3]);
            _logger.LogDebug($"{DateTime.Now.ToString("HH:mm:ss:fffff")} DealsController получили из репозитория={rawSecCode}");
            //проверить, что нам прислали действительно seccode а не ошибку
            if (!StaticData.SecCodes.Any(x => x.SecCode == rawSecCode))// если нет
            {
                //отправим найденное в ошибки
                ViewData["Message"] = rawSecCode;
                model.SecCode = "0";//сбросим присвоенную ошибку
            }
            else
            {
                model.SecCode = rawSecCode;
            }
            _logger.LogDebug($"{DateTime.Now.ToString("HH:mm:ss:fffff")} DealsController HttpPost " +
                $"CreateFromText set SecCode={model.SecCode}");

            return View("Create", model);
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
                return View();
            }
            if (model.SecBoard == 1 && model.NKD is not null)
            {
                _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} DealsController Create error " +
                    $"- NKD на акции недопустим");
                ViewData["Message"] = $"NKD на акции недопустим";
                return View();
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
                return View();
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
            _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} DealsController GET Delete id={id} called");

            DealModel deleteItem = await _repository.GetSingleDealById(cancellationToken, id);

            ViewData["SecBoard"] = @StaticData.SecBoards[StaticData.SecBoards
                .FindIndex(secb => secb.Id == deleteItem.SecBoard)].Name;

            return View(deleteItem);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(CancellationToken cancellationToken, DealModel model)
        {
            _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} DealsController HttpPost Delete called");

            string result = await _repository.DeleteSingleDeal(cancellationToken, model.Id);
            if (!result.Equals("1"))
            {
                ViewData["Message"] = $"Удаление не удалось.\r\n{result}";
                return View(model);
            }

            return RedirectToAction("Deals");
        }



        private string CleanString(string str)
        {
            if (str.EndsWith('.')) // убрать последнюю точку, чтобы RegEx не ругался
            {
                str = str.Substring(0, str.Length - 1);
            }
            str = str.Replace(",", "");

            str = str.Replace(".", ",");

            return str;
        }
    }
}
