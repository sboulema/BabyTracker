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
        => View(new LoginViewModel());

    [HttpPost("[action]")]
    public async Task<IActionResult> Login(LoginViewModel model, string returnUrl = "")
    {
        var claimsPrincipal = await _accountService.Login(model);

        if (claimsPrincipal == null)
        {
            TempData["notificationMessage"] = "Username and/or password are incorrect.";
            TempData["notificationSuccess"] = "false";

            return RedirectToAction("Login");
        }

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, claimsPrincipal);

        return RedirectToLocal(returnUrl);
    }

    [HttpGet("[action]")]
    public IActionResult Register()
        => View(new LoginViewModel());

    [HttpPost("[action]")]
    public async Task<IActionResult> Register(LoginViewModel model, string returnUrl = "")
    {
        await _accountService.Register(model);

        return await Login(model, returnUrl);
    }

    [HttpGet("[action]")]
    public IActionResult ResetPassword()
        => View(new LoginViewModel());

    [HttpPost("[action]")]
    public async Task<IActionResult> ResetPassword(LoginViewModel model)
    {
        var result = await _accountService.ResetPassword(model);

        TempData["notificationMessage"] = result;
        TempData["notificationSuccess"] = "true";

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
        var userMetaData = await _accountService.GetUserMetaData(User);

        var model = new ProfileViewModel
        {
            EnableMemoriesEmail = userMetaData?.EnableMemoriesEmail ?? false,
            MemoriesAddresses = userMetaData?.MemoriesAddresses ?? string.Empty,
            ShareList = userMetaData?.ShareList ?? string.Empty,
            FontSize = userMetaData?.FontSize ?? 6,
            NickName = User.FindFirstValue("nickname") ?? string.Empty,
            ProfileImageUrl = User.FindFirstValue("picture") ?? string.Empty,
            UserId = User.FindFirstValue("userId") ?? string.Empty,
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
            ShareList = viewModel.ShareList,
            FontSize = viewModel.FontSize
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