using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.Extensions.Primitives;

namespace BabyTracker.Policies;

public sealed class AuthenticatedOutputCachePolicy : IOutputCachePolicy
{
    ValueTask IOutputCachePolicy.CacheRequestAsync(
        OutputCacheContext context,
        CancellationToken cancellationToken)
    {
        var user = context.HttpContext.User;
        var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);

        // Never cache responses for anonymous users
        if (string.IsNullOrEmpty(userId))
        {
            context.EnableOutputCaching = false;

            return ValueTask.CompletedTask;
        }

        var activeUserId = user.FindFirstValue("activeUserId") ?? string.Empty;
        var attemptOutputCaching = AttemptOutputCaching(context);

        context.EnableOutputCaching = true;
        context.AllowCacheLookup = attemptOutputCaching;
        context.AllowCacheStorage = attemptOutputCaching;
        context.AllowLocking = true;
        context.ResponseExpirationTimeSpan = TimeSpan.FromDays(7);

        // Vary by any query by default
        context.CacheVaryByRules.QueryKeys = "*";

        // Vary by user and by the active clone, so switching
        // to a shared baby's clone never serves the previous clone's pages
        context.CacheVaryByRules.CacheKeyPrefix = $"{userId}:{activeUserId}";

        // Tag by user so all of their entries can be evicted at once
        context.Tags.Add(userId);

        return ValueTask.CompletedTask;
    }

    ValueTask IOutputCachePolicy.ServeFromCacheAsync(
        OutputCacheContext context,
        CancellationToken cancellationToken) => ValueTask.CompletedTask;

    ValueTask IOutputCachePolicy.ServeResponseAsync(
        OutputCacheContext context,
        CancellationToken cancellationToken)
    {
        // Only cache successful responses, so the redirect from
        // RequireDataCloneFilter is never stored
        if (context.HttpContext.Response.StatusCode != StatusCodes.Status200OK)
        {
            context.AllowCacheStorage = false;
        }

        return ValueTask.CompletedTask;
    }

    private static bool AttemptOutputCaching(OutputCacheContext context)
    {
        var request = context.HttpContext.Request;

        // Only GET and HEAD requests are cacheable
        if (!HttpMethods.IsGet(request.Method) &&
            !HttpMethods.IsHead(request.Method))
        {
            return false;
        }

        // Requests with an Authorization header are not cacheable
        return StringValues.IsNullOrEmpty(request.Headers.Authorization);
    }
}