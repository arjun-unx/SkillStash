using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Options;
using PromptStash.Api.Common.Settings;

namespace PromptStash.Api.Services.Messaging;

public sealed class AzureServiceBusConsumer(
    IOptions<ServiceBusOptions> options,
    EventDispatcher dispatcher,
    ILogger<AzureServiceBusConsumer> logger) : BackgroundService
{
    private readonly ServiceBusOptions _options = options.Value;
    private ServiceBusClient? _client;
    private ServiceBusProcessor? _processor;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (string.IsNullOrWhiteSpace(_options.ConnectionString))
        {
            logger.LogWarning("AzureServiceBusConsumer requires ServiceBus:ConnectionString. Skipping startup.");
            return;
        }

        _client = new ServiceBusClient(_options.ConnectionString);
        _processor = _client.CreateProcessor(_options.TopicName, _options.SubscriptionName, new ServiceBusProcessorOptions
        {
            AutoCompleteMessages = false,
            MaxConcurrentCalls = 4
        });
        _processor.ProcessMessageAsync += OnMessageAsync;
        _processor.ProcessErrorAsync += OnErrorAsync;

        await _processor.StartProcessingAsync(stoppingToken);
        logger.LogInformation("AzureServiceBusConsumer started ({Topic}/{Subscription})",
            _options.TopicName, _options.SubscriptionName);

        await Task.Delay(Timeout.Infinite, stoppingToken).ContinueWith(_ => { });
    }

    private async Task OnMessageAsync(ProcessMessageEventArgs args)
    {
        var name = args.Message.ApplicationProperties.TryGetValue("EventName", out var n) ? n?.ToString() : args.Message.Subject;
        if (string.IsNullOrWhiteSpace(name))
        {
            await args.DeadLetterMessageAsync(args.Message, "MissingEventName", cancellationToken: args.CancellationToken);
            return;
        }
        var messageId = Guid.TryParse(args.Message.MessageId, out var mid) ? mid : Guid.NewGuid();

        try
        {
            await dispatcher.DispatchAsync(name!, messageId, args.Message.Body.ToString(), args.CancellationToken);
            await args.CompleteMessageAsync(args.Message, args.CancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to process message {MessageId} ({EventName})", args.Message.MessageId, name);
            await args.AbandonMessageAsync(args.Message, cancellationToken: args.CancellationToken);
        }
    }

    private Task OnErrorAsync(ProcessErrorEventArgs args)
    {
        logger.LogError(args.Exception, "Service Bus error from {Source}", args.ErrorSource);
        return Task.CompletedTask;
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_processor is not null)
        {
            await _processor.StopProcessingAsync(cancellationToken);
            await _processor.DisposeAsync();
        }
        if (_client is not null) await _client.DisposeAsync();
        await base.StopAsync(cancellationToken);
    }
}
