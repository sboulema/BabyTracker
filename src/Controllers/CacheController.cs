using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Threading.Tasks;
using System.Security.Claims;
using Microsoft.AspNetCore.OutputCaching;
using System.Threading;

namespace BabyTracker.Controllers;

[Route("[controller]")]
public class CacheController(
	IOutputCacheStore outputCacheStore) : Controller
{
	[Authorize]
	[HttpGet("[action]")]
	public async Task<IActionResult> Clear(CancellationToken cancellationToken)
	{
		await outputCacheStore.EvictByTagAsync(User.FindFirstValue(ClaimTypes.NameIdentifier), cancellationToken);
		
		return RedirectToAction("Index", "Home");
	}
}
