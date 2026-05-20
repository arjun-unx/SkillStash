namespace PromptStash.Api.Services.Trending;

public static class TrendingRoleClassifier
{
    private static readonly (string Role, string[] Keywords)[] Rules =
    [
        ("Frontend Developer", ["frontend", "react", "vue", "angular", "css", "html", "ui component"]),
        ("Backend Developer", ["backend", "api", "server", "node", "dotnet", "java", "spring"]),
        ("Full Stack Developer", ["full stack", "fullstack"]),
        ("Database Engineer", ["database", "sql", "postgres", "mysql", "mongodb", "query"]),
        ("API Developer", ["rest", "graphql", "openapi", "swagger", "api design"]),
        ("Manual Tester", ["manual test", "test case", "qa manual"]),
        ("Automation Tester", ["selenium", "cypress", "playwright", "automation test"]),
        ("QA Engineer", ["qa", "quality assurance", "testing"]),
        ("DevOps Engineer", ["devops", "kubernetes", "docker", "ci/cd", "terraform", "aws", "azure"]),
        ("UI/UX Designer", ["ux", "ui design", "wireframe", "figma", "user experience"]),
        ("Graphic Designer", ["graphic", "illustration", "brand", "logo"]),
        ("Product Manager", ["product manager", "roadmap", "prd", "stakeholder"]),
        ("Machine Learning Engineer", ["machine learning", "ml engineer", "model training"]),
        ("Data Analyst", ["data analyst", "analytics", "dashboard", "excel", "tableau"]),
        ("Data Scientist", ["data scientist", "statistics", "experiment"]),
        ("GenAI Engineer", ["llm", "prompt engineer", "rag", "fine-tun", "genai", "gpt", "claude", "gemini"]),
        ("Cloud Engineer", ["cloud", "gcp", "azure", "aws", "infrastructure"]),
        ("Security Engineer", ["security", "pentest", "vulnerability", "owasp"]),
        ("Mobile App Developer", ["mobile", "ios", "android", "flutter", "react native"]),
        ("Technical Writer", ["technical writer", "documentation", "docs"]),
        ("Business Analyst", ["business analyst", "requirements", "user story"])
    ];

    public static string Classify(string? forColumn, string title, string body)
    {
        if (!string.IsNullOrWhiteSpace(forColumn))
        {
            var f = forColumn.Trim();
            if (f.Length <= 80) return f;
        }

        var hay = $"{title} {body}".ToLowerInvariant();
        foreach (var (role, keywords) in Rules)
        {
            if (keywords.Any(k => hay.Contains(k, StringComparison.Ordinal)))
                return role;
        }

        return "General";
    }

    public static string Snippet(string body, int max = 280)
    {
        var t = body.Trim().Replace("\r\n", "\n");
        if (t.Length <= max) return t;
        return t[..max].TrimEnd() + "â€¦";
    }

    public static string StableKey(string sourceName, string uniquePart)
    {
        var raw = $"{sourceName}:{uniquePart}";
        var hash = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(raw));
        return Convert.ToHexString(hash)[..32].ToLowerInvariant();
    }
}
