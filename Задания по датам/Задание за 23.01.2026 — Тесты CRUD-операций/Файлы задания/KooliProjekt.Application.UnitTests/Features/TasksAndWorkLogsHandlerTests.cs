using KooliProjekt.Application.Data;
using KooliProjekt.Application.Data.Repositories;
using KooliProjekt.Application.Features.ProjectTasks;
using KooliProjekt.Application.Features.WorkLogs;

namespace KooliProjekt.Application.UnitTests.Features;

public class TasksAndWorkLogsHandlerTests : TestBase
{
    [Fact]
    public async Task Save_task_should_create_task_for_existing_project()
    {
        await using var db = GetDbContext();
        var project = await AddProject(db);
        var handler = new SaveProjectTaskCommandHandler(new ProjectTaskRepository(db), db);

        var result = await handler.Handle(new SaveProjectTaskCommand
        {
            ProjectId = project.Id,
            Title = "Create prototype",
            StartDate = new DateOnly(2026, 9, 10),
            EstimatedHours = 12,
            Description = "Initial clickable prototype"
        }, CancellationToken.None);

        Assert.False(result.HasErrors);
        Assert.True(result.Value > 0);
        Assert.Single(db.ProjectTasks);
    }

    [Fact]
    public async Task Save_task_should_reject_missing_project()
    {
        await using var db = GetDbContext();
        var handler = new SaveProjectTaskCommandHandler(new ProjectTaskRepository(db), db);

        var result = await handler.Handle(new SaveProjectTaskCommand
        {
            ProjectId = 100,
            Title = "Orphan task",
            StartDate = new DateOnly(2026, 9, 10)
        }, CancellationToken.None);

        Assert.True(result.HasErrors);
        Assert.Contains("Project was not found.", result.Errors);
    }

    [Theory]
    [InlineData(0, 20)]
    [InlineData(1, 0)]
    [InlineData(1, 101)]
    public async Task List_tasks_should_reject_invalid_paging(int page, int pageSize)
    {
        var handler = new ListProjectTasksQueryHandler(new ProjectTaskRepository(GetFaultyDbContext()));

        await Assert.ThrowsAsync<ArgumentException>(() => handler.Handle(
            new ListProjectTasksQuery { Page = page, PageSize = pageSize }, CancellationToken.None));
    }

    [Fact]
    public async Task Get_task_should_return_null_for_non_positive_id_without_database()
    {
        var handler = new GetProjectTaskQueryHandler(new ProjectTaskRepository(GetFaultyDbContext()));

        var result = await handler.Handle(new GetProjectTaskQuery(0), CancellationToken.None);

        Assert.False(result.HasErrors);
        Assert.Null(result.Value);
    }

    [Fact]
    public async Task Save_work_log_should_create_log_for_its_performer()
    {
        await using var db = GetDbContext();
        var project = await AddProject(db);
        var user = await AddUser(db);
        var task = await AddTask(db, project.Id);
        var handler = new SaveWorkLogCommandHandler(new WorkLogRepository(db), db);

        var result = await handler.Handle(new SaveWorkLogCommand
        {
            ProjectTaskId = task.Id,
            PerformerId = user.Id,
            WorkDate = new DateOnly(2026, 9, 10),
            Hours = 3.5m,
            Description = "Prepared the project plan"
        }, CancellationToken.None);

        Assert.False(result.HasErrors);
        Assert.True(result.Value > 0);
        Assert.Single(db.WorkLogs);
    }

    [Fact]
    public async Task Save_work_log_should_reject_unknown_performer()
    {
        await using var db = GetDbContext();
        var project = await AddProject(db);
        var task = await AddTask(db, project.Id);
        var handler = new SaveWorkLogCommandHandler(new WorkLogRepository(db), db);

        var result = await handler.Handle(new SaveWorkLogCommand
        {
            ProjectTaskId = task.Id,
            PerformerId = "not-a-user",
            WorkDate = new DateOnly(2026, 9, 10),
            Hours = 2,
            Description = "Should fail"
        }, CancellationToken.None);

        Assert.True(result.HasErrors);
        Assert.Contains("Performer was not found.", result.Errors);
    }

    [Fact]
    public async Task Delete_work_log_should_reject_another_user()
    {
        await using var db = GetDbContext();
        var project = await AddProject(db);
        var performer = await AddUser(db, "performer");
        var anotherUser = await AddUser(db, "another");
        var task = await AddTask(db, project.Id);
        var workLog = new WorkLog
        {
            ProjectTaskId = task.Id,
            PerformerId = performer.Id,
            WorkDate = new DateOnly(2026, 9, 10),
            Hours = 2,
            Description = "Private entry"
        };
        db.WorkLogs.Add(workLog);
        await db.SaveChangesAsync();
        var handler = new DeleteWorkLogCommandHandler(new WorkLogRepository(db));

        var result = await handler.Handle(new DeleteWorkLogCommand(workLog.Id, anotherUser.Id), CancellationToken.None);

        Assert.True(result.HasErrors);
        Assert.Single(db.WorkLogs);
    }

    [Fact]
    public async Task Delete_work_log_should_remove_own_log()
    {
        await using var db = GetDbContext();
        var project = await AddProject(db);
        var user = await AddUser(db);
        var task = await AddTask(db, project.Id);
        var workLog = new WorkLog
        {
            ProjectTaskId = task.Id,
            PerformerId = user.Id,
            WorkDate = new DateOnly(2026, 9, 10),
            Hours = 2,
            Description = "Own entry"
        };
        db.WorkLogs.Add(workLog);
        await db.SaveChangesAsync();
        var handler = new DeleteWorkLogCommandHandler(new WorkLogRepository(db));

        var result = await handler.Handle(new DeleteWorkLogCommand(workLog.Id, user.Id), CancellationToken.None);

        Assert.False(result.HasErrors);
        Assert.True(result.Value);
        Assert.Empty(db.WorkLogs);
    }

    [Theory]
    [InlineData(0, 20)]
    [InlineData(1, 0)]
    [InlineData(1, 101)]
    public async Task List_work_logs_should_reject_invalid_paging(int page, int pageSize)
    {
        var handler = new ListWorkLogsQueryHandler(new WorkLogRepository(GetFaultyDbContext()));

        await Assert.ThrowsAsync<ArgumentException>(() => handler.Handle(
            new ListWorkLogsQuery { Page = page, PageSize = pageSize }, CancellationToken.None));
    }

    private static async Task<Project> AddProject(ApplicationDbContext db)
    {
        var project = new Project
        {
            Name = "Internal portal",
            StartDate = new DateOnly(2026, 9, 1),
            Deadline = new DateOnly(2026, 10, 1),
            Budget = 5000,
            HourlyRate = 50
        };
        db.Projects.Add(project);
        await db.SaveChangesAsync();
        return project;
    }

    private static async Task<ApplicationUser> AddUser(ApplicationDbContext db, string suffix = "worker")
    {
        var user = new ApplicationUser
        {
            Id = $"{suffix}-id",
            UserName = $"{suffix}@project.local",
            Email = $"{suffix}@project.local",
            DisplayName = suffix
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user;
    }

    private static async Task<ProjectTask> AddTask(ApplicationDbContext db, int projectId)
    {
        var task = new ProjectTask
        {
            ProjectId = projectId,
            Title = "Plan project",
            StartDate = new DateOnly(2026, 9, 1),
            EstimatedHours = 8
        };
        db.ProjectTasks.Add(task);
        await db.SaveChangesAsync();
        return task;
    }
}
