namespace KooliProjekt.Application.Data.Repositories;

public class ProjectTeamMemberRepository(ApplicationDbContext dbContext)
    : BaseRepository<ProjectTeamMember>(dbContext), IProjectTeamMemberRepository { }
