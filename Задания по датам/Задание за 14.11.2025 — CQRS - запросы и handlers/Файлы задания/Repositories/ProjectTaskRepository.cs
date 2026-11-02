namespace KooliProjekt.Application.Data.Repositories;

public class ProjectTaskRepository(ApplicationDbContext dbContext)
    : BaseRepository<ProjectTask>(dbContext), IProjectTaskRepository { }
