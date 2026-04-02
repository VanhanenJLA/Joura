using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Joura.Infrastructure.Persistence;

public sealed class JouraDbContextFactory : IDesignTimeDbContextFactory<JouraDbContext>
{
    public JouraDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<JouraDbContext>();
        const string connectionString = "Host=localhost;Port=5432;Database=joura;Username=joura;Password=joura";
        optionsBuilder.UseNpgsql(connectionString);
        return new JouraDbContext(optionsBuilder.Options);
    }
}
