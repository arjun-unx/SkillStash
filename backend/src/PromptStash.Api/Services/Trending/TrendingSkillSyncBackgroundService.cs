using Microsoft.Extensions.Options;
using PromptStash.Api.Common.Settings;

namespace PromptStash.Api.Services.Trending;

public sealed class TrendingSkillSyncBackgroundService(
    IServiceProvider services,
    IOptions<TrendingOptions> options,
    ILogger<TrendingSkillSyncBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (options.Value.SyncOnStartup)
            await RunSync(stoppingToken);

        var interval = TimeSpan.FromHours(Math.Max(1, options.Value.SyncIntervalHours));
        using var timer = new PeriodicTimer(interval);

        while (await timer.WaitForNextTickAsync(stoppingToken))
            await RunSync(stoppingToken);
    }

    private async Task RunSync(CancellationToken ct)
    {
        try
        {
            using var scope = services.CreateScope();
            var sync = scope.ServiceProvider.GetRequiredService<ITrendingSkillSyncService>();
            await sync.SyncAsync(ct: ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Trending background sync failed");
        }
    }
}
