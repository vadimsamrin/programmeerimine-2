using KooliProjekt.ApiClient;

namespace KooliProjekt.WindowsForms;

/// <summary>View contract used by the MVP presenter from the course exercises.</summary>
public interface IMainView
{
    IReadOnlyList<ProjectSummary>? DataSource { set; }
    ProjectEdit CurrentProject { get; set; }
    void SetPresenter(MainViewPresenter presenter);
    void ShowError(string title, OperationResult response);
    bool ConfirmDelete();
}
