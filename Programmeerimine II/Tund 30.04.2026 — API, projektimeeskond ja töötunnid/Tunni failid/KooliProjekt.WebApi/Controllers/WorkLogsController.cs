using System.Security.Claims;
using KooliProjekt.Application.Data;
using KooliProjekt.Application.Features.WorkLogs;
using KooliProjekt.Application.Infrastructure;
using KooliProjekt.WebApi.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KooliProjekt.WebApi.Controllers;

[ApiController]
[Authorize]
[Route("api/work-logs")]
public sealed class WorkLogsController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<OperationResult<PagedResult<WorkLogListItem>>>> List(
        [FromQuery] ListWorkLogsQuery query, CancellationToken cancellationToken)
    {
        var result = await sender.Send(query, cancellationToken);
        return result.HasErrors ? BadRequest(result) : Ok(result);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<OperationResult<WorkLogListItem?>>> Get(int id, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetWorkLogQuery(id), cancellationToken);
        return result.Value is null ? NotFound(result) : Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<OperationResult<int>>> Save(SaveWorkLogRequest request, CancellationToken cancellationToken)
    {
        var performerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(performerId)) return Unauthorized();
        var result = await sender.Send(new SaveWorkLogCommand
        {
            Id = request.Id,
            ProjectTaskId = request.ProjectTaskId,
            WorkDate = request.WorkDate,
            Hours = request.Hours,
            PerformerId = performerId,
            Description = request.Description
        }, cancellationToken);
        return result.HasErrors ? BadRequest(result) : Ok(result);
    }

    [HttpDelete("{id:int}")]
    public async Task<ActionResult<OperationResult<bool>>> Delete(int id, CancellationToken cancellationToken)
    {
        var performerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(performerId)) return Unauthorized();
        var result = await sender.Send(new DeleteWorkLogCommand(id, performerId), cancellationToken);
        return result.HasErrors ? BadRequest(result) : result.Value ? Ok(result) : NotFound(result);
    }
}
