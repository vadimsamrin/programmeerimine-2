using System.ComponentModel.DataAnnotations;

namespace KooliProjekt.Application.Data;

public class Project : Entity
{
    [Required, StringLength(200)]
    public string Name { get; set; } = string.Empty;

    public DateOnly StartDate { get; set; }
    public DateOnly Deadline { get; set; }

    [Range(0, 999999999)]
    public decimal Budget { get; set; }

    [Range(0, 999999)]
    public decimal HourlyRate { get; set; }

    public List<ProjectTeamMember> TeamMembers { get; set; } = [];
    public List<ProjectTask> Tasks { get; set; } = [];
}
