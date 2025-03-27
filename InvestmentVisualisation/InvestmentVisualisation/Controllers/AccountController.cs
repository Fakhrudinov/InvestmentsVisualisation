using InvestmentVisualisation.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace InvestmentVisualisation.Controllers
{
	public class AccountController : Controller
	{
		private readonly ILogger<HomeController> _logger;
		private readonly UserManager<AppUser> _userManager;
		private readonly SignInManager<AppUser> _signInManager;


		public AccountController(
			ILogger<HomeController> logger,
			UserManager<AppUser> userManager,
			SignInManager<AppUser> signInManager)
		{
			_logger = logger;
			_userManager = userManager;
			_signInManager = signInManager;
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Login(AppUser appUser)
		{

			//login functionality  
			AppUser user = await _userManager.FindByNameAsync(appUser.UserName);
			if (user != null)
			{
				//sign in  
				Microsoft.AspNetCore.Identity.SignInResult signInResult = await _signInManager.PasswordSignInAsync
								   (user, appUser.Password, false, false);

				if (signInResult.Succeeded)
				{
					_logger.LogInformation($"{DateTime.Now.ToString("HH:mm:ss:fffff")} AuthController " +
						$"HttpPost Login succed for login={appUser.UserName}");

					return RedirectToAction("Index", "Home");
				}
			}

			_logger.LogWarning($"{DateTime.Now.ToString("HH:mm:ss:fffff")} AuthController " +
				$"HttpPost Login failed for login={appUser.UserName} pass={appUser.Password}");
			TempData["Error"] = "Not valid data";
			return RedirectToAction("Index", "Home");
		}

		public async Task<IActionResult> LogOut()
		{
			await _signInManager.SignOutAsync();
			return RedirectToAction("Index", "Home");
		}

		public IActionResult AccessDenied()
		{
			return View();
		}
	}
}
