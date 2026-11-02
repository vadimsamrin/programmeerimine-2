using FluentValidation;
using KooliProjekt.Application.Data;
using KooliProjekt.Application.Data.Repositories;
using KooliProjekt.Application.Infrastructure;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace KooliProjekt.Application.Features.ProjectTeam;

public sealed class ProjectTeamMemberItem
{
    public int Id { get; init; }
    public string UserId { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public string? Email { get; init; }
    public string Role { get; init; } = string.Empty;
}

public sealed class ListProjectTeamQuery(int projectId) : IRequest<OperationResult<IReadOnlyList<ProjectTeamMemberItem>>>
{
    public int ProjectId { get; } = projectId;
}

public sealed class SaveProjectTeamMemberCommand : IRequest<OperationResult<int>>
{
    public int Id { get; init; }
    public int ProjectId { get; init; }
    public string UserId { get; init; } = string.Empty;
    public string Role { get; init; } = "Team member";
}

public sealed class DeleteProjectTeamMemberCommand(int projectId, int id) : IRequest<OperationResult<bool>>
{
    public int ProjectId { get; } = projectId;
    public int Id { get; } = id;
}

public sealed class SaveProjectTeamMemberCommandValidator : AbstractValidator<SaveProjectTeamMemberCommand>
{
    public SaveProjectTeamMemberCommandValidator()
    {
        RuleFor(x => x.ProjectId).GreaterThan(0);
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.Role).NotEmpty().MaximumLength(80);
    }
}

public sealed class ListProjectTeamQueryHandler(ApplicationDbContext dbContext)
    : IRequestHandler<ListProjectTeamQuery, OperationResult<IReadOnlyList<ProjectTeamMemberItem>>>
{
    public async Task<OperationResult<IReadOnlyList<ProjectTeamMemberItem>>> Handle(ListProjectTeamQuery request, CancellationToken cancellationToken)
    {
        if (request.ProjectId <= 0) return OperationResult<IReadOnlyList<ProjectTeamMemberItem>>.Failure("Project id must be positive.");
        var exists = await dbContext.Projects.AnyAsync(x => x.Id == request.ProjectId, cancellationToken);
        if (!exists) return OperationResult<IReadOnlyList<ProjectTeamMemberItem>>.Failure("Project was not found.");

        var team = await dbContext.ProjectTeamMembers.AsNoTracking()
            .Where(x => x.ProjectId == request.ProjectId)
            .OrderBy(x => x.User.DisplayName)
            .Select(x => new ProjectTeamMemberItem
            {
                Id = x.Id,
                UserId = x.UserId,
                DisplayName = x.User.DisplayName,
                Email = x.User.Email,
                Role = x.Role
            })
            .ToListAsync(cancellationToken);
        return OperationResult<IReadOnlyList<ProjectTeamMemberItem>>.Success(team);
    }
}

public sealed class SaveProjectTeamMemberCommandHandler(
    IProjectTeamMemberRepository repository,
    ApplicationDbContext dbContext)
    : IRequestHandler<SaveProjectTeamMemberCommand, OperationResult<int>>
{
    public async Task<OperationResult<int>> Handle(SaveProjectTeamMemberCommand request, CancellationToken cancellationToken)
    {
        var projectExists = await dbContext.Projects.AnyAsync(x => x.Id == request.ProjectId, cancellationToken);
        if (!projectExists) return OperationResult<int>.Failure("Project was not found.");
        var userExists = await dbContext.Users.AnyAsync(x => x.Id == request.UserId, cancellationToken);
        if (!userExists) return OperationResult<int>.Failure("Selected user was not found.");

        var member = request.Id == 0 ? null : await repository.GetAsync(request.Id, cancellationToken);
        if (request.Id != 0 && (member is null || member.ProjectId != request.ProjectId))
            return OperationResult<int>.Failure("Project team member was not found.");

        var duplicate = await dbContext.ProjectTeamMembers.AnyAsync(
            x => x.ProjectId == request.ProjectId && x.UserId == request.UserId && x.Id != request.Id,
            cancellationToken);
        if (duplicate) return OperationResult<int>.Failure("This user is already in the project team.");

        member ??= new ProjectTeamMember { ProjectId = request.ProjectId };
        member.UserId = request.UserId;
        member.Role = request.Role.Trim();
        if (member.Id == 0) await repository.AddAsync(member, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
        return OperationResult<int>.Success(member.Id);
    }
}

public sealed class DeleteProjectTeamMemberCommandHandler(IProjectTeamMemberRepository repository)
    : IRequestHandler<DeleteProjectTeamMemberCommand, OperationResult<bool>>
{
    public async Task<OperationResult<bool>> Handle(DeleteProjectTeamMemberCommand request, CancellationToken cancellationToken)
    {
        var member = await repository.GetAsync(request.Id, cancellationToken);
        if (member is null || member.ProjectId != request.ProjectId) return OperationResult<bool>.Success(false);
        repository.Remove(member);
        await repository.SaveChangesAsync(cancellationToken);
        return OperationResult<bool>.Success(true);
    }
}
