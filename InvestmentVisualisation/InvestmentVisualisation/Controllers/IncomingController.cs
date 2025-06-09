using DataAbstraction.Interfaces;
using DataAbstraction.Models;
using DataAbstraction.Models.Incoming;
using DataAbstraction.Models.Settings;
using InvestmentVisualisation.Models;
using Microsoft.AspNetCore.Authorization;
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
		private IInMemoryRepository _inMemoryRepository;

		public IncomingController(
            ILogger<IncomingController> logger,
            IMySqlIncomingRepository repository,
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

		[Authorize(Roles = "Admin")]
		public IActionResult CreateIncoming()
        {
            _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} IncomingController GET CreateIncoming called");

            CreateIncomingModel model = new CreateIncomingModel();
            model.Date = DateTime.Now.AddDays(-1);// обычно вношу за вчера
            model.Category = 1;

            return View(model);
        }
		[Authorize(Roles = "Admin")]
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

		[Authorize(Roles = "Admin")]
		[HttpPost]
        public async Task<IActionResult> CreateFromText(CancellationToken cancellationToken, string text)
        {
            _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} IncomingController HttpPost " +
                $"CreateFromText called, text={text}");

			CreateIncomingModel ? model = await TryParseStringToIncoming(text, cancellationToken);
			if (model is not null)
			{
				return View("CreateIncoming", model);
			}

			ViewData["Message"] = $"Input data not recognized;<br />" + ViewData["Message"];
			return View("CreateIncoming");
        }


		[Authorize(Roles = "Admin")]
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

		[Authorize(Roles = "Admin")]
		public async Task<IActionResult> Edit(CancellationToken cancellationToken, int id)
        {
            _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} IncomingController GET Edit id={id} called");

            IncomingModel editedItem = await _repository.GetSingleIncomingById(cancellationToken, id);

            return View(editedItem);
        }

		[Authorize(Roles = "Admin")]
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

		[Authorize(Roles = "Admin")]
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

		[Authorize(Roles = "Admin")]
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

        [AllowAnonymous]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} IncomingController GET Error called");
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        [AllowAnonymous]
        public IActionResult HelpPage()
        {
            _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} IncomingController GET HelpPage called");

            return View();
        }



		[Authorize(Roles = "Admin")]
		public ActionResult CreateSeveralIncomings(List<IndexedIncomingModel>? model)
		{
			_logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} IncomingController GET " +
				$"CreateSeveralIncomings called");

			if (model is null)
			{
				model = _inMemoryRepository.GetAllIncomings();
			}
			else
			{
				_inMemoryRepository.DeleteAllIncomings();
			}

			return View(model);
		}

		[Authorize(Roles = "Admin")]
		public IActionResult InMemoryRepositoryDeleteIncomingById(string id, CancellationToken cancellationToken)
		{
			_logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} IncomingController " +
				$"InMemoryRepositoryDeleteIncomingById id={id} called");

			_inMemoryRepository.DeleteSingleIncomingByStringId(id);

			List<IndexedIncomingModel> ? model = _inMemoryRepository.GetAllIncomings();
			return View("CreateSeveralIncomings", model);
		}

		//[Authorize(Roles = "Admin")]
		[AllowAnonymous]
		[HttpPost]
		public IActionResult EditIncomingById(
			Guid id,//
			DateTime date,//
			string secCode,//
			int secBoard,//
			string? comission,//
			int category,//
			string? value,
			CancellationToken cancellationToken)
		{
			_logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} IncomingController " +
				$"EditIncomingById called");
			bool hasNoErrors = true;
			// validation -----------------------
			CreateIncomingModel? checkExist = _inMemoryRepository.GetCreateIncomingModelByGuid(id);
			// is exist?
			if (checkExist is null)
			{
				hasNoErrors = false;
				ViewData["Message"] = "Что-то пошло не так! Сделки с таким Guid не найдено в IInMemoryRepository";
			}
			// avprice not null && > 0
			if (hasNoErrors && value is null || (_helper.IsDecimal(value) && _helper.GetDecimalFromString(value) <= 0))
			{
				hasNoErrors = false;
				ViewData["Message"] = "Объем введен некорректно. Должно быть задано, как число больше нуля";
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
				IndexedIncomingModel editModel = new IndexedIncomingModel
				{
					Id = id,
					Date = date,
					Value = value,
					Category = category,
					SecBoard = secBoard,
					SecCode = secCode,
					Comission = comission

				};
				_inMemoryRepository.EditSingleIncoming(editModel);
			}

			List<IndexedIncomingModel>? model = _inMemoryRepository.GetAllIncomings();
			return View("CreateSeveralIncomings", model);
		}

		[Authorize(Roles = "Admin")]
		public async Task<IActionResult> AddSingleIncomingById(string id, CancellationToken cancellationToken)
		{
			_logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} IncomingController " +
				$"AddSingleIncomingById id={id} called");

			CreateIncomingModel? newIncoming = _inMemoryRepository.GetCreateIncomingModelById(id);
			if (newIncoming is not null)
			{
				return await CreateIncoming(cancellationToken, newIncoming);
			}

			ViewData["Message"] = "В репозитории 'InMemoryRepository' не удалось найти запись IncomingModel с ID " + id;
			List<IndexedIncomingModel>? model = _inMemoryRepository.GetAllIncomings();
			return View("CreateSeveralIncomings", model);
		}

		[Authorize(Roles = "Admin")]
		[HttpPost]
		public async Task<IActionResult> CreateIncomingsFromText(string excelData, CancellationToken cancellationToken)
		{
			_logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} IncomingController HttpPost " +
				$"CreateIncomingsFromText called, excelData={excelData}");

			// new recognition, delete old dictionary
			_inMemoryRepository.DeleteAllIncomings();

			if (excelData is not null && excelData.Contains("\r\n"))
			{
				string[] excelRawList = excelData.Split("\r\n");

				foreach (string excelString in excelRawList)
				{
					if (excelString.Length == 0)
					{
						continue;
					}

					CreateIncomingModel? newIncoming = await TryParseStringToIncoming(excelString, cancellationToken);
					if (newIncoming is not null)
					{
						_inMemoryRepository.AddNewIncoming(newIncoming);
					}
				}

				int incomingsCount = _inMemoryRepository.ReturnIncomingsCount();
				if (incomingsCount > 0 && excelRawList.Length - 1 != incomingsCount)
				{
					ViewData["Message"] = $"Распознано {incomingsCount} строк из вставленных {excelRawList.Length - 1} " +
						$"строк;<br />" + ViewData["Message"];
				}
				else
				{
					// null error message here after all ------------------------------------------
				}
			}

			List<IndexedIncomingModel>? model = new List<IndexedIncomingModel>();
			if (_inMemoryRepository.ReturnIncomingsCount() == 0)
			{
				model = null;
				ViewData["Message"] = "Не удалось распознать ни одной строки!";
			}

			model = _inMemoryRepository.GetAllIncomings();

			return View("CreateSeveralIncomings", model);
		}

		[Authorize(Roles = "Admin")]
		public async Task<IActionResult> AddAllIncomingsFromInMemoryRepository(CancellationToken cancellationToken)
		{
			_logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} IncomingController " +
				$"AddAllIncomingsFromInMemoryRepository called");

			List<IndexedIncomingModel>? model = _inMemoryRepository.GetAllIncomings();

			if (model is not null && model.Count > 0)
			{
				string result = await _repository.CreateNewIncomingsFromList(cancellationToken, model);
				if (!result.Equals(_inMemoryRepository.ReturnIncomingsCount().ToString()))
				{
					_logger.LogWarning($"{DateTime.Now.ToString("HH:mm:ss:fffff")} IncomingController " +
						$"AddAllIncomingsFromInMemoryRepository action CreateNewIncomingsFromList error: \r\n{result}");

					ViewData["Message"] = $"Добавление не удалось. \r\n{result}";
					return View("CreateSeveralIncomings", model);
				}

				return RedirectToAction("Incoming");
			}

			ViewData["Message"] = "Что-то не так. Репозиторий входящих пуст, нечего добавлять.";
			return View("CreateSeveralIncomings", model);
		}



		private async Task<CreateIncomingModel?> TryParseStringToIncoming(
			string text, 			 
			CancellationToken cancellationToken)
		{

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
				ViewData["Message"] = "Чтение строки не удалось, строка пустая или не содержит " +
					"табуляций-разделителей: " + text+ ";<br />" + ViewData["Message"];
				return null;
			}

			string[]? textSplitted = _helper.ReturnSplittedArray(text);

			if (textSplitted is null || textSplitted.Length < 2)
			{
				ViewData["Message"] = "Чтение строки не удалось, " +
					"получено менее 3 значимых элементов (2х табуляций-разделителей) в строке: " + text +
					";<br />" + ViewData["Message"]; ;
				return null;
			}

			CreateIncomingModel model = new CreateIncomingModel();
			// тип операции
			if (textSplitted[0].Contains("Выплата дивидендов") || textSplitted[0].Contains("Выплата процентного дохода"))
			{
				_logger.LogDebug($"{DateTime.Now.ToString("HH:mm:ss:fffff")} IncomingController " +
					$"TryParseStringToIncoming set category=1 by 'Выплата дивидендов' ");
				model.Category = 1;
			}
			else if (textSplitted[0].Contains("Поступление ДС клиента"))
			{
				_logger.LogDebug($"{DateTime.Now.ToString("HH:mm:ss:fffff")} IncomingController " +
					$"TryParseStringToIncoming set category=0 seccode=0 by 'Поступление ДС клиента' ");
				model.Category = 0;
				model.SecCode = "0";
			}
			else if (textSplitted[0].Contains("Депозитарная комиссия"))
			{
				_logger.LogDebug($"{DateTime.Now.ToString("HH:mm:ss:fffff")} IncomingController " +
					$"TryParseStringToIncoming set category=3 seccode=0 by 'Депозитарная комиссия' ");
				model.Category = 3;
				model.SecCode = "0";
			}
			else if (textSplitted[0].Contains("Оборот по погашению"))
			{
				_logger.LogDebug($"{DateTime.Now.ToString("HH:mm:ss:fffff")} IncomingController " +
					$"TryParseStringToIncoming set category=2 by 'Оборот по погашению' ");
				model.Category = 2;
			}
			else if (textSplitted[0].Contains("Погашение ЦБ") || textSplitted[1].Contains("Погашение ЦБ"))
			{
				_logger.LogDebug($"{DateTime.Now.ToString("HH:mm:ss:fffff")} IncomingController " +
					$"TryParseStringToIncoming set category=4 by 'Погашение ЦБ' ");
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
						_logger.LogWarning($"{DateTime.Now.ToString("HH:mm:ss:fffff")} IncomingController " +
							$"TryParseStringToIncoming Date recognizing error for '{textSplitted[2]}', " +
							$"lenght of cell less then 12");
						// just set yesterday
						model.Date = DateTime.Now.AddDays(-1);
						model.IsRecognized = $"DATE '{textSplitted[2]}' not recognized, lenght of cell less then 12";
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
					model.IsRecognized = "Money data not found";
				}


				// 4 - ISIN == textSplitted 3 or 4, contain directly ISIN
				if (textSplitted.Length >=4 && textSplitted[3].Length > 0 &&
					StaticData.SecCodes.Any(x => x.SecCode == textSplitted[3]))
				{
					_logger.LogDebug($"{DateTime.Now.ToString("HH:mm:ss:fffff")} IncomingController " +
						$"TryParseStringToIncoming try set SecCode by {textSplitted[3]}");
					model.SecCode = StaticData.SecCodes.Find(x => x.SecCode == textSplitted[3]).SecCode;
				}
				if (textSplitted.Length >=5 && textSplitted[4].Length > 0 &&
					StaticData.SecCodes.Any(x => x.SecCode == textSplitted[4]))
				{
					_logger.LogDebug($"{DateTime.Now.ToString("HH:mm:ss:fffff")} IncomingController " +
						$"TryParseStringToIncoming try set SecCode by {textSplitted[4]}");
					model.SecCode = StaticData.SecCodes.Find(x => x.SecCode == textSplitted[4]).SecCode;
				}

				if (model.Category == 2 && (model.SecCode is null || model.SecCode.Equals("0")))
				{
					model.IsRecognized = "Check/Input tiker manually!";
				}
				return model;
			}
			else
			{
				// some unrecognizable shit
				return null;
			}



			bool isDataFormatCorrectCommon = _helper.IsDataFormatCorrect(textSplitted[1]);
			if (isDataFormatCorrectCommon)
			{
				model.Date = DateTime.Parse(textSplitted[1]);
			}
			else
			{
				_logger.LogWarning($"{DateTime.Now.ToString("HH:mm:ss:fffff")} IncomingController " +
					$"TryParseStringToIncoming Date recognizing error for '{textSplitted[1]}'");
				model.IsRecognized = $"DATE '{textSplitted[1]}' not recognized;";

				// just set yesterday
				model.Date = DateTime.Now.AddDays(-1);
			}
			_logger.LogDebug($"{DateTime.Now.ToString("HH:mm:ss:fffff")} IncomingController " +
				$"TryParseStringToIncoming set Date as: {model.Date}");


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
				_logger.LogWarning($"{DateTime.Now.ToString("HH:mm:ss:fffff")} IncomingController " +
					$"TryParseStringToIncoming number recognizing error for cells 2 and 3: " +
					$"'{textSplitted[2]}' or '{textSplitted[3]}'");
				model.IsRecognized = $"cell 2='{textSplitted[2]}' or cell 3='{textSplitted[3]}' " +
					$"not recognized as number;";
				model.Value = "0";
			}


			//ISIN to seccode
			if (model.Category != 0 && model.Category != 3) // это не зачисленные мной деньги или не списанная брок комиссия
			{
				if (model.Category == 2) // тут нет ISIN, надо запрашивать последний добавленный incoming и брать seccode оттуда 
				{
					_logger.LogDebug($"{DateTime.Now.ToString("HH:mm:ss:fffff")} IncomingController " +
						$"TryParseStringToIncoming request last incoming. Reason: Category == 2");
					model.SecCode = await _repository.GetSecCodeFromLastRecord(cancellationToken);
				}
				else // попробуем найти заполненный ISIN 
				{
					if (textSplitted.Length >=4 && textSplitted[3].Length > 0 && textSplitted[3].Contains("ISIN-"))
					{
						_logger.LogDebug($"{DateTime.Now.ToString("HH:mm:ss:fffff")} IncomingController " +
							$"TryParseStringToIncoming try set SecCode by {textSplitted[3]}");
						model.SecCode = await _helper.GetSecCodeByISIN(cancellationToken, textSplitted[3]);
					}
					if (textSplitted.Length >=5 && textSplitted[4].Length > 0 && textSplitted[4].Contains("ISIN-"))
					{
						_logger.LogDebug($"{DateTime.Now.ToString("HH:mm:ss:fffff")} IncomingController " +
							$"TryParseStringToIncoming try set SecCode by {textSplitted[4]}");
						model.SecCode = await _helper.GetSecCodeByISIN(cancellationToken, textSplitted[4]);
					}
				}
			}

			if (model.SecCode is null)
			{
				model.IsRecognized = "Check/Input tiker manually!";
			}
			//проверить, что нам прислали действительно seccode а не ошибку
			else if (!StaticData.SecCodes.Any(x => x.SecCode == model.SecCode))// если нет
			{
				//отправим найденное в ошибки
				model.IsRecognized = model.SecCode;
				model.SecCode = "0";//сбросим присвоенную ошибку
									// return here to show error
				return null;
			}

			if (model.SecCode is not null)
			{
				model.SecBoard = StaticData
				.SecCodes[StaticData.SecCodes.FindIndex(x => x.SecCode == model.SecCode)].SecBoard;
			}		


			return model;
		}
	}
}