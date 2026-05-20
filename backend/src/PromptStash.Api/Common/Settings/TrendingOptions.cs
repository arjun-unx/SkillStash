namespace PromptStash.Api.Common.Settings;

public sealed class TrendingOptions
{
    public const string SectionName = "Trending";

    public int SyncIntervalHours { get; set; } = 6;
    public bool SyncOnStartup { get; set; } = true;
    public string? GitHubToken { get; set; }

    public List<TrendingSourceOptions> Sources { get; set; } = new();
}

public sealed class TrendingSourceOptions
{
    public required string Provider { get; set; }
    public required string Name { get; set; }
    public required string Format { get; set; }
    public required string Owner { get; set; }
    public required string Repo { get; set; }
    public string Branch { get; set; } = "main";
    public string? Path { get; set; }
}
