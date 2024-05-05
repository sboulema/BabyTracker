using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.Cookies;
using BabyTracker.Models.ViewModels;
using BabyTracker.Services;
using BabyTracker.Models.Account;
using System.Security.Claims;
using BabyTracker.Constants;

namespace BabyTracker.Controllers;

[Route("[controller]")]
public class AccountController(IAccountService accountService) : Controller
{
	[HttpGet("[action]")]
	public IActionResult Login(string returnUrl = "")
	{
		ViewBag.Theme = ThemesEnum.Auto;
		ViewBag.ReturnUrl = returnUrl;

		return View(new LoginViewModel());
	}

	[HttpPost("[action]")]
	public async Task<IActionResult> Login(LoginViewModel model, string returnUrl = "")
	{
		var claimsPrincipal = await accountService.Login(model);

		if (claimsPrincipal == null)
		{
			TempData["notificationMessage"] = "Username and/or password are incorrect.";
			TempData["notificationType"] = "danger";

			return RedirectToAction("Login");
		}

		await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, claimsPrincipal);

		return RedirectToLocal(returnUrl);
	}

	[HttpGet("[action]")]
	public IActionResult Register()
	{
		ViewBag.Theme = ThemesEnum.Auto;

		return View(new LoginViewModel());
	}

	[HttpPost("[action]")]
	public async Task<IActionResult> Register(LoginViewModel model, string returnUrl = "")
	{
		await accountService.Register(model);

		return await Login(model, returnUrl);
	}

	[HttpGet("[action]")]
	public IActionResult ResetPassword()
	{
		ViewBag.Theme = ThemesEnum.Auto;

		return View(new LoginViewModel());
	}

	[HttpPost("[action]")]
	public async Task<IActionResult> ResetPassword(LoginViewModel model)
	{
		var result = await accountService.ResetPassword(model);

		TempData["notificationMessage"] = result;
		TempData["notificationType"] = "success";

		return RedirectToAction("Login");
	}

	[Authorize]
	[HttpGet("[action]")]
	public async Task Logout()
	{
		await HttpContext.SignOutAsync("Auth0", new AuthenticationProperties
		{
			RedirectUri = Url.Action("Index", "Home")
		});
		await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
	}

	[Authorize]
	[HttpGet("[action]")]
	public async Task<IActionResult> Profile()
	{
		var userMetaData = await accountService.GetUserMetaData(User);

		var model = new ProfileViewModel
		{
			EnableMemoriesEmail = userMetaData?.EnableMemoriesEmail ?? false,
			MemoriesAddresses = userMetaData?.MemoriesAddresses ?? string.Empty,
			ShareList = userMetaData?.ShareList ?? string.Empty,
			FontSize = userMetaData?.FontSize ?? 6,
			NickName = User.FindFirstValue("nickname") ?? string.Empty,
			ProfileImageUrl = User.FindFirstValue("picture") ?? string.Empty,
			UserId = User.FindFirstValue("userId") ?? string.Empty,
			Theme = userMetaData?.Theme ?? ThemesEnum.Auto,
			UseFullCardImages = userMetaData?.UseFullCardImages ?? false,
			UseCards = userMetaData?.UseCards ?? false,
		};

		ViewBag.Theme = userMetaData?.Theme;

		return View(model);
	}

	[Authorize]
	[HttpPost("[action]")]
	public async Task<IActionResult> ProfileSave(ProfileViewModel viewModel)
	{
		var userMetaDate = new UserMetaData
		{
			EnableMemoriesEmail = viewModel.EnableMemoriesEmail,
			MemoriesAddresses = viewModel.MemoriesAddresses,
			ShareList = viewModel.ShareList,
			FontSize = viewModel.FontSize,
			Theme = viewModel.Theme,
			UseFullCardImages = viewModel.UseFullCardImages,
			UseCards = viewModel.UseCards,
		};

		var success = await accountService.SaveUserMetaData(User, userMetaDate);

		TempData["notificationMessage"] = "Profile settings saved.";
		TempData["notificationType"] = success ? "success" : "danger";

		return RedirectToAction("Profile");
	}

	private IActionResult RedirectToLocal(string returnUrl)
	{
		if (Url.IsLocalUrl(returnUrl))
		{
			return Redirect(returnUrl);
		}
		else
		{
			return RedirectToAction("Index", "Home");
		}
	}
}