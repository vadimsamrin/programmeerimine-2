using KooliProjekt.Application.Features.ProjectTeam;
using KooliProjekt.Application.Infrastructure;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KooliProjekt.WebApi.Controllers;

[ApiController]
[Authorize]
[Route("api/projects/{projectId:int}/team")]
public sealed class ProjectTeamController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<OperationResult<IReadOnlyList<ProjectTeamMemberItem>>>> List(int projectId, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new ListProjectTeamQuery(projectId), cancellationToken);
        return result.HasErrors ? NotFound(result) : Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<OperationResult<int>>> Save(int projectId, SaveProjectTeamMemberCommand command, CancellationToken cancellationToken)
    {
        if (projectId != command.ProjectId) return BadRequest(OperationResult<int>.Failure("Project id in route and body must match."));
        var result = await sender.Send(command, cancellationToken);
        return result.HasErrors ? BadRequest(result) : Ok(result);
    }

    [HttpDelete("{id:int}")]
    public async Task<ActionResult<OperationResult<bool>>> Delete(int projectId, int id, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new DeleteProjectTeamMemberCommand(projectId, id), cancellationToken);
        return result.Value ? Ok(result) : NotFound(result);
    }
}
