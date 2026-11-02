namespace KooliProjekt.Application.Data;

public static class SeedData
{
    public static async Task GenerateAsync(ApplicationDbContext dbContext, ApplicationUser user)
    {
        if (dbContext.Projects.Any()) return;

        for (var index = 1; index <= 10; index++)
        {
            var start = DateOnly.FromDateTime(DateTime.Today.AddDays(-index * 4));
            var project = new Project
            {
                Name = $"Näidisprojekt {index}",
                StartDate = start,
                Deadline = start.AddDays(30 + index),
                Budget = 3000 + index * 700,
                HourlyRate = 45 + index
            };
            var task = new ProjectTask
            {
                Title = $"Ülesanne {index}",
                StartDate = start,
                EstimatedHours = 8 + index,
                AssigneeId = user.Id,
                Description = "Automaatne testandmete ülesanne.",
                IsCompleted = index % 3 == 0,
                FixedPrice = index % 4 == 0 ? 500 + index * 20 : null
            };
            task.WorkLogs.Add(new WorkLog
            {
                WorkDate = start.AddDays(1),
                Hours = 2 + index / 2m,
                PerformerId = user.Id,
                Description = "Testandmete töölogi"
            });
            task.Attachments.Add(new TaskAttachment
            {
                FileName = $"sample-{index}.txt",
                StoragePath = $"seed/sample-{index}.txt",
                ContentType = "text/plain"
            });
            project.Tasks.Add(task);
            project.TeamMembers.Add(new ProjectTeamMember
            {
                UserId = user.Id,
                Role = "Project manager"
            });
            dbContext.Projects.Add(project);
        }

        await dbContext.SaveChangesAsync();
    }
}
