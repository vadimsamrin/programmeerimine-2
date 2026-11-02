using System.Globalization;
using KooliProjekt.ApiClient;

namespace KooliProjekt.WindowsForms;

public partial class Form1 : Form, IMainView
{
    private readonly IProjectApiClient apiClient;
    private MainViewPresenter? presenter;
    private readonly DataGridView grid = new() { Dock = DockStyle.Fill, ReadOnly = true, AutoGenerateColumns = true, MultiSelect = false };
    private readonly TextBox email = new() { Text = "demo@project.local", Width = 190 };
    private readonly TextBox password = new() { Text = "Project123!", Width = 130, UseSystemPasswordChar = true };
    private readonly TextBox nameBox = new() { Width = 260 };
    private readonly TextBox budgetBox = new() { Width = 260 };
    private readonly TextBox hourlyRateBox = new() { Width = 260 };
    private readonly DateTimePicker startDate = new() { Width = 260, Format = DateTimePickerFormat.Short };
    private readonly DateTimePicker deadline = new() { Width = 260, Format = DateTimePickerFormat.Short };
    private ProjectEdit currentProject = new();

    public Form1(IProjectApiClient apiClient)
    {
        this.apiClient = apiClient;
        InitializeComponent();
        Text = "Project management";
        Width = 1150;
        Height = 700;

        var loginButton = new Button { Text = "Войти и загрузить", AutoSize = true };
        loginButton.Click += async (_, _) => await LoginAndLoadAsync();
        var top = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 46, Padding = new Padding(8) };
        top.Controls.AddRange([new Label { Text = "Email", AutoSize = true, Padding = new Padding(0, 7, 0, 0) }, email,
            new Label { Text = "Пароль", AutoSize = true, Padding = new Padding(0, 7, 0, 0) }, password, loginButton]);

        var editor = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, AutoScroll = true, Padding = new Padding(10) };
        editor.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        editor.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        AddField(editor, "Название", nameBox);
        AddField(editor, "Начало", startDate);
        AddField(editor, "Срок", deadline);
        AddField(editor, "Бюджет", budgetBox);
        AddField(editor, "Цена за час", hourlyRateBox);
        var add = new Button { Text = "Добавить", AutoSize = true };
        var save = new Button { Text = "Сохранить", AutoSize = true };
        var delete = new Button { Text = "Удалить", AutoSize = true };
        add.Click += (_, _) => presenter?.AddNew();
        save.Click += async (_, _) => { if (TryReadProject()) await (presenter?.Save() ?? Task.CompletedTask); };
        delete.Click += async (_, _) => await (presenter?.Delete() ?? Task.CompletedTask);
        var buttons = new FlowLayoutPanel { AutoSize = true };
        buttons.Controls.AddRange([add, save, delete]);
        editor.Controls.Add(buttons, 1, editor.RowCount);

        var content = new SplitContainer { Dock = DockStyle.Fill, SplitterDistance = 710 };
        content.Panel1.Controls.Add(grid);
        content.Panel2.Controls.Add(editor);
        Controls.Add(content);
        Controls.Add(top);
        grid.SelectionChanged += (_, _) => presenter?.SetSelection(grid.CurrentRow?.DataBoundItem as ProjectSummary);
    }

    public IReadOnlyList<ProjectSummary>? DataSource
    {
        set => grid.DataSource = value?.ToList();
    }

    public ProjectEdit CurrentProject
    {
        get => currentProject;
        set
        {
            currentProject = value;
            nameBox.Text = value.Name;
            startDate.Value = value.StartDate.ToDateTime(TimeOnly.MinValue);
            deadline.Value = value.Deadline.ToDateTime(TimeOnly.MinValue);
            budgetBox.Text = value.Budget.ToString(CultureInfo.CurrentCulture);
            hourlyRateBox.Text = value.HourlyRate.ToString(CultureInfo.CurrentCulture);
        }
    }

    public void SetPresenter(MainViewPresenter presenter) => this.presenter = presenter;

    public void ShowError(string title, OperationResult response)
    {
        var errors = response.Errors.Concat(response.PropertyErrors.SelectMany(x => x.Value)).DefaultIfEmpty("Неизвестная ошибка");
        MessageBox.Show(this, string.Join(Environment.NewLine, errors), title, MessageBoxButtons.OK, MessageBoxIcon.Error);
    }

    public bool ConfirmDelete() => MessageBox.Show(this, "Удалить выбранный проект?", "Подтверждение", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes;

    private async Task LoginAndLoadAsync()
    {
        try
        {
            await apiClient.LoginAsync(email.Text, password.Text);
            new MainViewPresenter(apiClient, this);
            await presenter!.LoadData();
        }
        catch (Exception exception)
        {
            ShowError("Ошибка входа", OperationResult.Failure(exception.Message));
        }
    }

    private bool TryReadProject()
    {
        if (!decimal.TryParse(budgetBox.Text, NumberStyles.Number, CultureInfo.CurrentCulture, out var budget) || budget < 0 ||
            !decimal.TryParse(hourlyRateBox.Text, NumberStyles.Number, CultureInfo.CurrentCulture, out var hourlyRate) || hourlyRate < 0)
        {
            ShowError("Проверь данные", OperationResult.Failure("Бюджет и цена за час должны быть неотрицательными числами."));
            return false;
        }
        currentProject.Name = nameBox.Text;
        currentProject.StartDate = DateOnly.FromDateTime(startDate.Value);
        currentProject.Deadline = DateOnly.FromDateTime(deadline.Value);
        currentProject.Budget = budget;
        currentProject.HourlyRate = hourlyRate;
        return true;
    }

    private static void AddField(TableLayoutPanel panel, string label, Control control)
    {
        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        panel.Controls.Add(new Label { Text = label, AutoSize = true, Padding = new Padding(0, 6, 10, 6) }, 0, panel.RowCount);
        panel.Controls.Add(control, 1, panel.RowCount);
        panel.RowCount++;
    }
}
