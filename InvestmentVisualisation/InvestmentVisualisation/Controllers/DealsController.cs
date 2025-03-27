using DataAbstraction.Models;
using Microsoft.AspNetCore.Mvc;
using DataAbstraction.Interfaces;
using Microsoft.Extensions.Options;
using DataAbstraction.Models.Settings;
using DataAbstraction.Models.Deals;
using UserInputService;
using System.Text;
using Microsoft.AspNetCore.Authorization;

namespace InvestmentVisualisation.Controllers
{
	public class DealsController : Controller
    {
        private readonly ILogger<DealsController> _logger;
        private IMySqlDealsRepository _repository;
        private int _itemsAtPage;
        private IMySqlSecCodesRepository _secCodesRepo;
        private InputHelper _helper;
        private IInMemoryRepository _inMemoryRepository;

        public DealsController(
            ILogger<DealsController> logger, 
            IMySqlDealsRepository repository, 
            IOptions<PaginationSettings> paginationSettings,
            IMySqlSecCodesRepository secCodesRepo,
            InputHelper helper,
            IInMemoryRepository inMemoryRepository)
        {
            _logger = logger;
            _repository = repository;
            _itemsAtPage = paginationSettings.Value.PageItemsCount;
            _secCodesRepo = secCodesRepo;
            _helper = helper;
            _inMemoryRepository = inMemoryRepository;
        }

		[Authorize]
		public async Task<IActionResult> Deals(CancellationToken cancellationToken, int page = 1, string secCode = "")
        {
            _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} DealsController GET " +
                $"Deals called, page={page} secCode={secCode}");

            _inMemoryRepository.DeleteAllDeals();

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

		[Authorize(Roles = "Admin")]
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

		[Authorize(Roles = "Admin")]
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

		[Authorize(Roles = "Admin")]
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

            CreateDealsModel? model = await TryParseStringToDeaL(text, cancellationToken);

            return View("Create", model);
        }

		[Authorize(Roles = "Admin")]
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

		[Authorize(Roles = "Admin")]
		[HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateDealsModel model, CancellationToken cancellationToken)
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

		[Authorize(Roles = "Admin")]
		public async Task<IActionResult> Edit(CancellationToken cancellationToken, int id)
        {
            _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} DealsController GET Edit id={id} called");

            DealModel editedItem = await _repository.GetSingleDealById(cancellationToken, id);

            return View(editedItem);
        }

		[Authorize(Roles = "Admin")]
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

		[Authorize(Roles = "Admin")]
		public async Task<IActionResult> Delete(CancellationToken cancellationToken, int id)
        {
            _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} DealsController GET " +
                $"Delete id={id} called");

            DealModel deleteItem = await _repository.GetSingleDealById(cancellationToken, id);

            ViewData["SecBoard"] = @StaticData.SecBoards[StaticData.SecBoards
                .FindIndex(secb => secb.Id == deleteItem.SecBoard)].Name;

            return View(deleteItem);
        }

		[Authorize(Roles = "Admin")]
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

		[Authorize(Roles = "Admin")]
		public ActionResult CreateNewDeals(List<IndexedDealModel> ? model)
        {
            _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} DealsController GET " +
                $"CreateNewDeals called");

            if (model is null)
            {
                model = _inMemoryRepository.GetAllDeals();
            }
            else
            {
                _inMemoryRepository.DeleteAllDeals();
            }

            return View(model);
        }

		[Authorize(Roles = "Admin")]
		public IActionResult DeleteDealById(string id, CancellationToken cancellationToken)
        {
            _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} DealsController " +
                $"DeleteDealById id={id} called");

            _inMemoryRepository.DeleteSingleDealByStringId(id);

            List<IndexedDealModel> model = _inMemoryRepository.GetAllDeals();
            return View("CreateNewDeals", model);
        }

		[Authorize(Roles = "Admin")]
		[HttpPost]
        public IActionResult EditDealById(
            Guid id,
            DateTime date,
            string? price,
            int pieces,
            string? nkd,
            string? comission,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} DealsController " +
                $"EditDealById called");
            bool hasNoErrors = true;
            // validation -----------------------
            CreateDealsModel? checkExist = _inMemoryRepository.GetCreateDealsModelByGuid(id);
            // is exist?
            if (checkExist is null)
            {
                hasNoErrors = false;
                ViewData["Message"] = "Что-то пошло не так! Сделки с таким Guid не найдено в IInMemoryRepository";
            }
            // nkd only on obligations!
            if (hasNoErrors && checkExist is not null && checkExist.SecBoard != 2 && nkd is not null)
            {
                hasNoErrors = false;
                ViewData["Message"] = "NKD допустим только на облигации!";
            }
            // pieces > 0
            if (hasNoErrors && pieces <= 0)
            {
                hasNoErrors = false;
                ViewData["Message"] = "Количество штук введено некорректно. Должно быть целое число больше нуля";
            }
            // avprice not null && > 0
            if (hasNoErrors && price is null || (_helper.IsDecimal(price) && _helper.GetDecimalFromString(price) <= 0))
            {
                hasNoErrors = false;
                ViewData["Message"] = "Цена введена некорректно. Должно быть задано, как число больше нуля";
            }
            // nkd null || not null>0
            if (hasNoErrors && nkd is not null && (_helper.IsDecimal(nkd) && _helper.GetDecimalFromString(nkd) <= 0))
            {
                hasNoErrors = false;
                ViewData["Message"] = "НКД должно быть или как NULL, или должно быть задано, как число больше нуля";
            }
            // comission null || not null>0
            if (hasNoErrors && comission is not null && 
                (_helper.IsDecimal(comission) && _helper.GetDecimalFromString(comission) <= 0))
            {
                hasNoErrors = false;
                ViewData["Message"] = "Комиссия должна быть или как NULL, или должна быть задана, как число больше нуля";
            }
            // date not minValue
            if (hasNoErrors && date.Equals(DateTime.MinValue) || date > DateTime.Now)
            {
                hasNoErrors = false;
                ViewData["Message"] = "Дата введена некорректно. Дата должна быть задана и быть в прошлом.";
            }

            if (hasNoErrors)
            {
                IndexedDealModel editDeal = new IndexedDealModel
                {
                    Id = id,
                    Date = date,
                    Pieces = pieces,
                    AvPrice = price,
                    NKD = nkd,
                    Comission = comission,

                };
                _inMemoryRepository.EditSingleDeal(editDeal);
            }

            List<IndexedDealModel> model = _inMemoryRepository.GetAllDeals();
            return View("CreateNewDeals", model);
        }

		[Authorize(Roles = "Admin")]
		public async Task<IActionResult> AddSingleDealById(string id, CancellationToken cancellationToken)
        {
            _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} DealsController " +
                $"AddSingleDealById id={id} called");

            CreateDealsModel ? newDeal = _inMemoryRepository.GetCreateDealsModelById(id);
            if (newDeal is not null)
            {
                return await Create(newDeal, cancellationToken);
            }

            ViewData["Message"] = "В репозитории 'IInMemoryRepository' не удалось найти запись с ID " + id;
            List<IndexedDealModel> model = _inMemoryRepository.GetAllDeals();
            return View("CreateNewDeals", model);
        }

		[Authorize(Roles = "Admin")]
		[HttpPost]
        public async Task<IActionResult> CreateDealsFromText(string excelData, CancellationToken cancellationToken)
        {
            _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} DealsController HttpPost " +
                $"CreateDealsFromText called, excelData={excelData}");

            // new recognition, delete old dictionary
            _inMemoryRepository.DeleteAllDeals();

            if (excelData is not null && excelData.Contains("\r\n"))
            {
                string[] excelRawList = excelData.Split("\r\n");

                foreach (string excelString in excelRawList)
                {
                    if (excelString.Length == 0)
                    {
                        continue;
                    }

                    CreateDealsModel? newDeal = await TryParseStringToDeaL(excelString, cancellationToken);
                    if (newDeal is not null)
                    {
                        _inMemoryRepository.AddNewDeal(newDeal);
                    }
                }

                int dealsCount = _inMemoryRepository.ReturnDealsCount();
                if (dealsCount > 0 && excelRawList.Length - 1 != dealsCount)
                {
                    ViewData["Message"] = $"Распознано {dealsCount} строк из вставленных {excelRawList.Length - 1} строк";
                }
                else
                {
                    // null error message here after all ------------------------------------------
                }
            }

            List<IndexedDealModel> ? model = new List<IndexedDealModel>();
            if (_inMemoryRepository.ReturnDealsCount() == 0)
            {
                model = null;
                ViewData["Message"] = "Не удалось распознать ни одной сделки!";
            }
            else
            {
                model = _inMemoryRepository.GetAllDeals();

                //logica = summ same SecCode with same AvPrice
                for (int i = model.Count - 1; i >= 0; i--)
                {
                    foreach (IndexedDealModel dealTarget in model)
                    {
                        if (dealTarget.Id == model[i].Id)
                        {
                            break;
                        }

                        if (dealTarget.SecCode == model[i].SecCode && dealTarget.AvPrice == model[i].AvPrice)
                        {
                            _inMemoryRepository.CombineTwoDealsById(dealTarget.Id, model[i].Id);
                            break;
                        }
                    }
                }
            }

            model = _inMemoryRepository.GetAllDeals();

            return View("CreateNewDeals", model);
        }

		[Authorize(Roles = "Admin")]
		public async Task<IActionResult> AddAllDealsFromInMemoryRepository(CancellationToken cancellationToken)
        {
            _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} DealsController " +
                $"AddAllDealsFromInMemoryRepository called");

            List<IndexedDealModel> model = _inMemoryRepository.GetAllDeals();

            if (model is not null && model.Count > 0)
            {
                string result = await _repository.CreateNewDealsFromList(cancellationToken, model);
                if (!result.Equals(_inMemoryRepository.ReturnDealsCount().ToString()))
                {
                    _logger.LogWarning($"{DateTime.Now.ToString("HH:mm:ss:fffff")} DealsController " +
                        $"AddAllDealsFromInMemoryRepository action CreateNewDealsFromList error: \r\n{result}");

                    ViewData["Message"] = $"Добавление не удалось. \r\n{result}";
                    return View("CreateNewDeals", model);
                }

                return RedirectToAction("Deals");
            }

            ViewData["Message"] = "Что-то не так. Репозиторий сделок пуст, нечего добавлять.";
            return View("CreateNewDeals", model);
        }

        private async Task<CreateDealsModel?> TryParseStringToDeaL(string text, CancellationToken cancellationToken)
        {
			// pdf format:
			// "11989899733 57858315808 Покупка ЦБ на бирже
			// 10.01.2025 15:06:41 Банк СПб, ПАО ао03, 10300436B RU0009100945 1.00 20.00 366.15 RUB RUB 7 323.00 1.10
			// 13.01.2025 13.01.2025 13.01.2025 13.01.2025 ПАО Московская Биржа НКО АО НРД"

			/*
             * строка длинная
             * содержит Покупка
             * от неё отсчет по дате
             * есть запись 12 символов начинающаяся с RU
             * от нее отсчет по цене количеству и комиссиям== комиссии это всё между RUB(вторым) и датой поставки
             * ПРОВЕРИТЬ ЧТО ЗА ХРЕНЬ С РАСПОЛОЖЕНИЕМ второго РУБ - он точно перед объемом сделки?
             */


			if (text is null || !text.Contains("\t"))
            {
                ViewData["Message"] = "Чтение строки не удалось, строка пустая или не содержит табуляций-разделителей: " + 
                    text;
                return null;
            }

            string[]? textSplitted = _helper.ReturnSplittedArray(text);

            if (textSplitted is null || textSplitted.Length < 13)
            {
                ViewData["Message"] = "Чтение строки не удалось, " +
                    "получено менее 13 элементов (12х табуляций-разделителей) в строке: " + text;
                return null;
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
                    //return View("Create", new CreateDealsModel());
                    return null;
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
                    $"TryParseStringToDeaL set date={model.Date} from {textSplitted[startPointer + 1]}");
            }
            else
            {
                stringBuilderErrors.Append("Date not recognized! ");
                _logger.LogWarning($"{DateTime.Now.ToString("HH:mm:ss:fffff")} DealsController HttpPost " +
                    $"TryParseStringToDeaL Date not recognized from {textSplitted[startPointer + 1]}");

                // just set yesterday
                model.Date = DateTime.Now.AddDays(-1);
            }

            // количество // 60,000.00
            if (_helper.IsInt32Correct(textSplitted[startPointer + 5]))
            {
                model.Pieces = _helper.GetInt32FromString(textSplitted[startPointer + 5]);
                _logger.LogDebug($"{DateTime.Now.ToString("HH:mm:ss:fffff")} DealsController HttpPost " +
                    $"TryParseStringToDeaL set Pieces={model.Pieces} from {textSplitted[startPointer + 5]}");
            }
            else
            {
                stringBuilderErrors.Append("Pieces not recognized! ");
                _logger.LogWarning($"{DateTime.Now.ToString("HH:mm:ss:fffff")} DealsController HttpPost " +
                    $"TryParseStringToDeaL Pieces not recognized from {textSplitted[startPointer + 5]}");
            }

            // цена // 4,583.5 или 0.08708
            if (_helper.IsDecimal(textSplitted[startPointer + 6]))
            {
                model.AvPrice = _helper.CleanPossibleNumber(textSplitted[startPointer + 6]);
                _logger.LogDebug($"{DateTime.Now.ToString("HH:mm:ss:fffff")} DealsController HttpPost " +
                    $"TryParseStringToDeaL set AvPrice={model.AvPrice} from {textSplitted[startPointer + 6]}");
            }
            else
            {
                stringBuilderErrors.Append("AvPrice not recognized! ");
                _logger.LogWarning($"{DateTime.Now.ToString("HH:mm:ss:fffff")} DealsController HttpPost " +
                    $"TryParseStringToDeaL AvPrice not recognized from {textSplitted[startPointer + 6]}");
            }

            //комиссия = комиссия биржи + комиссия брокера 1.79	0.24
            if (_helper.IsDecimal(textSplitted[startPointer + 11]) && _helper.IsDecimal(textSplitted[startPointer + 12]))
            {
                decimal moexComiss = _helper.GetDecimalFromString(textSplitted[startPointer + 11]);
                decimal brokComiss = _helper.GetDecimalFromString(textSplitted[startPointer + 12]);
                model.Comission = (moexComiss + brokComiss).ToString();

                if (model.Comission.Equals("0"))
                {
                    model.Comission = null;
                }

                _logger.LogDebug($"{DateTime.Now.ToString("HH:mm:ss:fffff")} DealsController HttpPost " +
                    $"TryParseStringToDeaL set Comission={model.Comission} from " +
                    $"{textSplitted[startPointer + 11]}({moexComiss}) + {textSplitted[startPointer + 12]}({brokComiss})");
            }
            else
            {
                stringBuilderErrors.Append("Comission not recognized! ");
                _logger.LogWarning($"{DateTime.Now.ToString("HH:mm:ss:fffff")} DealsController HttpPost " +
                    $"TryParseStringToDeaL Comission not recognized from " +
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
                        $"TryParseStringToDeaL set NKD={model.NKD} from {textSplitted[startPointer + 9]}");
                }
                else
                {
                    stringBuilderErrors.Append("NKD not recognized! ");
                    _logger.LogWarning($"{DateTime.Now.ToString("HH:mm:ss:fffff")} DealsController HttpPost " +
                        $"TryParseStringToDeaL NKD is zero value or not recognized from {textSplitted[startPointer + 9]}");
                }
            }


            // тикер
            string rawSecCode = await _secCodesRepo.GetSecCodeByISIN(cancellationToken, textSplitted[startPointer + 3]);
            _logger.LogDebug($"{DateTime.Now.ToString("HH:mm:ss:fffff")} DealsController получили из " +
                $"репозитория={rawSecCode}");
            //проверить, что нам прислали действительно seccode а не ошибку
            if (!StaticData.SecCodes.Any(x => x.SecCode == rawSecCode))// если нет
            {
                _logger.LogWarning($"{DateTime.Now.ToString("HH:mm:ss:fffff")} DealsController HttpPost " +
                    $"TryParseStringToDeaL ISIN not recognized from {textSplitted[startPointer + 3]}");

                //отправим найденное в ошибки
                stringBuilderErrors.Append("ISIN не распознан! Не отправляйте сделки в базу данных!");
                model.SecCode = "0";//сбросим присвоенную ошибку
            }
            else
            {
                model.SecCode = rawSecCode;
            }
            _logger.LogDebug($"{DateTime.Now.ToString("HH:mm:ss:fffff")} DealsController HttpPost " +
                $"TryParseStringToDeaL set SecCode={model.SecCode}");

            // secBord
            model.SecBoard = StaticData.SecCodes[StaticData.SecCodes.FindIndex(x => x.SecCode == model.SecCode)].SecBoard;
            if (model.SecCode.Equals("0"))
            {
                _logger.LogWarning($"{DateTime.Now.ToString("HH:mm:ss:fffff")} DealsController TryParseStringToDeaL error " +
                    $"- deals SecBoard not recognized - now it is '0' !!! Possible problem in database table Deals");
                stringBuilderErrors.Append($"Secbord(0=деньги) не найден или найден некорректно! " +
                    $"Не отправляйте сделки в базу данных!");
            }

            ViewData["Message"] = stringBuilderErrors.ToString();

            return model;
        }
    }
}
