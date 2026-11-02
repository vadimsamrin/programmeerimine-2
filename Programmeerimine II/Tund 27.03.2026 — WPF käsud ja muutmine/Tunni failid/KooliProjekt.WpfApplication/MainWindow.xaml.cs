using System.Windows;
using System.Net.Http;
using KooliProjekt.ApiClient;

namespace KooliProjekt.WpfApplication;

public partial class MainWindow : Window
{
    private readonly MainWindowViewModel viewModel;

    public MainWindow()
    {
        InitializeComponent();
        viewModel = new MainWindowViewModel(new ProjectApiClient(new HttpClient { BaseAddress = new Uri("http://localhost:5179/") }));
        DataContext = viewModel;
    }

    private async void Window_Loaded(object sender, RoutedEventArgs e) => await viewModel.LoadDataAsync();
}
