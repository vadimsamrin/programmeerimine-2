using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace KooliProjekt.ApiClient;

public interface IProjectApiClient
{
    Task<LoginResponse> LoginAsync(string email, string password, CancellationToken cancellationToken = default);
    Task<PagedResult<ProjectSummary>> ListProjectsAsync(int page = 1, int pageSize = 20, string? search = null, CancellationToken cancellationToken = default);
    Task<ProjectDetails?> GetProjectAsync(int id, CancellationToken cancellationToken = default);
    Task<OperationResult<int>> SaveProjectAsync(ProjectEdit project, CancellationToken cancellationToken = default);
    Task<OperationResult<bool>> DeleteProjectAsync(int id, CancellationToken cancellationToken = default);
}

public sealed class ProjectApiClient(HttpClient httpClient) : IProjectApiClient
{
    public async Task<LoginResponse> LoginAsync(string email, string password, CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.PostAsJsonAsync("login?useCookies=false", new { email, password }, cancellationToken);
        response.EnsureSuccessStatusCode();
        var login = await response.Content.ReadFromJsonAsync<LoginResponse>(cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("The API returned an empty login response.");
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.AccessToken);
        return login;
    }

    public async Task<PagedResult<ProjectSummary>> ListProjectsAsync(int page = 1, int pageSize = 20, string? search = null, CancellationToken cancellationToken = default)
    {
        var suffix = string.IsNullOrWhiteSpace(search) ? string.Empty : $"&search={Uri.EscapeDataString(search)}";
        var result = await httpClient.GetFromJsonAsync<OperationResult<PagedResult<ProjectSummary>>>(
            $"api/projects?page={page}&pageSize={pageSize}{suffix}", cancellationToken);
        return result?.Value ?? throw new InvalidOperationException("The API returned an invalid projects response.");
    }

    public async Task<ProjectDetails?> GetProjectAsync(int id, CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.GetAsync($"api/projects/{id}", cancellationToken);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound) return null;
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<OperationResult<ProjectDetails>>(cancellationToken: cancellationToken))?.Value;
    }

    public async Task<OperationResult<int>> SaveProjectAsync(ProjectEdit project, CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.PostAsJsonAsync("api/projects", project, cancellationToken);
        var result = await response.Content.ReadFromJsonAsync<OperationResult<int>>(cancellationToken: cancellationToken)
            ?? OperationResult<int>.Failure("The API returned an invalid response.");
        return result;
    }

    public async Task<OperationResult<bool>> DeleteProjectAsync(int id, CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.DeleteAsync($"api/projects/{id}", cancellationToken);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound) return OperationResult<bool>.Failure("Project was not found.");
        return await response.Content.ReadFromJsonAsync<OperationResult<bool>>(cancellationToken: cancellationToken)
            ?? OperationResult<bool>.Failure("The API returned an invalid response.");
    }
}

public sealed class LoginResponse
{
    public string AccessToken { get; init; } = string.Empty;
    public int ExpiresIn { get; init; }
    public string RefreshToken { get; init; } = string.Empty;
}

public class OperationResult
{
    public List<string> Errors { get; init; } = [];
    public Dictionary<string, List<string>> PropertyErrors { get; init; } = new();
    public bool HasErrors => Errors.Count > 0 || PropertyErrors.Count > 0;

    public static OperationResult Failure(params string[] errors) => new() { Errors = [.. errors] };
}

public sealed class OperationResult<T> : OperationResult
{
    public T? Value { get; init; }

    public new static OperationResult<T> Failure(params string[] errors) => new() { Errors = [.. errors] };
}

public sealed class PagedResult<T>
{
    public IReadOnlyList<T> Results { get; init; } = [];
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalCount { get; init; }
    public int TotalPages { get; init; }
}

public class ProjectSummary
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public DateOnly StartDate { get; init; }
    public DateOnly Deadline { get; init; }
    public decimal Budget { get; init; }
    public decimal CurrentCost { get; init; }
    public int TaskCount { get; init; }
    public int CompletedTaskCount { get; init; }
}

public sealed class ProjectDetails : ProjectSummary
{
    public decimal HourlyRate { get; init; }
    public IReadOnlyList<ProjectTaskSummary> Tasks { get; init; } = [];
}

public sealed class ProjectTaskSummary
{
    public int Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public decimal EstimatedHours { get; init; }
    public decimal ActualHours { get; init; }
    public decimal? FixedPrice { get; init; }
    public bool IsCompleted { get; init; }
}

public sealed class ProjectEdit
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateOnly StartDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    public DateOnly Deadline { get; set; } = DateOnly.FromDateTime(DateTime.Today.AddDays(14));
    public decimal Budget { get; set; }
    public decimal HourlyRate { get; set; }
}
