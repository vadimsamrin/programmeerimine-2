namespace KooliProjekt.WebApi.Models;

public sealed class ProjectTaskResponse
{
    public int Id { get; init; }
    public int ProjectId { get; init; }
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

public sealed class SaveProjectTaskRequest
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

public sealed class WorkLogResponse
{
    public int Id { get; init; }
    public int ProjectTaskId { get; init; }
    public DateOnly WorkDate { get; init; }
    public decimal Hours { get; init; }
    public string PerformerId { get; init; } = string.Empty;
    public string PerformerName { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
}

public sealed class SaveWorkLogRequest
{
    public int Id { get; init; }
    public int ProjectTaskId { get; init; }
    public DateOnly WorkDate { get; init; }
    public decimal Hours { get; init; }
    public string Description { get; init; } = string.Empty;
}

public sealed class AttachmentResponse
{
    public int Id { get; init; }
    public int ProjectTaskId { get; init; }
    public string FileName { get; init; } = string.Empty;
    public string ContentType { get; init; } = string.Empty;
    public DateTimeOffset UploadedAt { get; init; }
}
