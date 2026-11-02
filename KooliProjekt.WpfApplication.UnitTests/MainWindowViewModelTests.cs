using KooliProjekt.ApiClient;
using KooliProjekt.WpfApplication;

namespace KooliProjekt.WpfApplication.UnitTests;

public class MainWindowViewModelTests
{
    [Fact]
    public async Task LoadData_should_fill_projects_with_valid_response()
    {
        var viewModel = new MainWindowViewModel(new FakeApiClient
        {
            Projects = [new ProjectSummary { Id = 1, Name = "Website", Budget = 5000, CurrentCost = 250 }]
        });

        await viewModel.LoadDataAsync();

        Assert.Null(viewModel.Error);
        Assert.Single(viewModel.Projects);
        Assert.Equal("Website", viewModel.Projects[0].Name);
    }

    [Fact]
    public async Task LoadData_should_set_error_with_faulty_response()
    {
        var viewModel = new MainWindowViewModel(new FakeApiClient { Throws = true });

        await viewModel.LoadDataAsync();

        Assert.Empty(viewModel.Projects);
        Assert.NotNull(viewModel.Error);
    }

    [Fact]
    public void Selected_project_should_be_copied_to_editor()
    {
        var viewModel = new MainWindowViewModel(new FakeApiClient());

        viewModel.SelectedProject = new ProjectSummary
        {
            Id = 7,
            Name = "Office move",
            StartDate = new DateOnly(2026, 2, 1),
            Deadline = new DateOnly(2026, 3, 1),
            Budget = 7500m
        };

        Assert.Equal(7, viewModel.EditingProject.Id);
        Assert.Equal("Office move", viewModel.EditingProject.Name);
        Assert.Equal(7500m, viewModel.EditingProject.Budget);
    }

    [Fact]
    public async Task Save_should_reload_projects_after_success()
    {
        var client = new FakeApiClient
        {
            Projects = [new ProjectSummary { Id = 4, Name = "Saved" }]
        };
        var viewModel = new MainWindowViewModel(client);
        viewModel.EditingProject.Name = "Saved";

        await viewModel.SaveAsync();

        Assert.Equal(1, client.SaveCalls);
        Assert.Single(viewModel.Projects);
        Assert.Null(viewModel.Error);
    }

    [Fact]
    public async Task Delete_should_reset_editor_and_reload_projects()
    {
        var client = new FakeApiClient();
        var viewModel = new MainWindowViewModel(client);
        viewModel.SelectedProject = new ProjectSummary { Id = 12, Name = "To delete" };

        await viewModel.DeleteAsync();

        Assert.Equal(1, client.DeleteCalls);
        Assert.Equal(0, viewModel.EditingProject.Id);
    }

    private sealed class FakeApiClient : IProjectApiClient
    {
        public IReadOnlyList<ProjectSummary> Projects { get; init; } = [];
        public bool Throws { get; init; }
        public int SaveCalls { get; private set; }
        public int DeleteCalls { get; private set; }

        public Task<LoginResponse> LoginAsync(string email, string password, CancellationToken cancellationToken = default)
        {
            if (Throws) throw new HttpRequestException("Connection failed");
            return Task.FromResult(new LoginResponse { AccessToken = "test" });
        }

        public Task<PagedResult<ProjectSummary>> ListProjectsAsync(int page = 1, int pageSize = 20, string? search = null, CancellationToken cancellationToken = default)
        {
            if (Throws) throw new HttpRequestException("Connection failed");
            return Task.FromResult(new PagedResult<ProjectSummary> { Results = Projects, Page = page, PageSize = pageSize, TotalCount = Projects.Count });
        }

        public Task<ProjectDetails?> GetProjectAsync(int id, CancellationToken cancellationToken = default) =>
            Task.FromResult(Projects.FirstOrDefault(x => x.Id == id) is { } project
                ? new ProjectDetails
                {
                    Id = project.Id,
                    Name = project.Name,
                    StartDate = project.StartDate,
                    Deadline = project.Deadline,
                    Budget = project.Budget
                }
                : null);

        public Task<OperationResult<int>> SaveProjectAsync(ProjectEdit project, CancellationToken cancellationToken = default)
        {
            SaveCalls++;
            return Task.FromResult(new OperationResult<int> { Value = project.Id == 0 ? 1 : project.Id });
        }

        public Task<OperationResult<bool>> DeleteProjectAsync(int id, CancellationToken cancellationToken = default)
        {
            DeleteCalls++;
            return Task.FromResult(new OperationResult<bool> { Value = true });
        }
    }
}
