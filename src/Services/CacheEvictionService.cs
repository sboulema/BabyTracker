using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.OutputCaching;

namespace BabyTracker.Services;

public interface ICacheEvictionService
{
    Task EvictUserPagesAsync(ClaimsPrincipal user, CancellationToken cancellationToken = default);

    /// <summary>Evicts the cached pages of every user viewing the clone of <paramref name="activeUserId"/>.</summary>
    Task EvictClonePagesAsync(string activeUserId, CancellationToken cancellationToken = default);
}

public class CacheEvictionService(IOutputCacheStore outputCacheStore) : ICacheEvictionService
{
    public async Task EvictUserPagesAsync(ClaimsPrincipal user, CancellationToken cancellationToken = default)
    {
        var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrEmpty(userId))
        {
            return;
        }

        await outputCacheStore.EvictByTagAsync(userId, cancellationToken);
    }

    public async Task EvictClonePagesAsync(string activeUserId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(activeUserId))
        {
            return;
        }

        await outputCacheStore.EvictByTagAsync(CacheTags.Clone(activeUserId), cancellationToken);
    }
}