using KooliProjekt.ApiClient;

namespace KooliProjekt.WindowsForms;

public sealed class MainViewPresenter
{
    private readonly IProjectApiClient apiClient;
    private readonly IMainView mainView;
    private ProjectSummary? selectedProject;

    public MainViewPresenter(IProjectApiClient apiClient, IMainView mainView)
    {
        this.apiClient = apiClient;
        this.mainView = mainView;
        mainView.SetPresenter(this);
    }

    public async Task LoadData()
    {
        try
        {
            var response = await apiClient.ListProjectsAsync(1, 100);
            mainView.DataSource = response.Results;
        }
        catch (Exception exception)
        {
            mainView.DataSource = null;
            mainView.ShowError("Ошибка загрузки проектов", OperationResult.Failure(exception.Message));
        }
    }

    public void SetSelection(ProjectSummary? selected)
    {
        selectedProject = selected;
        mainView.CurrentProject = selected is null
            ? new ProjectEdit()
            : new ProjectEdit
            {
                Id = selected.Id,
                Name = selected.Name,
                StartDate = selected.StartDate,
                Deadline = selected.Deadline,
                Budget = selected.Budget
            };
    }

    public void AddNew() => SetSelection(null);

    public async Task Save()
    {
        try
        {
            var result = await apiClient.SaveProjectAsync(mainView.CurrentProject);
            if (result.HasErrors)
            {
                mainView.ShowError("Ошибка сохранения", result);
                return;
            }
            await LoadData();
        }
        catch (Exception exception)
        {
            mainView.ShowError("Ошибка сохранения", OperationResult.Failure(exception.Message));
        }
    }

    public async Task Delete()
    {
        if (selectedProject is null || !mainView.ConfirmDelete()) return;
        try
        {
            var result = await apiClient.DeleteProjectAsync(selectedProject.Id);
            if (result.HasErrors)
            {
                mainView.ShowError("Ошибка удаления", result);
                return;
            }
            AddNew();
            await LoadData();
        }
        catch (Exception exception)
        {
            mainView.ShowError("Ошибка удаления", OperationResult.Failure(exception.Message));
        }
    }
}
