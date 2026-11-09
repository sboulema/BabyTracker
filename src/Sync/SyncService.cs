using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BabyTracker.Sync;

public enum SyncStatus
{
    /// <summary>Sync is off, so nothing was sent.</summary>
    Skipped,
    Sent,
    Failed
}

public sealed record SyncResult(SyncStatus Status, string? Message = null);

public interface ISyncService
{
    /// <summary>Whether entries added to the clone of <paramref name="userId"/> are sent to the Baby Tracker server.</summary>
    Task<bool> IsEnabledAsync(string userId);

    /// <summary>
    /// Sends the entry as a new transaction of this website's device. Never throws: the entry is already
    /// in the clone, so a failure is reported in the result.
    /// </summary>
    Task<SyncResult> PublishAsync(string userId, SyncEntry entry, CancellationToken cancellationToken = default);

    /// <summary>Logs in and lists the devices of the group. Sends no entries.</summary>
    Task<SyncResult> TestConnectionAsync(string userId, CancellationToken cancellationToken = default);
}

public sealed class SyncService(
    ISyncAccountStore accountStore,
    IHttpClientFactory httpClientFactory,
    IOptions<BabyTrackerSyncOptions> options,
    ILogger<SyncService> logger) : ISyncService
{
    public const string HttpClientName = "BabyTrackerSync";

    private const string ThumbnailSuffix = "__thumbnail";

    private static readonly ConcurrentDictionary<string, SemaphoreSlim> UserLocks = new();

    public async Task<bool> IsEnabledAsync(string userId)
    {
        if (!options.Value.Enabled)
        {
            return false;
        }

        var account = await accountStore.GetAsync(userId);

        return IsUsable(account);
    }

    public async Task<SyncResult> PublishAsync(string userId, SyncEntry entry, CancellationToken cancellationToken = default)
    {
        if (!options.Value.Enabled)
        {
            return new SyncResult(SyncStatus.Skipped);
        }

        // The SyncID counter must not be used twice, so one upload at a time per account
        var userLock = UserLocks.GetOrAdd(userId, _ => new SemaphoreSlim(1, 1));

        await userLock.WaitAsync(cancellationToken);

        try
        {
            var account = await accountStore.GetAsync(userId);

            if (!IsUsable(account))
            {
                return new SyncResult(SyncStatus.Skipped);
            }

            var session = await LoginAsync(userId, account!, cancellationToken);

            var syncId = account!.LastSyncId + 1;

            await session.UploadTransactionAsync(syncId, entry.Transaction, cancellationToken);

            // Remember it at once: a SyncID that the server has seen must not be used again
            account.LastSyncId = syncId;
            await accountStore.SaveAsync(userId, account);

            if (entry.Picture is { } picture)
            {
                await session.UploadPictureAsync($"{picture.FileName}.jpg", picture.Jpeg, cancellationToken);
                await session.UploadPictureAsync($"{picture.FileName}{ThumbnailSuffix}.jpg", picture.Thumbnail, cancellationToken);
            }

            return new SyncResult(SyncStatus.Sent);
        }
        catch (Exception exception) when (exception is BabyTrackerSyncException or HttpRequestException or TaskCanceledException)
        {
            logger.LogWarning(exception, "Sending an entry to the Baby Tracker server failed.");

            return new SyncResult(SyncStatus.Failed, GetMessage(exception));
        }
        finally
        {
            userLock.Release();
        }
    }

    public async Task<SyncResult> TestConnectionAsync(string userId, CancellationToken cancellationToken = default)
    {
        if (!options.Value.Enabled)
        {
            return new SyncResult(SyncStatus.Skipped);
        }

        var userLock = UserLocks.GetOrAdd(userId, _ => new SemaphoreSlim(1, 1));

        await userLock.WaitAsync(cancellationToken);

        try
        {
            var account = await accountStore.GetAsync(userId);

            if (account is null || string.IsNullOrEmpty(account.EmailAddress) || string.IsNullOrEmpty(account.Password))
            {
                return new SyncResult(SyncStatus.Failed, "Save the email address and password of your Baby Tracker account first.");
            }

            var session = await LoginAsync(userId, account, cancellationToken);
            var devices = await session.GetDevicesAsync(cancellationToken);

            var known = devices.Any(device => string.Equals(device.DeviceUUID, account.DeviceId, StringComparison.OrdinalIgnoreCase));

            return new SyncResult(
                SyncStatus.Sent,
                $"Connected. The group has {devices.Count} device(s){(known ? ", including this website" : "")}.");
        }
        catch (Exception exception) when (exception is BabyTrackerSyncException or HttpRequestException or TaskCanceledException)
        {
            logger.LogWarning(exception, "Testing the connection to the Baby Tracker server failed.");

            return new SyncResult(SyncStatus.Failed, GetMessage(exception));
        }
        finally
        {
            userLock.Release();
        }
    }

    private async Task<BabyTrackerSyncSession> LoginAsync(string userId, SyncAccount account, CancellationToken cancellationToken)
    {
        var session = await BabyTrackerSyncSession.LoginAsync(
            httpClientFactory.CreateClient(HttpClientName),
            account,
            cancellationToken);

        if (account.AccountId is null)
        {
            account.AccountId = session.AccountId;
            await accountStore.SaveAsync(userId, account);
        }
        else if (account.AccountId != session.AccountId)
        {
            // The app stops syncing in this situation too
            throw new BabyTrackerSyncException(
                "The Baby Tracker account is not the one this website was set up with. Save the account settings again to start over.");
        }

        return session;
    }

    private static bool IsUsable(SyncAccount? account)
        => account is { Enabled: true } &&
           !string.IsNullOrEmpty(account.EmailAddress) &&
           !string.IsNullOrEmpty(account.Password);

    private static string GetMessage(Exception exception)
        => exception switch
        {
            BabyTrackerSyncException => exception.Message,
            TaskCanceledException => "The Baby Tracker server did not answer in time.",
            _ => "The Baby Tracker server could not be reached."
        };
}
