using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.Cookies;
using BabyTracker.Models.ViewModels;
using BabyTracker.Services;
using BabyTracker.Models.Account;

namespace BabyTracker.Controllers;

[Route("[controller]")]
public class AccountController : Controller
{
    private readonly IAccountService _accountService;

    public AccountController(IAccountService accountService)
    {
        _accountService = accountService;
    }

    [HttpGet("[action]")]
    public IActionResult Login()
    {
        return View(new LoginViewModel());
    }

    [HttpPost("[action]")]
    public async Task<IActionResult> Login(LoginViewModel model, string returnUrl = "")
    {
        var claimsPrincipal = await _accountService.Login(model);

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, claimsPrincipal);

        return RedirectToLocal(returnUrl);
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
        var userMetaData = await _accountService.GetUserMetaData(User);

        var model = new ProfileViewModel
        {
            Profile = AccountService.GetProfile(User),
            EnableMemoriesEmail = userMetaData?.EnableMemoriesEmail ?? false,
            MemoriesAddresses = userMetaData?.MemoriesAddresses ?? string.Empty,
            ShareList = userMetaData?.ShareList ?? string.Empty
        };

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
            ShareList = viewModel.ShareList
        };

        await _accountService.SaveUserMetaData(User, userMetaDate);

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