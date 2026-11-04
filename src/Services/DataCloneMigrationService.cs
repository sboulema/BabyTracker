using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BabyTracker.Services;

/// <summary>
/// Encrypts every data clone that still holds plaintext files when the application starts.
/// Login-time migration only covers users who log in again; this also covers users with a
/// long-lived "remember me" cookie and the background memories job, neither of which pass
/// through the login. Runs before the application accepts requests.
/// </summary>
public sealed class DataCloneMigrationService(
    IServiceScopeFactory scopeFactory,
    IDataClonePathProvider pathProvider,
    ILogger<DataCloneMigrationService> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        RemoveLeftoverDirectories();

        var userIds = pathProvider.EnumerateUserIds().ToList();

        // Sequential on purpose: each user can trigger Auth0 Management API calls.
        foreach (var userId in userIds)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            try
            {
                using var scope = scopeFactory.CreateScope();
                var importService = scope.ServiceProvider.GetRequiredService<IImportService>();

                await importService.EnsureDataCloneEncrypted(userId);
            }
            catch (Exception e)
            {
                // Do not block startup. Reads of this clone fail closed until the next login retries.
                logger.LogError(e, "Encrypting the data clone of user {UserId} failed", userId);
            }
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
        => Task.CompletedTask;

    private void RemoveLeftoverDirectories()
        => pathProvider.EnumerateLeftoverDirectories()
            .ToList()
            .ForEach(directory =>
            {
                try
                {
                    System.IO.Directory.Delete(directory, recursive: true);
                    logger.LogInformation("Removed leftover import directory '{Directory}'", directory);
                }
                catch (Exception e)
                {
                    logger.LogWarning(e, "Could not remove leftover import directory '{Directory}'", directory);
                }
            });
}
