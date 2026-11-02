using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace KooliProjekt.Application.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<ProjectTeamMember> ProjectTeamMembers => Set<ProjectTeamMember>();
    public DbSet<ProjectTask> ProjectTasks => Set<ProjectTask>();
    public DbSet<WorkLog> WorkLogs => Set<WorkLog>();
    public DbSet<TaskAttachment> TaskAttachments => Set<TaskAttachment>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Project>(entity =>
        {
            entity.Property(x => x.Budget).HasPrecision(18, 2);
            entity.Property(x => x.HourlyRate).HasPrecision(18, 2);
        });

        builder.Entity<ProjectTask>(entity =>
        {
            entity.Property(x => x.EstimatedHours).HasPrecision(10, 2);
            entity.Property(x => x.FixedPrice).HasPrecision(18, 2);
            entity.HasOne(x => x.Assignee)
                .WithMany()
                .HasForeignKey(x => x.AssigneeId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<WorkLog>(entity =>
        {
            entity.Property(x => x.Hours).HasPrecision(8, 2);
            entity.HasOne(x => x.Performer)
                .WithMany()
                .HasForeignKey(x => x.PerformerId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<ProjectTeamMember>()
            .HasIndex(x => new { x.ProjectId, x.UserId })
            .IsUnique();
    }
}
