namespace PromptStash.Api.Common.Settings;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";
    public string Issuer { get; set; } = "PromptStash";
    public string Audience { get; set; } = "PromptStash.Clients";
    public string Secret { get; set; } = string.Empty;
    public int ExpiryMinutes { get; set; } = 60 * 24;
}

public sealed class ServiceBusOptions
{
    public const string SectionName = "ServiceBus";
    public bool UseAzure { get; set; }
    public string ConnectionString { get; set; } = string.Empty;
    public string TopicName { get; set; } = "promptstash-events";
    public string SubscriptionName { get; set; } = "promptstash-api";
    public int MaxRetries { get; set; } = 5;
}

public sealed class EmailOptions
{
    public const string SectionName = "Email";
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 1025;
    public bool UseSsl { get; set; }
    public string? Username { get; set; }
    public string? Password { get; set; }
    public string FromAddress { get; set; } = "no-reply@promptstash.local";
    public string FromName { get; set; } = "PromptStash";
    public bool LogOnly { get; set; } = true;
}

public sealed class WorkerOptions
{
    public const string SectionName = "Worker";
    public bool HostInProcess { get; set; } = true;
}
