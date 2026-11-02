using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Net.Http;
using System.Runtime.CompilerServices;
using KooliProjekt.ApiClient;

namespace KooliProjekt.WpfApplication;

public class MainWindowViewModel : INotifyPropertyChanged
{
    private readonly IProjectApiClient apiClient;
    public ObservableCollection<ProjectSummary> Projects { get; } = [];
    private ProjectSummary? selectedProject;
    private ProjectEdit editingProject = new();
    private string? error;

    public MainWindowViewModel() : this(new ProjectApiClient(new HttpClient { BaseAddress = new Uri("http://localhost:5179/") })) { }

    public MainWindowViewModel(IProjectApiClient apiClient)
    {
        this.apiClient = apiClient;
        LoadCommand = new AsyncRelayCommand(LoadDataAsync);
        NewCommand = new AsyncRelayCommand(() => { AddNew(); return Task.CompletedTask; });
        SaveCommand = new AsyncRelayCommand(SaveAsync);
        DeleteCommand = new AsyncRelayCommand(DeleteAsync, () => EditingProject.Id > 0);
    }

    public ProjectSummary? SelectedProject
    {
        get => selectedProject;
        set
        {
            selectedProject = value;
            EditingProject = value is null
                ? new ProjectEdit()
                : new ProjectEdit
                {
                    Id = value.Id,
                    Name = value.Name,
                    StartDate = value.StartDate,
                    Deadline = value.Deadline,
                    Budget = value.Budget
                };
            OnPropertyChanged();
            DeleteCommand.RaiseCanExecuteChanged();
        }
    }

    public ProjectEdit EditingProject
    {
        get => editingProject;
        private set { editingProject = value; OnPropertyChanged(); }
    }

    public string? Error
    {
        get => error;
        private set { error = value; OnPropertyChanged(); }
    }

    public AsyncRelayCommand LoadCommand { get; }
    public AsyncRelayCommand NewCommand { get; }
    public AsyncRelayCommand SaveCommand { get; }
    public AsyncRelayCommand DeleteCommand { get; }

    public async Task LoadDataAsync()
    {
        try
        {
            Error = null;
            await apiClient.LoginAsync("demo@project.local", "Project123!");
            var response = await apiClient.ListProjectsAsync();
            Projects.Clear();
            foreach (var project in response.Results) Projects.Add(project);
        }
        catch (Exception exception)
        {
            Error = exception.Message;
        }
    }

    public void AddNew()
    {
        selectedProject = null;
        EditingProject = new ProjectEdit();
        OnPropertyChanged(nameof(SelectedProject));
        DeleteCommand.RaiseCanExecuteChanged();
    }

    public async Task SaveAsync()
    {
        try
        {
            Error = null;
            var result = await apiClient.SaveProjectAsync(EditingProject);
            if (result.HasErrors)
            {
                Error = string.Join(Environment.NewLine, result.Errors.Concat(result.PropertyErrors.SelectMany(x => x.Value)));
                return;
            }
            await LoadDataAsync();
        }
        catch (Exception exception)
        {
            Error = exception.Message;
        }
    }

    public async Task DeleteAsync()
    {
        if (EditingProject.Id <= 0) return;
        try
        {
            Error = null;
            var result = await apiClient.DeleteProjectAsync(EditingProject.Id);
            if (result.HasErrors)
            {
                Error = string.Join(Environment.NewLine, result.Errors.Concat(result.PropertyErrors.SelectMany(x => x.Value)));
                return;
            }
            AddNew();
            await LoadDataAsync();
        }
        catch (Exception exception)
        {
            Error = exception.Message;
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
