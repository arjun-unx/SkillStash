namespace PromptStash.Api.Services.Messaging;

/// <summary>
/// Polls the in-memory queue and dispatches each envelope through <see cref="EventDispatcher"/>.
/// Used for local development / docker-compose without Azure Service Bus credentials.
/// </summary>
public sealed class InMemoryBusConsumer(
    InMemoryServiceBus bus,
    EventDispatcher dispatcher,
    ILogger<InMemoryBusConsumer> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("InMemoryBusConsumer started");
        while (!stoppingToken.IsCancellationRequested)
        {
            if (bus.TryDequeue(out var envelope))
            {
                try
                {
                    await dispatcher.DispatchAsync(envelope.EventName, envelope.MessageId, envelope.PayloadJson, stoppingToken);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to handle in-memory integration event {EventName}", envelope.EventName);
                }
            }
            else
            {
                await Task.Delay(TimeSpan.FromMilliseconds(250), stoppingToken);
            }
        }
        logger.LogInformation("InMemoryBusConsumer stopped");
    }
}
