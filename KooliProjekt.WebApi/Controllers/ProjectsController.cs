using KooliProjekt.Application.Data;
using KooliProjekt.Application.Features.Projects;
using KooliProjekt.Application.Infrastructure;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KooliProjekt.WebApi.Controllers;

[ApiController]
[Authorize]
[Route("api/projects")]
public class ProjectsController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<OperationResult<PagedResult<ProjectListItem>>>> List(
        [FromQuery] ListProjectsQuery query,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(query, cancellationToken);
        return result.HasErrors ? BadRequest(result) : Ok(result);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<OperationResult<ProjectDetails?>>> Get(int id, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetProjectQuery(id), cancellationToken);
        return result.Value is null ? NotFound(result) : Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<OperationResult<int>>> Save(SaveProjectCommand command, CancellationToken cancellationToken)
    {
        var result = await sender.Send(command, cancellationToken);
        return result.HasErrors ? BadRequest(result) : Ok(result);
    }

    [HttpDelete("{id:int}")]
    public async Task<ActionResult<OperationResult<bool>>> Delete(int id, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new DeleteProjectCommand(id), cancellationToken);
        return result.Value ? Ok(result) : NotFound(result);
    }
}
