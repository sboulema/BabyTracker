using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
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
public class AccountController(IAccountService accountService,
	IImportService importService,
	ILogger<AccountController> logger) : Controller
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

		var activeUserId = claimsPrincipal.FindFirstValue("activeUserId");
		if (!string.IsNullOrEmpty(activeUserId))
		{
			try
			{
				await importService.EnsureDataCloneEncrypted(activeUserId);
			}
			catch (Exception e)
			{
				// Fail closed: do not sign in to a clone that may still hold plaintext files.
				logger.LogError(e, "Encrypting the data clone of user {UserId} failed", activeUserId);

				TempData["notificationMessage"] = "Your data could not be secured right now. Please try again later.";
				TempData["notificationType"] = "danger";

				return RedirectToAction("Login");
			}
		}

		await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, claimsPrincipal, new AuthenticationProperties
		{
			IsPersistent = model.RememberMe
		});

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
        var result = await accountService.Register(model);

        if (!result.Success)
        {
            TempData["notificationMessage"] = result.Error;
            TempData["notificationType"] = "danger";

            return RedirectToAction("Register");
        }

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
    [HttpPost("[action]")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        return RedirectToAction("Index", "Home");
    }

    [Authorize]
	[HttpGet("[action]")]
	public async Task<IActionResult> Profile()
	{
		var userMetaData = await accountService.GetUserMetaData(User);

		var model = new ProfileViewModel
		{
            EmailAddress = await accountService.GetUserEmail(User) ?? string.Empty,
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

    [Authorize]
    [HttpPost("[action]")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RequestEmailChange(ProfileViewModel model)
    {
        var success = await accountService.RequestEmailChange(User, model.NewEmailAddress);

        TempData["notificationMessage"] = success
            ? "Your email address has been changed."
            : "Could not change the email address. Check the address and try again.";
        TempData["notificationType"] = success ? "success" : "danger";

        if (success)
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction(nameof(Login));
        }

        return RedirectToAction(nameof(Profile));
    }

	[Authorize]
	[HttpGet("[action]")]
	public IActionResult RemoveAccount()
	{
		ViewBag.Theme = ThemesEnum.Auto;

		return View();
	}

	[Authorize]
	[HttpPost("[action]")]
	public async Task<IActionResult> RemoveAccountConfirm()
	{
		var success = await accountService.RemoveAccount(User);

		if (success)
		{
			await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
			TempData["notificationMessage"] = "Your account has been successfully removed.";
			TempData["notificationType"] = "success";
			return RedirectToAction("Login");
		}
		else
		{
			TempData["notificationMessage"] = "Failed to remove your account. Please try again.";
			TempData["notificationType"] = "danger";
			return RedirectToAction("Profile");
		}
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