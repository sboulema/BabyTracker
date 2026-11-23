using System;
using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.Cookies;
using BabyTracker.Models.ViewModels;
using BabyTracker.Services;
using BabyTracker.Sync;
using Microsoft.Extensions.Options;
using BabyTracker.Models.Account;
using System.Security.Claims;
using BabyTracker.Constants;

namespace BabyTracker.Controllers;

[Route("[controller]")]
public class AccountController(IAccountService accountService,
	IImportService importService,
    ICacheEvictionService cacheEvictionService,
	ISyncAccountStore syncAccountStore,
	ISyncService syncService,
	IOptions<BabyTrackerSyncOptions> syncOptions,
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

		if (syncOptions.Value.Enabled)
		{
			var syncAccount = await syncAccountStore.GetAsync(model.UserId);

			model.SyncAvailable = true;
			model.SyncEnabled = syncAccount?.Enabled ?? false;
			model.SyncEmailAddress = syncAccount?.EmailAddress ?? string.Empty;
			model.SyncHasPassword = !string.IsNullOrEmpty(syncAccount?.Password);
			model.SyncDeviceId = syncAccount?.DeviceId ?? string.Empty;
		}

		return View(model);
	}

	[Authorize]
	[HttpPost("[action]")]
	[ValidateAntiForgeryToken]
	public async Task<IActionResult> SyncSave(SyncSettingsViewModel model)
	{
		var userId = User.FindFirstValue("userId");

		if (!syncOptions.Value.Enabled || string.IsNullOrEmpty(userId))
		{
			return NotFound();
		}

		var emailAddress = (model.EmailAddress ?? string.Empty).Trim();
		var account = await syncAccountStore.GetAsync(userId) ?? new SyncAccount();

		if (model.Enabled && emailAddress.Length == 0)
		{
			TempData["notificationMessage"] = "Enter the email address of your Baby Tracker account.";
			TempData["notificationType"] = "danger";

			return RedirectToAction(nameof(Profile));
		}

		if (model.Enabled && string.IsNullOrEmpty(model.Password) && string.IsNullOrEmpty(account.Password))
		{
			TempData["notificationMessage"] = "Enter the password of your Baby Tracker account.";
			TempData["notificationType"] = "danger";

			return RedirectToAction(nameof(Profile));
		}

		// Another account is another group: this website starts over as a new device in it
		if (!string.Equals(account.EmailAddress, emailAddress, StringComparison.OrdinalIgnoreCase))
		{
			account.AccountId = null;
			account.LastSyncId = 0;
			account.DeviceId = Guid.NewGuid().ToString("D");
		}

		account.Enabled = model.Enabled;
		account.EmailAddress = emailAddress;

		if (!string.IsNullOrEmpty(model.Password))
		{
			if (!string.IsNullOrEmpty(account.Password) && account.Password != model.Password)
			{
				// A new password can belong to another account: check the account id again at the next login
				account.AccountId = null;
			}

			account.Password = model.Password;
		}

		await syncAccountStore.SaveAsync(userId, account);

		TempData["notificationMessage"] = "Baby Tracker app sync settings saved.";
		TempData["notificationType"] = "success";

		return RedirectToAction(nameof(Profile));
	}

	[Authorize]
	[HttpPost("[action]")]
	[ValidateAntiForgeryToken]
	public async Task<IActionResult> SyncTest()
	{
		var userId = User.FindFirstValue("userId");

		if (!syncOptions.Value.Enabled || string.IsNullOrEmpty(userId))
		{
			return NotFound();
		}

		var result = await syncService.TestConnectionAsync(userId);

		TempData["notificationMessage"] = WebUtility.HtmlEncode(result.Message ?? "Done.");
		TempData["notificationType"] = result.Status == SyncStatus.Failed ? "danger" : "success";

		return RedirectToAction(nameof(Profile));
	}

	[Authorize]
	[HttpPost("[action]")]
	[ValidateAntiForgeryToken]
	public async Task<IActionResult> SyncRemove()
	{
		var userId = User.FindFirstValue("userId");

		if (string.IsNullOrEmpty(userId))
		{
			return NotFound();
		}

		await syncAccountStore.DeleteAsync(userId);

		TempData["notificationMessage"] = "The Baby Tracker account was removed from this website.";
		TempData["notificationType"] = "success";

		return RedirectToAction(nameof(Profile));
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

        await cacheEvictionService.EvictUserPagesAsync(User);

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
		var userId = User.FindFirstValue("userId");

		var success = await accountService.RemoveAccount(User);

		if (success)
		{
			if (!string.IsNullOrEmpty(userId))
			{
				await syncAccountStore.DeleteAsync(userId);
			}

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