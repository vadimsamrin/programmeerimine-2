namespace KooliProjekt.Application.Data.Repositories;

public interface IBaseRepository<TEntity> where TEntity : Entity
{
    IQueryable<TEntity> Query();
    Task<TEntity?> GetAsync(int id, CancellationToken cancellationToken = default);
    Task AddAsync(TEntity entity, CancellationToken cancellationToken = default);
    void Remove(TEntity entity);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
