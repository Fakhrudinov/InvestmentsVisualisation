using DataAbstraction.Interfaces;
using DataAbstraction.Models;
using DataAbstraction.Models.Deals;
using DataAbstraction.Models.ExtraordinaryBuy;
using DataAbstraction.Models.Settings;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using UserInputService;

namespace InvestmentVisualisation.Controllers
{
	public class ExtraordinaryBuyController : Controller
	{
		private readonly ILogger<ExtraordinaryBuyController> _logger;
		private IMySqlExtraordinaryBuyRepository _repository;
		private int _itemsAtPage;
		//private IMySqlSecCodesRepository _secCodesRepo;
		//private InputHelper _helper;
		//private IInMemoryRepository _inMemoryRepository;

		public ExtraordinaryBuyController(
			ILogger<ExtraordinaryBuyController> logger,
			IMySqlExtraordinaryBuyRepository repository,
			IOptions<PaginationSettings> paginationSettings
			//,IMySqlSecCodesRepository secCodesRepo
			//,InputHelper helper
			//,IInMemoryRepository inMemoryRepository
			)
		{
			_logger = logger;
			_repository = repository;
			_itemsAtPage = paginationSettings.Value.PageItemsCount;
			//_secCodesRepo = secCodesRepo;
			//_helper = helper;
			//_inMemoryRepository = inMemoryRepository;
		}


		public async Task<IActionResult> Index(CancellationToken cancellationToken, int page = 1)
		{
			_logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} ExtraordinaryBuyController GET " +
				$"Index called, page={page}");

			int count = await _repository.GetExtraordinaryBuyCount(cancellationToken);
			_logger.LogDebug($"{DateTime.Now.ToString("HH:mm:ss:fffff")} ExtraordinaryBuyController table size={count}");

			if (count == 0)
			{
				return View();
			}

			ExtraordinaryBuyListWithPaginations extraordinaryBuyListWithPaginations = new ExtraordinaryBuyListWithPaginations();

			extraordinaryBuyListWithPaginations.PageViewModel = new PaginationPageViewModel(
			count,
			page,
			_itemsAtPage);

			extraordinaryBuyListWithPaginations.ExtraordinaryBuy = await _repository
				.GetPageFromExtraordinaryBuy(cancellationToken, _itemsAtPage, (page - 1) * _itemsAtPage);

			return View(extraordinaryBuyListWithPaginations);
		}

		//[Authorize(Roles = "Admin")]
		public async Task<IActionResult> Delete(
			CancellationToken cancellationToken,
			string seccode)
		{
			_logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} ExtraordinaryBuyController Delete " +
				$"seccode={seccode} called");

			await _repository.Delete(cancellationToken, seccode);

			return RedirectToAction("Index");
		}

		//[Authorize(Roles = "Admin")]
		[HttpPost]
		public async Task<IActionResult> CreateNew(
			CancellationToken cancellationToken,
			string seccode,
			int volume,
			string ? description)
		{
			_logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} ExtraordinaryBuyController CreateNew " +
				$"seccode={seccode} volume={volume} description='{description}' called");

			await _repository.CreateNew(cancellationToken, seccode, volume, description);

			return RedirectToAction("Index");
		}

		//[Authorize(Roles = "Admin")]
		[HttpPost]
		public async Task<IActionResult> Edit(
			CancellationToken cancellationToken,
			string seccode,
			int volume,
			string description)
		{
			_logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} ExtraordinaryBuyController Edit " +
				$"seccode={seccode} volume={volume} description='{description}' called");

			await _repository.Edit(cancellationToken, seccode, volume, description);

			return RedirectToAction("Index");
		}
	}
}
