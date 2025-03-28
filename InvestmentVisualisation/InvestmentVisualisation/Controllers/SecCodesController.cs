﻿using DataAbstraction.Interfaces;
using DataAbstraction.Models;
using DataAbstraction.Models.Settings;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using DataAbstraction.Models.SecCodes;
using Microsoft.AspNetCore.Authorization;

namespace InvestmentVisualisation.Controllers
{
    public class SecCodesController : Controller
    {
        private readonly ILogger<SecCodesController> _logger;
        private IMySqlSecCodesRepository _repository;
        private int _itemsAtPage;

        public SecCodesController(
            ILogger<SecCodesController> logger, 
            IMySqlSecCodesRepository repository, 
            IOptions<PaginationSettings> paginationSettings)
        {
            _logger = logger;
            _repository = repository;
            _itemsAtPage = paginationSettings.Value.PageItemsCount;
        }

        [AllowAnonymous]
        public async Task<IActionResult> Index(CancellationToken cancellationToken, int page = 1, bool showOnlyActive = false)
        {
            _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} SecCodesController GET " +
                $"Index called, page={page}, showOnlyActive={showOnlyActive}");

            int count = 0;
            if (showOnlyActive)
            {
				count = await _repository.GetOnlyActiveSecCodesCount(cancellationToken);
			}
            else
            {
                count = await _repository.GetSecCodesCount(cancellationToken);

			}
            _logger.LogDebug($"{DateTime.Now.ToString("HH:mm:ss:fffff")} SecCodesController SecCodes table size={count}");

            SecCodesWithPaginations dealsWithPaginations = new SecCodesWithPaginations();

            dealsWithPaginations.PageViewModel = new PaginationPageViewModel(
                count,
                page,
                _itemsAtPage);

			if (showOnlyActive)
			{
				dealsWithPaginations.SecCodes = await _repository.GetPageFromOnlyActiveSecCodes(
					cancellationToken,
					_itemsAtPage,
					(page - 1) * _itemsAtPage);
			}
            else
            {
				dealsWithPaginations.SecCodes = await _repository.GetPageFromSecCodes(
                    cancellationToken, 
                    _itemsAtPage, 
                    (page - 1) * _itemsAtPage);
			}

            ViewBag.Active = showOnlyActive;
			return View(dealsWithPaginations);
        }

		[Authorize(Roles = "Admin")]
		public ActionResult Create()
        {
            _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} SecCodesController GET Create called");

            SecCodeInfo model = new SecCodeInfo();
            return View(model);
        }
		[Authorize(Roles = "Admin")]
		[HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CancellationToken cancellationToken, SecCodeInfo model)
        {
            _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} SecCodesController POST Create called");

            string result = await _repository.CreateNewSecCode(cancellationToken, model);
            if (!result.Equals("1"))
            {
                ViewData["Message"] = $"Добавление не удалось. \r\n{result}";
                return View();
            }

            _repository.RenewStaticSecCodesList(cancellationToken);

            return RedirectToAction("Index");
        }

		[Authorize(Roles = "Admin")]
		public async Task<IActionResult> Edit(CancellationToken cancellationToken, string secCode)
        {
            _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} SecCodesController GET " +
                $"Edit secCode={secCode} called");

            SecCodeInfo editedItem = await _repository.GetSingleSecCodeBySecCode(cancellationToken, secCode);

            return View(editedItem);
        }

		[Authorize(Roles = "Admin")]
		[HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(CancellationToken cancellationToken, SecCodeInfo model)
        {
            _logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} SecCodesController HttpPost Edit called");

            string result = await _repository.EditSingleSecCode(cancellationToken, model);
            if (!result.Equals("1"))
            {
                ViewData["Message"] = $"Редактирование не удалось.\r\n{result}";
                return View(model);
            }

            _repository.RenewStaticSecCodesList(cancellationToken);

            return RedirectToAction("Index");
        }
    }
}
