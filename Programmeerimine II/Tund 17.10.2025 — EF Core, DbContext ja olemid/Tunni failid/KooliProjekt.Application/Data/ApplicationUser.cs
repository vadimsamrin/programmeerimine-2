using Microsoft.AspNetCore.Identity;

namespace KooliProjekt.Application.Data;

public class ApplicationUser : IdentityUser
{
    public string DisplayName { get; set; } = string.Empty;
}
