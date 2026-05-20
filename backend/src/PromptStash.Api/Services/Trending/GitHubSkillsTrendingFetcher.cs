using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using PromptStash.Api.Common.Settings;

namespace PromptStash.Api.Services.Trending;

/// <summary>Discovers SKILL.md files in public GitHub repositories via the GitHub API.</summary>
public sealed class GitHubSkillsTrendingFetcher(
    IHttpClientFactory httpFactory,
    IOptions<TrendingOptions> options,
    ILogger<GitHubSkillsTrendingFetcher> logger) : ITrendingSkillFetcher
{
    public string Format => "github-skills";

    private const int MaxSkillsPerSource = 150;

    public async Task<TrendingFetchResult> FetchAsync(TrendingSourceOptions source, CancellationToken ct)
    {
        var client = httpFactory.CreateClient("trending");

        var branch = string.IsNullOrWhiteSpace(source.Branch) ? "main" : source.Branch;
        var treeUrl =
            $"https://api.github.com/repos/{source.Owner}/{source.Repo}/git/trees/{Uri.EscapeDataString(branch)}?recursive=1";

        var treeResponse = await client.GetAsync(treeUrl, ct);
        if (!treeResponse.IsSuccessStatusCode)
        {
            var rateLimited = await LogGitHubFailureAsync(treeResponse, source, ct);
            return new TrendingFetchResult([], rateLimited ? TrendingFetchStatus.RateLimited : TrendingFetchStatus.Failed);
        }

        var tree = await treeResponse.Content.ReadFromJsonAsync<GitTreeResponse>(ct);
        if (tree?.Tree is null || tree.Tree.Count == 0)
            return new TrendingFetchResult([], TrendingFetchStatus.Empty);

        var prefix = source.Path?.Trim().TrimEnd('/');
        var skillPaths = tree.Tree
            .Where(t => string.Equals(t.Type, "blob", StringComparison.OrdinalIgnoreCase)
                        && t.Path.EndsWith("SKILL.md", StringComparison.OrdinalIgnoreCase))
            .Where(t => string.IsNullOrEmpty(prefix)
                        || t.Path.StartsWith(prefix + "/", StringComparison.OrdinalIgnoreCase)
                        || string.Equals(t.Path, prefix + "/SKILL.md", StringComparison.OrdinalIgnoreCase))
            .Take(MaxSkillsPerSource)
            .ToList();

        var results = new List<TrendingFetchedSkill>();
        foreach (var item in skillPaths)
        {
            try
            {
                var skill = await FetchSkillFileAsync(client, source, branch, item.Path, ct);
                if (skill is not null)
                    results.Add(skill);
            }
            catch (Exception ex)
            {
                logger.LogDebug(ex, "Skipped {Path} in {Owner}/{Repo}", item.Path, source.Owner, source.Repo);
            }

            await Task.Delay(80, ct);
        }

        logger.LogInformation("GitHub skills: {Count} from {Owner}/{Repo}", results.Count, source.Owner, source.Repo);
        return new TrendingFetchResult(results, results.Count > 0 ? TrendingFetchStatus.Success : TrendingFetchStatus.Empty);
    }

    private async Task<bool> LogGitHubFailureAsync(
        HttpResponseMessage response,
        TrendingSourceOptions source,
        CancellationToken ct)
    {
        var body = await response.Content.ReadAsStringAsync(ct);
        var rateLimited = response.StatusCode == HttpStatusCode.Forbidden
                          && body.Contains("rate limit", StringComparison.OrdinalIgnoreCase);

        if (rateLimited)
        {
            var hasToken = !string.IsNullOrWhiteSpace(options.Value.GitHubToken);
            var resetAt = FormatRateLimitReset(response);
            if (!hasToken)
            {
                logger.LogWarning(
                    "GitHub API rate limit exceeded for {Owner}/{Repo} (unauthenticated). " +
                    "Set Trending:GitHubToken in appsettings.Secrets.json (see appsettings.Secrets.json.example) " +
                    "or run: dotnet user-secrets set \"Trending:GitHubToken\" \"ghp_...\" --project src/PromptStash.Api. " +
                    "Authenticated limit is 5,000 requests/hour.{Reset}",
                    source.Owner, source.Repo, resetAt);
            }
            else
            {
                logger.LogWarning(
                    "GitHub API rate limit exceeded for {Owner}/{Repo} even with a token configured.{Reset}",
                    source.Owner, source.Repo, resetAt);
            }

            return true;
        }

        logger.LogWarning(
            "GitHub tree request failed for {Owner}/{Repo}: {Status} {Body}",
            source.Owner, source.Repo, (int)response.StatusCode, Truncate(body, 200));
        return false;
    }

    private static string FormatRateLimitReset(HttpResponseMessage response)
    {
        if (!response.Headers.TryGetValues("X-RateLimit-Reset", out var values)
            || !long.TryParse(values.FirstOrDefault(), out var unix))
            return "";

        var reset = DateTimeOffset.FromUnixTimeSeconds(unix).ToLocalTime();
        return $" Rate limit resets at {reset:t}.";
    }

    private static string Truncate(string value, int max) =>
        value.Length <= max ? value : value[..max] + "â€¦";

    private async Task<TrendingFetchedSkill?> FetchSkillFileAsync(
        HttpClient client,
        TrendingSourceOptions source,
        string branch,
        string path,
        CancellationToken ct)
    {
        var contentUrl =
            $"https://api.github.com/repos/{source.Owner}/{source.Repo}/contents/{path}?ref={Uri.EscapeDataString(branch)}";

        var response = await client.GetAsync(contentUrl, ct);
        if (!response.IsSuccessStatusCode)
        {
            if (response.StatusCode == HttpStatusCode.Forbidden)
                await LogGitHubFailureAsync(response, source, ct);
            return null;
        }

        var file = await response.Content.ReadFromJsonAsync<GitHubContentResponse>(ct);
        if (file?.Content is null)
            return null;

        var body = DecodeContent(file.Content, file.Encoding);
        if (body.Length < 40)
            return null;

        var title = ExtractTitle(body, path);
        var role = InferRole(path, body);
        var externalKey = HashKey($"{source.Owner}/{source.Repo}:{path}");
        var sourceUrl = $"https://github.com/{source.Owner}/{source.Repo}/blob/{branch}/{path}";

        return new TrendingFetchedSkill(
            externalKey,
            source.Provider,
            source.Name,
            sourceUrl,
            title,
            body,
            role,
            source.Provider,
            [source.Provider, "skill", "github"],
            ScoreFor(body),
            null);
    }

    private static string DecodeContent(string content, string? encoding)
    {
        if (string.Equals(encoding, "base64", StringComparison.OrdinalIgnoreCase))
            return Encoding.UTF8.GetString(Convert.FromBase64String(content.Replace("\n", "", StringComparison.Ordinal)));
        return content;
    }

    private static string ExtractTitle(string body, string path)
    {
        foreach (var line in body.Split('\n'))
        {
            var t = line.Trim();
            if (t.StartsWith("# ", StringComparison.Ordinal))
                return t[2..].Trim();
        }

        var dir = Path.GetDirectoryName(path)?.Replace('\\', '/');
        if (!string.IsNullOrEmpty(dir))
        {
            var segment = dir.Split('/').LastOrDefault(s => !string.IsNullOrWhiteSpace(s));
            if (!string.IsNullOrWhiteSpace(segment))
                return segment.Replace('-', ' ').Replace('_', ' ');
        }

        return "Untitled skill";
    }

    private static string InferRole(string path, string body)
    {
        var lower = (path + " " + body[..Math.Min(body.Length, 500)]).ToLowerInvariant();
        if (lower.Contains("code review", StringComparison.Ordinal)) return "Code review";
        if (lower.Contains("test", StringComparison.Ordinal)) return "Testing";
        if (lower.Contains("doc", StringComparison.Ordinal)) return "Documentation";
        if (lower.Contains("design", StringComparison.Ordinal)) return "Design";
        if (lower.Contains("data", StringComparison.Ordinal)) return "Data";
        if (lower.Contains("security", StringComparison.Ordinal)) return "Security";
        return "General";
    }

    private static int ScoreFor(string body) => Math.Clamp(body.Length / 120, 10, 100);

    private static string HashKey(string input)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(hash)[..32].ToLowerInvariant();
    }

    private sealed class GitTreeResponse
    {
        [JsonPropertyName("tree")]
        public List<GitTreeItem> Tree { get; set; } = [];
    }

    private sealed class GitTreeItem
    {
        [JsonPropertyName("path")]
        public string Path { get; set; } = "";

        [JsonPropertyName("type")]
        public string Type { get; set; } = "";
    }

    private sealed class GitHubContentResponse
    {
        [JsonPropertyName("content")]
        public string? Content { get; set; }

        [JsonPropertyName("encoding")]
        public string? Encoding { get; set; }
    }
}
