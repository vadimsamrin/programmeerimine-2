using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using KooliProjekt.Application.Data;
using KooliProjekt.Application.Features.Projects;
using KooliProjekt.Application.Infrastructure;

namespace KooliProjekt.IntegrationTests;

public class ProjectsApiTests(TestApplicationFactory factory) : IClassFixture<TestApplicationFactory>
{
    [Fact]
    public async Task Projects_endpoint_requires_authentication()
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/projects");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Authenticated_user_can_list_seeded_projects()
    {
        using var client = await CreateAuthenticatedClient();

        var response = await client.GetAsync("/api/projects?page=1&pageSize=20");
        var payload = await response.Content.ReadFromJsonAsync<OperationResult<PagedResult<ProjectListItem>>>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(payload);
        Assert.False(payload.HasErrors);
        Assert.True(payload.Value!.TotalCount >= 10);
        Assert.Contains(payload.Value.Results, x => x.Name.StartsWith("Näidisprojekt"));
    }

    [Fact]
    public async Task Invalid_project_paging_returns_a_safe_bad_request()
    {
        using var client = await CreateAuthenticatedClient();

        var response = await client.GetAsync("/api/projects?page=0&pageSize=20");
        using var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var errors = payload.RootElement.GetProperty("errors");
        Assert.Equal(JsonValueKind.Array, errors.ValueKind);
        Assert.Equal(1, errors.GetArrayLength());
        Assert.Contains("unexpected server error", errors[0].GetString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Authenticated_user_can_create_project_and_receive_identifier()
    {
        using var client = await CreateAuthenticatedClient();
        var command = new SaveProjectCommand
        {
            Name = "Integration-tested project",
            StartDate = new DateOnly(2026, 9, 10),
            Deadline = new DateOnly(2026, 10, 10),
            Budget = 9000m,
            HourlyRate = 75m
        };

        var response = await client.PostAsJsonAsync("/api/projects", command);
        var payload = await response.Content.ReadFromJsonAsync<OperationResult<int>>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(payload);
        Assert.False(payload.HasErrors);
        Assert.True(payload.Value > 0);
    }

    private async Task<HttpClient> CreateAuthenticatedClient()
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Integration-Test-Auth", "true");
        return await Task.FromResult(client);
    }
}
