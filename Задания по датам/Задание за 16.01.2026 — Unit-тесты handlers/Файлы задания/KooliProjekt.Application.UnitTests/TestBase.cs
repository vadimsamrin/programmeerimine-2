using KooliProjekt.Application.Data;
using Microsoft.EntityFrameworkCore;

namespace KooliProjekt.Application.UnitTests;

public abstract class TestBase
{
    protected static ApplicationDbContext GetDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    protected static ApplicationDbContext GetFaultyDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>();
        return new ApplicationDbContext(options.Options);
    }
}
