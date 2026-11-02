using FluentValidation;
using KooliProjekt.Application.Data;
using KooliProjekt.Application.Data.Repositories;
using KooliProjekt.Application.Infrastructure;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KooliProjekt.Application.Features.ProjectTasks;

public sealed class ProjectTaskListItem
{
    public int Id { get; init; }
    public int ProjectId { get; init; }
    public string ProjectName { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public DateOnly StartDate { get; init; }
    public decimal EstimatedHours { get; init; }
    public decimal ActualHours { get; init; }
    public decimal? FixedPrice { get; init; }
    public decimal ActualCost { get; init; }
    public string? AssigneeId { get; init; }
    public string? AssigneeName { get; init; }
    public string? Description { get; init; }
    public bool IsCompleted { get; init; }
}

public sealed class ListProjectTasksQuery : IRequest<OperationResult<PagedResult<ProjectTaskListItem>>>
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public int? ProjectId { get; init; }
    public string? Search { get; init; }
}

public sealed class GetProjectTaskQuery(int id) : IRequest<OperationResult<ProjectTaskListItem?>>
{
    public int Id { get; } = id;
}

public sealed class SaveProjectTaskCommand : IRequest<OperationResult<int>>
{
    public int Id { get; init; }
    public int ProjectId { get; init; }
    public string Title { get; init; } = string.Empty;
    public DateOnly StartDate { get; init; }
    public decimal EstimatedHours { get; init; }
    public string? AssigneeId { get; init; }
    public string? Description { get; init; }
    public bool IsCompleted { get; init; }
    public decimal? FixedPrice { get; init; }
}

public sealed class DeleteProjectTaskCommand(int id) : IRequest<OperationResult<bool>>
{
    public int Id { get; } = id;
}

public sealed class SaveProjectTaskCommandValidator : AbstractValidator<SaveProjectTaskCommand>
{
    public SaveProjectTaskCommandValidator()
    {
        RuleFor(x => x.ProjectId).GreaterThan(0);
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.EstimatedHours).GreaterThanOrEqualTo(0);
        RuleFor(x => x.FixedPrice).GreaterThanOrEqualTo(0).When(x => x.FixedPrice.HasValue);
        RuleFor(x => x.Description).MaximumLength(4000).When(x => x.Description is not null);
    }
}

public sealed class ListProjectTasksQueryHandler(IProjectTaskRepository repository)
    : IRequestHandler<ListProjectTasksQuery, OperationResult<PagedResult<ProjectTaskListItem>>>
{
    public async Task<OperationResult<PagedResult<ProjectTaskListItem>>> Handle(ListProjectTasksQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.Page <= 0) throw new ArgumentException("Page must be greater than zero.", nameof(request));
        if (request.PageSize is <= 0 or > 100) throw new ArgumentException("Page size must be between 1 and 100.", nameof(request));

        var query = repository.Query().AsNoTracking();
        if (request.ProjectId.HasValue) query = query.Where(x => x.ProjectId == request.ProjectId);
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim();
            query = query.Where(x => x.Title.Contains(search) || (x.Description != null && x.Description.Contains(search)));
        }

        var value = await query.OrderBy(x => x.StartDate).ThenBy(x => x.Title)
            .SelectProjectTaskItems().ToPagedResultAsync(request.Page, request.PageSize, cancellationToken);
        return OperationResult<PagedResult<ProjectTaskListItem>>.Success(value);
    }
}

public sealed class GetProjectTaskQueryHandler(IProjectTaskRepository repository)
    : IRequestHandler<GetProjectTaskQuery, OperationResult<ProjectTaskListItem?>>
{
    public async Task<OperationResult<ProjectTaskListItem?>> Handle(GetProjectTaskQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.Id <= 0) return OperationResult<ProjectTaskListItem?>.Success(null);
        var value = await repository.Query().AsNoTracking().Where(x => x.Id == request.Id)
            .SelectProjectTaskItems().FirstOrDefaultAsync(cancellationToken);
        return OperationResult<ProjectTaskListItem?>.Success(value);
    }
}

public sealed class SaveProjectTaskCommandHandler(IProjectTaskRepository repository, ApplicationDbContext dbContext)
    : IRequestHandler<SaveProjectTaskCommand, OperationResult<int>>
{
    public async Task<OperationResult<int>> Handle(SaveProjectTaskCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (!await dbContext.Projects.AnyAsync(x => x.Id == request.ProjectId, cancellationToken))
            return OperationResult<int>.Failure("Project was not found.");
        if (request.AssigneeId is not null && !await dbContext.Users.AnyAsync(x => x.Id == request.AssigneeId, cancellationToken))
            return OperationResult<int>.Failure("Assignee was not found.");

        var entity = request.Id == 0 ? null : await repository.GetAsync(request.Id, cancellationToken);
        if (request.Id != 0 && entity is null) return OperationResult<int>.Failure("Task was not found.");
        entity ??= new ProjectTask();
        entity.ProjectId = request.ProjectId;
        entity.Title = request.Title.Trim();
        entity.StartDate = request.StartDate;
        entity.EstimatedHours = request.EstimatedHours;
        entity.AssigneeId = request.AssigneeId;
        entity.Description = request.Description?.Trim();
        entity.IsCompleted = request.IsCompleted;
        entity.FixedPrice = request.FixedPrice;
        if (entity.Id == 0) await repository.AddAsync(entity, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
        return OperationResult<int>.Success(entity.Id);
    }
}

public sealed class DeleteProjectTaskCommandHandler(IProjectTaskRepository repository)
    : IRequestHandler<DeleteProjectTaskCommand, OperationResult<bool>>
{
    public async Task<OperationResult<bool>> Handle(DeleteProjectTaskCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.Id <= 0) return OperationResult<bool>.Success(false);
        var entity = await repository.GetAsync(request.Id, cancellationToken);
        if (entity is null) return OperationResult<bool>.Success(false);
        repository.Remove(entity);
        await repository.SaveChangesAsync(cancellationToken);
        return OperationResult<bool>.Success(true);
    }
}

internal static class ProjectTaskProjection
{
    public static IQueryable<ProjectTaskListItem> SelectProjectTaskItems(this IQueryable<ProjectTask> query) =>
        query.Select(x => new ProjectTaskListItem
        {
            Id = x.Id,
            ProjectId = x.ProjectId,
            ProjectName = x.Project.Name,
            Title = x.Title,
            StartDate = x.StartDate,
            EstimatedHours = x.EstimatedHours,
            ActualHours = x.WorkLogs.Sum(w => w.Hours),
            FixedPrice = x.FixedPrice,
            ActualCost = x.FixedPrice ?? (x.WorkLogs.Sum(w => w.Hours) * x.Project.HourlyRate),
            AssigneeId = x.AssigneeId,
            AssigneeName = x.Assignee == null ? null : x.Assignee.DisplayName,
            Description = x.Description,
            IsCompleted = x.IsCompleted
        });
}
