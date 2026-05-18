using PromptStash.Api.Common.DTOs;
using PromptStash.Api.Common.Models;
using PromptStash.Api.Data.Entities;

namespace PromptStash.Api.Services.Trending;

public interface ITrendingSkillRepository
{
    Task<IReadOnlyList<TrendingProviderDto>> GetProvidersAsync(CancellationToken ct);
    Task<PaginatedList<TrendingSkillDto>> SearchAsync(
        string? provider,
        string? role,
        string? category,
        string? search,
        string sort,
        int page,
        int pageSize,
        Guid? userId,
        CancellationToken ct);
    Task<TrendingSkillDto?> GetByIdAsync(Guid id, Guid? userId, CancellationToken ct);
    Task<TrendingSkill?> GetEntityAsync(Guid id, CancellationToken ct);
}
