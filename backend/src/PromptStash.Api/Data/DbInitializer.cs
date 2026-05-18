using Microsoft.EntityFrameworkCore;
using PromptStash.Api.Data.Entities;
using PromptStash.Api.Services;

namespace PromptStash.Api.Data;

public static class DbInitializer
{
    public static async Task InitializeAsync(IServiceProvider services, ILogger logger, CancellationToken ct = default)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

        // If migrations exist apply them, otherwise create the schema from the model.
        if (db.Database.GetMigrations().Any())
            await db.Database.MigrateAsync(ct);
        else
            await db.Database.EnsureCreatedAsync(ct);

        await DbSchemaPatches.ApplyAsync(db, ct);

        if (await db.Users.AnyAsync(ct)) return;
        logger.LogInformation("Seeding initial data…");

        var demo = new AppUser
        {
            Email = "demo@skillstash.io",
            UserName = "demo",
            DisplayName = "Demo Curator",
            PasswordHash = hasher.Hash("Demo!2345"),
            Headline = "Staff Engineer · Platform",
            Bio = "Curating agent skills for builders.",
            CreatedAtUtc = DateTime.UtcNow
        };
        db.Users.Add(demo);

        db.Skills.AddRange(new[]
        {
            new Skill
            {
                Title = "Code review skill",
                Body = "# Code review\n\nYou are a senior staff engineer. Review code for correctness, edge cases, readability, and security.",
                Description = "SKILL.md-style code review for any language.",
                AgentSlug = "claude",
                Visibility = SkillVisibility.Public,
                Tags = new() { "code-review", "engineering" },
                AuthorId = demo.Id,
                CreatedAtUtc = DateTime.UtcNow
            },
            new Skill
            {
                Title = "Bug triage skill",
                Body = "# Bug triage\n\nGiven a bug report, produce root cause, blast radius, mitigation, and a fix plan.",
                Description = "Turn vague reports into actionable triage.",
                AgentSlug = "any",
                Visibility = SkillVisibility.Public,
                Tags = new() { "bugs", "ops" },
                AuthorId = demo.Id,
                CreatedAtUtc = DateTime.UtcNow
            }
        });

        await db.SaveChangesAsync(ct);
    }
}
