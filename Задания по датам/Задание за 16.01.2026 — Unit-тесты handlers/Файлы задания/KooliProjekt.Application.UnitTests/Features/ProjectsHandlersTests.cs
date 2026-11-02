using KooliProjekt.Application.Data;
using KooliProjekt.Application.Data.Repositories;
using KooliProjekt.Application.Features.Projects;

namespace KooliProjekt.Application.UnitTests.Features;

public class ProjectsHandlersTests : TestBase
{
    [Fact]
    public async Task Get_should_throw_when_request_is_null()
    {
        var handler = new GetProjectQueryHandler(new ProjectRepository(GetDbContext()));
        await Assert.ThrowsAsync<ArgumentNullException>(() => handler.Handle(null!, CancellationToken.None));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    public async Task Get_should_return_null_when_request_id_is_zero_or_less(int id)
    {
        var handler = new GetProjectQueryHandler(new ProjectRepository(GetFaultyDbContext()));
        var result = await handler.Handle(new GetProjectQuery(id), CancellationToken.None);

        Assert.False(result.HasErrors);
        Assert.Null(result.Value);
    }

    [Theory]
    [InlineData(0, 10)]
    [InlineData(-1, 10)]
    [InlineData(1, 0)]
    [InlineData(1, 101)]
    public async Task List_should_throw_when_paging_is_invalid(int page, int pageSize)
    {
        var handler = new ListProjectsQueryHandler(new ProjectRepository(GetFaultyDbContext()));
        await Assert.ThrowsAsync<ArgumentException>(() => handler.Handle(
            new ListProjectsQuery { Page = page, PageSize = pageSize }, CancellationToken.None));
    }

    [Fact]
    public async Task Save_should_create_project_with_valid_command()
    {
        await using var dbContext = GetDbContext();
        var handler = new SaveProjectCommandHandler(new ProjectRepository(dbContext));
        var result = await handler.Handle(new SaveProjectCommand
        {
            Name = "Website renewal",
            StartDate = new DateOnly(2026, 9, 1),
            Deadline = new DateOnly(2026, 10, 1),
            Budget = 5000,
            HourlyRate = 55
        }, CancellationToken.None);

        Assert.False(result.HasErrors);
        Assert.True(result.Value > 0);
        Assert.Single(dbContext.Projects);
    }

    [Fact]
    public async Task List_should_return_saved_project()
    {
        await using var dbContext = GetDbContext();
        dbContext.Projects.Add(new Project
        {
            Name = "Mobile application",
            StartDate = new DateOnly(2026, 9, 1),
            Deadline = new DateOnly(2026, 12, 1),
            Budget = 10000,
            HourlyRate = 60
        });
        await dbContext.SaveChangesAsync();

        var handler = new ListProjectsQueryHandler(new ProjectRepository(dbContext));
        var result = await handler.Handle(new ListProjectsQuery { Page = 1, PageSize = 20 }, CancellationToken.None);

        Assert.False(result.HasErrors);
        Assert.Single(result.Value!.Results);
        Assert.Equal("Mobile application", result.Value.Results[0].Name);
    }
}
