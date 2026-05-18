using PromptStash.Api.Common.Settings;

namespace PromptStash.Api.Services.Trending;

internal static class TrendingSourceDefaults
{
    public static List<TrendingSourceOptions> Get() =>
    [
        new()
        {
            Provider = "claude",
            Name = "anthropic-skills",
            Format = "github-skills",
            Owner = "anthropics",
            Repo = "skills",
            Branch = "main",
            Path = "skills"
        },
        new()
        {
            Provider = "openai",
            Name = "openai-skills",
            Format = "github-skills",
            Owner = "openai",
            Repo = "skills",
            Branch = "main"
        },
        new()
        {
            Provider = "cursor",
            Name = "cursor-skills",
            Format = "github-skills",
            Owner = "cursor",
            Repo = "agent-skills",
            Branch = "main"
        },
        new()
        {
            Provider = "gemini",
            Name = "google-gemini-skills",
            Format = "github-skills",
            Owner = "GoogleCloudPlatform",
            Repo = "generative-ai",
            Branch = "main",
            Path = "gemini"
        },
        new()
        {
            Provider = "community",
            Name = "obra-superpowers",
            Format = "github-skills",
            Owner = "obra",
            Repo = "superpowers",
            Branch = "main"
        }
    ];
}
