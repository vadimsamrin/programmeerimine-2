using KooliProjekt.ApiClient;
using KooliProjekt.WindowsForms;

namespace KooliProjekt.WindowsForms.UnitTests;

public class MainViewPresenterTests
{
    [Fact]
    public async Task LoadData_populates_view_when_api_returns_projects()
    {
        var client = new FakeProjectApiClient
        {
            Projects = new PagedResult<ProjectSummary>
            {
                Results = [Project(1, "Website"), Project(2, "Mobile app")]
            }
        };
        var view = new FakeMainView();
        var presenter = new MainViewPresenter(client, view);

        await presenter.LoadData();

        Assert.Equal(2, view.DataSource!.Count);
        Assert.Null(view.LastError);
    }

    [Fact]
    public async Task LoadData_shows_error_when_api_throws()
    {
        var client = new FakeProjectApiClient { ListException = new InvalidOperationException("Service unavailable") };
        var view = new FakeMainView();
        var presenter = new MainViewPresenter(client, view);

        await presenter.LoadData();

        Assert.Null(view.DataSource);
        Assert.Contains("Service unavailable", view.LastError!.Errors);
    }

    [Fact]
    public void SetSelection_copies_project_to_editor()
    {
        var view = new FakeMainView();
        var presenter = new MainViewPresenter(new FakeProjectApiClient(), view);

        presenter.SetSelection(Project(7, "Office renovation"));

        Assert.Equal(7, view.CurrentProject.Id);
        Assert.Equal("Office renovation", view.CurrentProject.Name);
        Assert.Equal(1000m, view.CurrentProject.Budget);
    }

    [Fact]
    public void AddNew_resets_editor()
    {
        var view = new FakeMainView { CurrentProject = new ProjectEdit { Id = 5, Name = "Old" } };
        var presenter = new MainViewPresenter(new FakeProjectApiClient(), view);

        presenter.AddNew();

        Assert.Equal(0, view.CurrentProject.Id);
        Assert.Equal(string.Empty, view.CurrentProject.Name);
    }

    [Fact]
    public async Task Save_shows_validation_errors_and_does_not_reload()
    {
        var client = new FakeProjectApiClient { SaveResult = OperationResult<int>.Failure("Name is required.") };
        var view = new FakeMainView { CurrentProject = new ProjectEdit { Name = "" } };
        var presenter = new MainViewPresenter(client, view);

        await presenter.Save();

        Assert.Equal(1, client.SaveCalls);
        Assert.Equal(0, client.ListCalls);
        Assert.Contains("Name is required.", view.LastError!.Errors);
    }

    [Fact]
    public async Task Save_reloads_data_after_success()
    {
        var client = new FakeProjectApiClient
        {
            SaveResult = new OperationResult<int> { Value = 4 },
            Projects = new PagedResult<ProjectSummary> { Results = [Project(4, "Saved")] }
        };
        var view = new FakeMainView { CurrentProject = new ProjectEdit { Name = "Saved" } };
        var presenter = new MainViewPresenter(client, view);

        await presenter.Save();

        Assert.Equal(1, client.SaveCalls);
        Assert.Equal(1, client.ListCalls);
        Assert.Single(view.DataSource!);
    }

    [Fact]
    public async Task Delete_does_nothing_when_user_cancels_confirmation()
    {
        var client = new FakeProjectApiClient();
        var view = new FakeMainView { ConfirmDeleteResult = false };
        var presenter = new MainViewPresenter(client, view);
        presenter.SetSelection(Project(9, "Do not delete"));

        await presenter.Delete();

        Assert.Equal(0, client.DeleteCalls);
    }

    [Fact]
    public async Task Delete_shows_error_when_api_rejects_request()
    {
        var client = new FakeProjectApiClient { DeleteResult = OperationResult<bool>.Failure("Project has work logs.") };
        var view = new FakeMainView { ConfirmDeleteResult = true };
        var presenter = new MainViewPresenter(client, view);
        presenter.SetSelection(Project(3, "Protected project"));

        await presenter.Delete();

        Assert.Equal(1, client.DeleteCalls);
        Assert.Contains("Project has work logs.", view.LastError!.Errors);
    }

    [Fact]
    public async Task Delete_clears_editor_and_reloads_after_success()
    {
        var client = new FakeProjectApiClient
        {
            DeleteResult = new OperationResult<bool> { Value = true },
            Projects = new PagedResult<ProjectSummary> { Results = [] }
        };
        var view = new FakeMainView { ConfirmDeleteResult = true };
        var presenter = new MainViewPresenter(client, view);
        presenter.SetSelection(Project(3, "Completed project"));

        await presenter.Delete();

        Assert.Equal(1, client.DeleteCalls);
        Assert.Equal(1, client.ListCalls);
        Assert.Equal(0, view.CurrentProject.Id);
        Assert.Empty(view.DataSource!);
    }

    private static ProjectSummary Project(int id, string name) => new()
    {
        Id = id,
        Name = name,
        StartDate = new DateOnly(2026, 1, 1),
        Deadline = new DateOnly(2026, 2, 1),
        Budget = 1000m
    };

    private sealed class FakeMainView : IMainView
    {
        public IReadOnlyList<ProjectSummary>? DataSource { get; set; }
        public ProjectEdit CurrentProject { get; set; } = new();
        public OperationResult? LastError { get; private set; }
        public bool ConfirmDeleteResult { get; set; } = true;

        public void SetPresenter(MainViewPresenter presenter) { }
        public void ShowError(string title, OperationResult response) => LastError = response;
        public bool ConfirmDelete() => ConfirmDeleteResult;
    }

    private sealed class FakeProjectApiClient : IProjectApiClient
    {
        public PagedResult<ProjectSummary> Projects { get; set; } = new();
        public OperationResult<int> SaveResult { get; set; } = new() { Value = 1 };
        public OperationResult<bool> DeleteResult { get; set; } = new() { Value = true };
        public Exception? ListException { get; set; }
        public int ListCalls { get; private set; }
        public int SaveCalls { get; private set; }
        public int DeleteCalls { get; private set; }

        public Task<LoginResponse> LoginAsync(string email, string password, CancellationToken cancellationToken = default) =>
            Task.FromResult(new LoginResponse());

        public Task<PagedResult<ProjectSummary>> ListProjectsAsync(int page = 1, int pageSize = 20, string? search = null, CancellationToken cancellationToken = default)
        {
            ListCalls++;
            return ListException is null ? Task.FromResult(Projects) : Task.FromException<PagedResult<ProjectSummary>>(ListException);
        }

        public Task<ProjectDetails?> GetProjectAsync(int id, CancellationToken cancellationToken = default) =>
            Task.FromResult<ProjectDetails?>(null);

        public Task<OperationResult<int>> SaveProjectAsync(ProjectEdit project, CancellationToken cancellationToken = default)
        {
            SaveCalls++;
            return Task.FromResult(SaveResult);
        }

        public Task<OperationResult<bool>> DeleteProjectAsync(int id, CancellationToken cancellationToken = default)
        {
            DeleteCalls++;
            return Task.FromResult(DeleteResult);
        }
    }
}
