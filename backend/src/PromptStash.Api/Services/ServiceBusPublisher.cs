using System.Collections.Concurrent;
using System.Text.Json;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Options;
using PromptStash.Api.Common.Events;
using PromptStash.Api.Common.Settings;

namespace PromptStash.Api.Services;

public interface IServiceBusPublisher
{
    Task PublishAsync<T>(T @event, CancellationToken ct = default) where T : IntegrationEvent;
}

public sealed record EnvelopeJson(string EventName, Guid MessageId, string PayloadJson);

internal static class JsonOptions
{
    public static readonly JsonSerializerOptions Default = new(JsonSerializerDefaults.Web);
}

/// <summary>
/// In-process queue used for local development. The same singleton instance is consumed
/// by <see cref="InMemoryBusConsumer"/>.
/// </summary>
public sealed class InMemoryServiceBus : IServiceBusPublisher
{
    private readonly ConcurrentQueue<EnvelopeJson> _queue = new();

    public Task PublishAsync<T>(T @event, CancellationToken ct = default) where T : IntegrationEvent
    {
        _queue.Enqueue(new EnvelopeJson(@event.EventName, @event.MessageId,
            JsonSerializer.Serialize(@event, @event.GetType(), JsonOptions.Default)));
        return Task.CompletedTask;
    }

    public bool TryDequeue(out EnvelopeJson envelope) => _queue.TryDequeue(out envelope!);
}

public sealed class AzureServiceBusPublisher : IServiceBusPublisher, IAsyncDisposable
{
    private readonly ServiceBusOptions _options;
    private readonly ServiceBusClient _client;
    private readonly ServiceBusSender _sender;

    public AzureServiceBusPublisher(IOptions<ServiceBusOptions> options)
    {
        _options = options.Value;
        _client = new ServiceBusClient(_options.ConnectionString);
        _sender = _client.CreateSender(_options.TopicName);
    }

    public async Task PublishAsync<T>(T @event, CancellationToken ct = default) where T : IntegrationEvent
    {
        var message = new ServiceBusMessage(JsonSerializer.Serialize(@event, @event.GetType(), JsonOptions.Default))
        {
            MessageId = @event.MessageId.ToString(),
            Subject = @event.EventName,
            ContentType = "application/json",
            ApplicationProperties = { ["EventName"] = @event.EventName }
        };
        await _sender.SendMessageAsync(message, ct);
    }

    public async ValueTask DisposeAsync()
    {
        await _sender.DisposeAsync();
        await _client.DisposeAsync();
    }
}
