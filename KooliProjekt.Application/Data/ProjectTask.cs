using System.ComponentModel.DataAnnotations;

namespace KooliProjekt.Application.Data;

public class ProjectTask : Entity
{
    public int ProjectId { get; set; }
    public Project Project { get; set; } = null!;

    [Required, StringLength(200)]
    public string Title { get; set; } = string.Empty;

    public DateOnly StartDate { get; set; }

    [Range(0, 99999)]
    public decimal EstimatedHours { get; set; }

    public string? AssigneeId { get; set; }
    public ApplicationUser? Assignee { get; set; }

    [StringLength(4000)]
    public string? Description { get; set; }

    public bool IsCompleted { get; set; }

    [Range(0, 999999999)]
    public decimal? FixedPrice { get; set; }

    public List<WorkLog> WorkLogs { get; set; } = [];
    public List<TaskAttachment> Attachments { get; set; } = [];
}
