using KooliProjekt.Application.Data;
using KooliProjekt.WebApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KooliProjekt.WebApi.Controllers;

[ApiController]
[Authorize]
[Route("api/task-attachments")]
public class TaskAttachmentsController(ApplicationDbContext dbContext, IWebHostEnvironment environment) : ControllerBase
{
    private const long MaxUploadSize = 10 * 1024 * 1024;

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AttachmentResponse>>> List([FromQuery] int taskId, CancellationToken cancellationToken) =>
        Ok(await dbContext.TaskAttachments.AsNoTracking().Where(x => x.ProjectTaskId == taskId).OrderByDescending(x => x.UploadedAt)
            .Select(x => new AttachmentResponse
            {
                Id = x.Id,
                ProjectTaskId = x.ProjectTaskId,
                FileName = x.FileName,
                ContentType = x.ContentType,
                UploadedAt = x.UploadedAt
            }).ToListAsync(cancellationToken));

    [HttpPost]
    [RequestSizeLimit(MaxUploadSize)]
    public async Task<ActionResult<AttachmentResponse>> Upload([FromForm] int taskId, [FromForm] IFormFile file, CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0) return BadRequest("A non-empty file is required.");
        if (file.Length > MaxUploadSize) return BadRequest("Maximum file size is 10 MB.");
        if (!await dbContext.ProjectTasks.AnyAsync(x => x.Id == taskId, cancellationToken)) return BadRequest("Task was not found.");

        var uploadsDirectory = Path.Combine(environment.ContentRootPath, "uploads");
        Directory.CreateDirectory(uploadsDirectory);
        var storedName = $"{Guid.NewGuid():N}{Path.GetExtension(Path.GetFileName(file.FileName))}";
        var path = Path.Combine(uploadsDirectory, storedName);
        await using (var stream = System.IO.File.Create(path))
        {
            await file.CopyToAsync(stream, cancellationToken);
        }

        var attachment = new TaskAttachment
        {
            ProjectTaskId = taskId,
            FileName = Path.GetFileName(file.FileName),
            StoragePath = storedName,
            ContentType = string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType
        };
        dbContext.TaskAttachments.Add(attachment);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Ok(new AttachmentResponse
        {
            Id = attachment.Id,
            ProjectTaskId = attachment.ProjectTaskId,
            FileName = attachment.FileName,
            ContentType = attachment.ContentType,
            UploadedAt = attachment.UploadedAt
        });
    }

    [HttpGet("{id:int}/download")]
    public async Task<IActionResult> Download(int id, CancellationToken cancellationToken)
    {
        var attachment = await dbContext.TaskAttachments.FindAsync([id], cancellationToken);
        if (attachment is null) return NotFound();
        var path = Path.Combine(environment.ContentRootPath, "uploads", attachment.StoragePath);
        if (!System.IO.File.Exists(path)) return NotFound("File content was not found.");
        return File(System.IO.File.OpenRead(path), attachment.ContentType, attachment.FileName);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var attachment = await dbContext.TaskAttachments.FindAsync([id], cancellationToken);
        if (attachment is null) return NotFound();
        var path = Path.Combine(environment.ContentRootPath, "uploads", attachment.StoragePath);
        if (System.IO.File.Exists(path)) System.IO.File.Delete(path);
        dbContext.TaskAttachments.Remove(attachment);
        await dbContext.SaveChangesAsync(cancellationToken);
        return NoContent();
    }
}
