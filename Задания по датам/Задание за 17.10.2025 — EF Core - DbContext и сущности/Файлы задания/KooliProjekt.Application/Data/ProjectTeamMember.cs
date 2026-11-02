using System.ComponentModel.DataAnnotations;

namespace KooliProjekt.Application.Data;

public class ProjectTeamMember : Entity
{
    public int ProjectId { get; set; }
    public Project Project { get; set; } = null!;

    [Required]
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser User { get; set; } = null!;

    [Required, StringLength(80)]
    public string Role { get; set; } = "Team member";
}
