using Microsoft.EntityFrameworkCore;

namespace KooliProjekt.Application.Data;

public static class PagerExtension
{
    public static async Task<PagedResult<T>> ToPagedResultAsync<T>(
        this IQueryable<T> query,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        if (page <= 0) throw new ArgumentException("Page must be greater than zero.", nameof(page));
        if (pageSize <= 0 || pageSize > 100) throw new ArgumentException("Page size must be between 1 and 100.", nameof(pageSize));

        var totalCount = await query.CountAsync(cancellationToken);
        var results = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);

        return new PagedResult<T>
        {
            Results = results,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }
}
