using KooliProjekt.Application.Data;
using KooliProjekt.Application.Features.ProjectTasks;
using KooliProjekt.Application.Infrastructure;
using KooliProjekt.WebApi.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KooliProjekt.WebApi.Controllers;

[ApiController]
[Authorize]
[Route("api/tasks")]
public sealed class ProjectTasksController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<OperationResult<PagedResult<ProjectTaskListItem>>>> List(
        [FromQuery] ListProjectTasksQuery query, CancellationToken cancellationToken)
    {
        var result = await sender.Send(query, cancellationToken);
        return result.HasErrors ? BadRequest(result) : Ok(result);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<OperationResult<ProjectTaskListItem?>>> Get(int id, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetProjectTaskQuery(id), cancellationToken);
        return result.Value is null ? NotFound(result) : Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<OperationResult<int>>> Save(SaveProjectTaskRequest request, CancellationToken cancellationToken)
    {
        var command = new SaveProjectTaskCommand
        {
            Id = request.Id,
            ProjectId = request.ProjectId,
            Title = request.Title,
            StartDate = request.StartDate,
            EstimatedHours = request.EstimatedHours,
            AssigneeId = request.AssigneeId,
            Description = request.Description,
            IsCompleted = request.IsCompleted,
            FixedPrice = request.FixedPrice
        };
        var result = await sender.Send(command, cancellationToken);
        return result.HasErrors ? BadRequest(result) : Ok(result);
    }

    [HttpDelete("{id:int}")]
    public async Task<ActionResult<OperationResult<bool>>> Delete(int id, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new DeleteProjectTaskCommand(id), cancellationToken);
        return result.Value ? Ok(result) : NotFound(result);
    }
}
