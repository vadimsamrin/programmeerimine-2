using KooliProjekt.Application.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KooliProjekt.WebApi.Controllers;

[ApiController]
[Authorize]
[Route("api/users")]
public class UsersController(ApplicationDbContext dbContext) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken cancellationToken) => Ok(await dbContext.Users.AsNoTracking()
        .OrderBy(x => x.DisplayName)
        .Select(x => new { x.Id, x.DisplayName, x.Email })
        .ToListAsync(cancellationToken));
}
