using Microsoft.EntityFrameworkCore;

namespace KooliProjekt.Application.Data.Repositories;

public class ProjectRepository(ApplicationDbContext dbContext) : BaseRepository<Project>(dbContext), IProjectRepository
{
    public Task<Project?> GetDetailsAsync(int id, CancellationToken cancellationToken = default) =>
        Query()
            .Include(x => x.TeamMembers).ThenInclude(x => x.User)
            .Include(x => x.Tasks).ThenInclude(x => x.WorkLogs)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
}
