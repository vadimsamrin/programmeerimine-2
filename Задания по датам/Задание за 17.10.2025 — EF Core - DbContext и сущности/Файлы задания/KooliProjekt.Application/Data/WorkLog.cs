using System.ComponentModel.DataAnnotations;

namespace KooliProjekt.Application.Data;

public class WorkLog : Entity
{
    public int ProjectTaskId { get; set; }
    public ProjectTask ProjectTask { get; set; } = null!;

    public DateOnly WorkDate { get; set; }

    [Range(0.01, 24)]
    public decimal Hours { get; set; }

    [Required]
    public string PerformerId { get; set; } = string.Empty;
    public ApplicationUser Performer { get; set; } = null!;

    [Required, StringLength(2000)]
    public string Description { get; set; } = string.Empty;
}
