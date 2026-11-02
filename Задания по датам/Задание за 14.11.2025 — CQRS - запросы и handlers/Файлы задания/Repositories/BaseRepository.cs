using Microsoft.EntityFrameworkCore;

namespace KooliProjekt.Application.Data.Repositories;

public class BaseRepository<TEntity>(ApplicationDbContext dbContext) : IBaseRepository<TEntity>
    where TEntity : Entity
{
    protected ApplicationDbContext DbContext { get; } = dbContext;
    protected DbSet<TEntity> Entities => DbContext.Set<TEntity>();

    public IQueryable<TEntity> Query() => Entities.AsQueryable();
    public Task<TEntity?> GetAsync(int id, CancellationToken cancellationToken = default) =>
        Entities.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    public Task AddAsync(TEntity entity, CancellationToken cancellationToken = default) =>
        Entities.AddAsync(entity, cancellationToken).AsTask();
    public void Remove(TEntity entity) => Entities.Remove(entity);
    public Task SaveChangesAsync(CancellationToken cancellationToken = default) => DbContext.SaveChangesAsync(cancellationToken);
}
