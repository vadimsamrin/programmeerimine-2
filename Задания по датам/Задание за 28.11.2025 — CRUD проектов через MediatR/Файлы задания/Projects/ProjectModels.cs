using MediatR;
using Microsoft.EntityFrameworkCore;
using KooliProjekt.Application.Data;
using KooliProjekt.Application.Data.Repositories;
using KooliProjekt.Application.Infrastructure;

namespace KooliProjekt.Application.Features.Projects;

public class ProjectListItem
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

public sealed class ProjectDetails : ProjectListItem
{
    public decimal HourlyRate { get; init; }
    public IReadOnlyList<ProjectTaskDetails> Tasks { get; init; } = [];
}

public sealed class ProjectTaskDetails
{
    public int Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public decimal EstimatedHours { get; init; }
    public decimal ActualHours { get; init; }
    public decimal? FixedPrice { get; init; }
    public bool IsCompleted { get; init; }
}

public sealed class SaveProjectCommand : IRequest<OperationResult<int>>
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public DateOnly StartDate { get; init; }
    public DateOnly Deadline { get; init; }
    public decimal Budget { get; init; }
    public decimal HourlyRate { get; init; }
}

public sealed class ListProjectsQuery : IRequest<OperationResult<PagedResult<ProjectListItem>>>
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public string? Search { get; init; }
}

public sealed class GetProjectQuery(int id) : IRequest<OperationResult<ProjectDetails?>>
{
    public int Id { get; } = id;
}

public sealed class DeleteProjectCommand(int id) : IRequest<OperationResult<bool>>
{
    public int Id { get; } = id;
}

public sealed class ListProjectsQueryHandler(IProjectRepository repository)
    : IRequestHandler<ListProjectsQuery, OperationResult<PagedResult<ProjectListItem>>>
{
    public async Task<OperationResult<PagedResult<ProjectListItem>>> Handle(ListProjectsQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.Page <= 0) throw new ArgumentException("Page must be greater than zero.", nameof(request));
        if (request.PageSize <= 0 || request.PageSize > 100) throw new ArgumentException("Page size must be between 1 and 100.", nameof(request));

        var query = repository.Query().AsNoTracking();
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim();
            query = query.Where(x => x.Name.Contains(search));
        }

        var projects = await query
            .OrderBy(x => x.Deadline)
            .Select(x => new ProjectListItem
            {
                Id = x.Id,
                Name = x.Name,
                StartDate = x.StartDate,
                Deadline = x.Deadline,
                Budget = x.Budget,
                TaskCount = x.Tasks.Count,
                CompletedTaskCount = x.Tasks.Count(t => t.IsCompleted),
                CurrentCost = x.Tasks.Sum(t => t.FixedPrice ?? (t.WorkLogs.Sum(w => w.Hours) * x.HourlyRate))
            })
            .ToPagedResultAsync(request.Page, request.PageSize, cancellationToken);

        return OperationResult<PagedResult<ProjectListItem>>.Success(projects);
    }
}

public sealed class GetProjectQueryHandler(IProjectRepository repository)
    : IRequestHandler<GetProjectQuery, OperationResult<ProjectDetails?>>
{
    public async Task<OperationResult<ProjectDetails?>> Handle(GetProjectQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.Id <= 0) return OperationResult<ProjectDetails?>.Success(null);

        var project = await repository.GetDetailsAsync(request.Id, cancellationToken);
        if (project is null) return OperationResult<ProjectDetails?>.Success(null);

        var tasks = project.Tasks.Select(t => new ProjectTaskDetails
        {
            Id = t.Id,
            Title = t.Title,
            EstimatedHours = t.EstimatedHours,
            ActualHours = t.WorkLogs.Sum(w => w.Hours),
            FixedPrice = t.FixedPrice,
            IsCompleted = t.IsCompleted
        }).ToList();

        var cost = project.Tasks.Sum(t => t.FixedPrice ?? (t.WorkLogs.Sum(w => w.Hours) * project.HourlyRate));
        return OperationResult<ProjectDetails?>.Success(new ProjectDetails
        {
            Id = project.Id,
            Name = project.Name,
            StartDate = project.StartDate,
            Deadline = project.Deadline,
            Budget = project.Budget,
            HourlyRate = project.HourlyRate,
            CurrentCost = cost,
            TaskCount = tasks.Count,
            CompletedTaskCount = tasks.Count(t => t.IsCompleted),
            Tasks = tasks
        });
    }
}

public sealed class SaveProjectCommandHandler(IProjectRepository repository)
    : IRequestHandler<SaveProjectCommand, OperationResult<int>>
{
    public async Task<OperationResult<int>> Handle(SaveProjectCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (string.IsNullOrWhiteSpace(request.Name)) return OperationResult<int>.Failure("Project name is required.");
        if (request.Deadline < request.StartDate) return OperationResult<int>.Failure("Deadline cannot be earlier than start date.");
        if (request.Budget < 0 || request.HourlyRate < 0) return OperationResult<int>.Failure("Budget and hourly rate must not be negative.");

        var project = request.Id == 0 ? null : await repository.GetAsync(request.Id, cancellationToken);
        if (request.Id != 0 && project is null) return OperationResult<int>.Failure("Project was not found.");
        project ??= new Project();

        project.Name = request.Name.Trim();
        project.StartDate = request.StartDate;
        project.Deadline = request.Deadline;
        project.Budget = request.Budget;
        project.HourlyRate = request.HourlyRate;
        if (project.Id == 0) await repository.AddAsync(project, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
        return OperationResult<int>.Success(project.Id);
    }
}

public sealed class DeleteProjectCommandHandler(IProjectRepository repository)
    : IRequestHandler<DeleteProjectCommand, OperationResult<bool>>
{
    public async Task<OperationResult<bool>> Handle(DeleteProjectCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.Id <= 0) return OperationResult<bool>.Success(false);
        var project = await repository.GetAsync(request.Id, cancellationToken);
        if (project is null) return OperationResult<bool>.Success(false);
        repository.Remove(project);
        await repository.SaveChangesAsync(cancellationToken);
        return OperationResult<bool>.Success(true);
    }
}
