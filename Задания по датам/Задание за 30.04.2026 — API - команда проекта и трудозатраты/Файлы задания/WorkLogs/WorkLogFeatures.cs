using FluentValidation;
using KooliProjekt.Application.Data;
using KooliProjekt.Application.Data.Repositories;
using KooliProjekt.Application.Infrastructure;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KooliProjekt.Application.Features.WorkLogs;

public sealed class WorkLogListItem
{
    public int Id { get; init; }
    public int ProjectTaskId { get; init; }
    public string TaskTitle { get; init; } = string.Empty;
    public DateOnly WorkDate { get; init; }
    public decimal Hours { get; init; }
    public string PerformerId { get; init; } = string.Empty;
    public string PerformerName { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
}

public sealed class ListWorkLogsQuery : IRequest<OperationResult<PagedResult<WorkLogListItem>>>
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public int? ProjectTaskId { get; init; }
    public string? Search { get; init; }
}

public sealed class GetWorkLogQuery(int id) : IRequest<OperationResult<WorkLogListItem?>>
{
    public int Id { get; } = id;
}

public sealed class SaveWorkLogCommand : IRequest<OperationResult<int>>
{
    public int Id { get; init; }
    public int ProjectTaskId { get; init; }
    public DateOnly WorkDate { get; init; }
    public decimal Hours { get; init; }
    public string PerformerId { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
}

public sealed class DeleteWorkLogCommand(int id, string performerId) : IRequest<OperationResult<bool>>
{
    public int Id { get; } = id;
    public string PerformerId { get; } = performerId;
}

public sealed class SaveWorkLogCommandValidator : AbstractValidator<SaveWorkLogCommand>
{
    public SaveWorkLogCommandValidator()
    {
        RuleFor(x => x.ProjectTaskId).GreaterThan(0);
        RuleFor(x => x.PerformerId).NotEmpty();
        RuleFor(x => x.Hours).InclusiveBetween(0.01m, 24m);
        RuleFor(x => x.Description).NotEmpty().MaximumLength(2000);
    }
}

public sealed class ListWorkLogsQueryHandler(IWorkLogRepository repository)
    : IRequestHandler<ListWorkLogsQuery, OperationResult<PagedResult<WorkLogListItem>>>
{
    public async Task<OperationResult<PagedResult<WorkLogListItem>>> Handle(ListWorkLogsQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.Page <= 0) throw new ArgumentException("Page must be greater than zero.", nameof(request));
        if (request.PageSize is <= 0 or > 100) throw new ArgumentException("Page size must be between 1 and 100.", nameof(request));
        var query = repository.Query().AsNoTracking();
        if (request.ProjectTaskId.HasValue) query = query.Where(x => x.ProjectTaskId == request.ProjectTaskId);
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim();
            query = query.Where(x => x.Description.Contains(search));
        }
        var value = await query.OrderByDescending(x => x.WorkDate).ThenByDescending(x => x.Id)
            .SelectWorkLogItems().ToPagedResultAsync(request.Page, request.PageSize, cancellationToken);
        return OperationResult<PagedResult<WorkLogListItem>>.Success(value);
    }
}

public sealed class GetWorkLogQueryHandler(IWorkLogRepository repository)
    : IRequestHandler<GetWorkLogQuery, OperationResult<WorkLogListItem?>>
{
    public async Task<OperationResult<WorkLogListItem?>> Handle(GetWorkLogQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.Id <= 0) return OperationResult<WorkLogListItem?>.Success(null);
        var value = await repository.Query().AsNoTracking().Where(x => x.Id == request.Id)
            .SelectWorkLogItems().FirstOrDefaultAsync(cancellationToken);
        return OperationResult<WorkLogListItem?>.Success(value);
    }
}

public sealed class SaveWorkLogCommandHandler(IWorkLogRepository repository, ApplicationDbContext dbContext)
    : IRequestHandler<SaveWorkLogCommand, OperationResult<int>>
{
    public async Task<OperationResult<int>> Handle(SaveWorkLogCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (!await dbContext.ProjectTasks.AnyAsync(x => x.Id == request.ProjectTaskId, cancellationToken))
            return OperationResult<int>.Failure("Task was not found.");
        if (!await dbContext.Users.AnyAsync(x => x.Id == request.PerformerId, cancellationToken))
            return OperationResult<int>.Failure("Performer was not found.");

        var entity = request.Id == 0 ? null : await repository.GetAsync(request.Id, cancellationToken);
        if (request.Id != 0 && entity is null) return OperationResult<int>.Failure("Work log was not found.");
        entity ??= new WorkLog { PerformerId = request.PerformerId };
        if (entity.PerformerId != request.PerformerId) return OperationResult<int>.Failure("Only the performer may change this work log.");
        entity.ProjectTaskId = request.ProjectTaskId;
        entity.WorkDate = request.WorkDate;
        entity.Hours = request.Hours;
        entity.Description = request.Description.Trim();
        if (entity.Id == 0) await repository.AddAsync(entity, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
        return OperationResult<int>.Success(entity.Id);
    }
}

public sealed class DeleteWorkLogCommandHandler(IWorkLogRepository repository)
    : IRequestHandler<DeleteWorkLogCommand, OperationResult<bool>>
{
    public async Task<OperationResult<bool>> Handle(DeleteWorkLogCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.Id <= 0) return OperationResult<bool>.Success(false);
        var entity = await repository.GetAsync(request.Id, cancellationToken);
        if (entity is null) return OperationResult<bool>.Success(false);
        if (entity.PerformerId != request.PerformerId) return OperationResult<bool>.Failure("Only the performer may delete this work log.");
        repository.Remove(entity);
        await repository.SaveChangesAsync(cancellationToken);
        return OperationResult<bool>.Success(true);
    }
}

internal static class WorkLogProjection
{
    public static IQueryable<WorkLogListItem> SelectWorkLogItems(this IQueryable<WorkLog> query) => query.Select(x => new WorkLogListItem
    {
        Id = x.Id,
        ProjectTaskId = x.ProjectTaskId,
        TaskTitle = x.ProjectTask.Title,
        WorkDate = x.WorkDate,
        Hours = x.Hours,
        PerformerId = x.PerformerId,
        PerformerName = x.Performer.DisplayName,
        Description = x.Description
    });
}
