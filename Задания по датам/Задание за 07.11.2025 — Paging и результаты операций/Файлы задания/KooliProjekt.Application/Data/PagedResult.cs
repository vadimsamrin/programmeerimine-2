namespace KooliProjekt.Application.Data;

public class PagedResult<T>
{
    public IReadOnlyList<T> Results { get; init; } = [];
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalCount { get; init; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
}
