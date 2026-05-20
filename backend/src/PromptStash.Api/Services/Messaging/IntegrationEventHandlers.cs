using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PromptStash.Api.Common.Events;
using PromptStash.Api.Data;

namespace PromptStash.Api.Services.Messaging;

public interface IIntegrationEventHandler
{
    string EventName { get; }
    Task HandleAsync(string payloadJson, CancellationToken ct);
}

public sealed class UserRegisteredHandler(
    IEmailService email,
    ILogger<UserRegisteredHandler> logger) : IIntegrationEventHandler
{
    public string EventName => "user.registered.v1";

    public async Task HandleAsync(string payloadJson, CancellationToken ct)
    {
        var evt = JsonSerializer.Deserialize<UserRegisteredIntegrationEvent>(payloadJson,
            new JsonSerializerOptions(JsonSerializerDefaults.Web));
        if (evt is null) return;

        logger.LogInformation("Sending welcome email to {Email}", evt.Email);
        var html = $"""
            <h1>Welcome to SkillStash, {evt.DisplayName}!</h1>
            <p>Thanks for joining. Discover agent skills from GitHub or publish your own SKILL.md-style instructions.</p>
            <p>— The SkillStash team</p>
            """;
        await email.SendAsync(evt.Email, "Welcome to SkillStash", html, ct);
    }
}

public sealed class SkillPublishedHandler(
    AppDbContext db,
    IEmailService email,
    ILogger<SkillPublishedHandler> logger) : IIntegrationEventHandler
{
    public string EventName => "skill.published.v1";

    public async Task HandleAsync(string payloadJson, CancellationToken ct)
    {
        var evt = JsonSerializer.Deserialize<SkillPublishedIntegrationEvent>(payloadJson,
            new JsonSerializerOptions(JsonSerializerDefaults.Web));
        if (evt is null) return;

        await email.SendAsync(
            evt.AuthorEmail,
            $"Your skill '{evt.Title}' is live on SkillStash",
            $"""
                <h2>{evt.Title}</h2>
                <p>Your skill has been published and is now visible in the public feed.</p>
                <p>Skill ID: {evt.SkillId}</p>
            """,
            ct);

        // Fan out to followers (opt-out aware, free-tier sequential).
        var followerEmails = await (from f in db.Follows
                                    join u in db.Users on f.FollowerId equals u.Id
                                    where f.FolloweeId == evt.AuthorId && u.EmailNotificationsEnabled
                                    select new { u.Email, u.DisplayName })
                                   .ToListAsync(ct);

        logger.LogInformation("Fanning out 'skill.published' to {Count} followers of {AuthorId}",
            followerEmails.Count, evt.AuthorId);

        foreach (var f in followerEmails)
        {
            await email.SendAsync(
                f.Email,
                $"{evt.AuthorDisplayName} just shared a new skill",
                $"""
                    <p>Hey {f.DisplayName},</p>
                    <h2>{evt.Title}</h2>
                    {(string.IsNullOrWhiteSpace(evt.Description) ? "" : $"<p>{evt.Description}</p>")}
                    <p>Check it out on SkillStash.</p>
                """,
                ct);
        }
    }
}
