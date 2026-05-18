using Microsoft.EntityFrameworkCore;
using PromptStash.Api.Data;
using PromptStash.Api.Data.Entities;

namespace PromptStash.Api.Services;

/// <summary>
/// Dispatches an integration event payload to the matching <see cref="IIntegrationEventHandler"/>(s)
/// in a fresh DI scope, with idempotency enforced via <see cref="ProcessedMessage"/>.
/// </summary>
public sealed class EventDispatcher(IServiceProvider rootProvider, ILogger<EventDispatcher> logger)
{
    public async Task DispatchAsync(string eventName, Guid messageId, string payloadJson, CancellationToken ct)
    {
        using var scope = rootProvider.CreateScope();
        var sp = scope.ServiceProvider;
        var db = sp.GetRequiredService<AppDbContext>();

        if (await db.ProcessedMessages.AnyAsync(m => m.MessageId == messageId, ct))
        {
            logger.LogInformation("Skipping duplicate message {MessageId} ({EventName})", messageId, eventName);
            return;
        }

        var handlers = sp.GetServices<IIntegrationEventHandler>()
                         .Where(h => h.EventName == eventName)
                         .ToList();

        if (handlers.Count == 0)
        {
            logger.LogWarning("No handlers registered for event {EventName} ({MessageId})", eventName, messageId);
            return;
        }

        foreach (var handler in handlers)
        {
            await handler.HandleAsync(payloadJson, ct);
        }

        db.ProcessedMessages.Add(new ProcessedMessage
        {
            MessageId = messageId,
            EventName = eventName,
            ProcessedAtUtc = DateTime.UtcNow
        });
        await db.SaveChangesAsync(ct);

        logger.LogInformation("Processed integration event {EventName} ({MessageId})", eventName, messageId);
    }
}
