namespace KooliProjekt.Application.Data.Repositories;

public interface IProjectRepository : IBaseRepository<Project>
{
    Task<Project?> GetDetailsAsync(int id, CancellationToken cancellationToken = default);
}
