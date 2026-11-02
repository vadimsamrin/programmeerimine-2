using System.ComponentModel.DataAnnotations;

namespace KooliProjekt.Application.Data;

public class TaskAttachment : Entity
{
    public int ProjectTaskId { get; set; }
    public ProjectTask ProjectTask { get; set; } = null!;

    [Required, StringLength(260)]
    public string FileName { get; set; } = string.Empty;

    [Required, StringLength(500)]
    public string StoragePath { get; set; } = string.Empty;

    [Required, StringLength(120)]
    public string ContentType { get; set; } = "application/octet-stream";

    public DateTimeOffset UploadedAt { get; set; } = DateTimeOffset.UtcNow;
}
