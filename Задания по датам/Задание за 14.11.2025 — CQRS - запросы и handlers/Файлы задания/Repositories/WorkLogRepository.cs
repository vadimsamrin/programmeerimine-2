namespace KooliProjekt.Application.Data.Repositories;

public class WorkLogRepository(ApplicationDbContext dbContext)
    : BaseRepository<WorkLog>(dbContext), IWorkLogRepository { }
