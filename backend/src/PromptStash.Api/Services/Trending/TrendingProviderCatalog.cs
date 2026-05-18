namespace PromptStash.Api.Services.Trending;

public static class TrendingProviderCatalog
{
    public static readonly IReadOnlyList<ProviderMeta> All =
    [
        new("claude", "Claude", "Anthropic", "Official Anthropic agent skills (SKILL.md) from GitHub.", "spark"),
        new("openai", "ChatGPT", "OpenAI", "OpenAI agent skills repositories on GitHub.", "smart_toy"),
        new("gemini", "Gemini", "Google", "Google Gemini skills and guides from public GitHub repos.", "auto_awesome"),
        new("cursor", "Cursor", "Cursor", "Cursor agent skills and IDE workflows from GitHub.", "terminal"),
        new("grok", "Grok", "xAI", "xAI / Grok-oriented skills from community GitHub repos.", "bolt"),
        new("community", "Community", "Curated", "Community-maintained SKILL.md collections on GitHub.", "groups")
    ];

    public static ProviderMeta? Find(string slug) =>
        All.FirstOrDefault(p => p.Slug.Equals(slug, StringComparison.OrdinalIgnoreCase));

    public sealed record ProviderMeta(string Slug, string Name, string ShortLabel, string Description, string Icon);
}
