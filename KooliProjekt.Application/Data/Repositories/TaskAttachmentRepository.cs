namespace KooliProjekt.Application.Data.Repositories;

public class TaskAttachmentRepository(ApplicationDbContext dbContext)
    : BaseRepository<TaskAttachment>(dbContext), ITaskAttachmentRepository { }
