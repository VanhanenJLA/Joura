using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Joura.Infrastructure.Persistence;

public sealed class JouraDbContextFactory : IDesignTimeDbContextFactory<JouraDbContext>
{
    public JouraDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .AddEnvironmentVariables()
            .Build();
        var databaseProvider = ResolveDatabaseProvider(args, configuration);
        var connectionString = ResolveConnectionString(args, configuration, databaseProvider);
        var optionsBuilder = new DbContextOptionsBuilder<JouraDbContext>();
        DependencyInjection.ConfigureDbContext(optionsBuilder, databaseProvider, connectionString);
        return new JouraDbContext(optionsBuilder.Options);
    }

    private static DatabaseProvider ResolveDatabaseProvider(string[] args, IConfiguration configuration)
    {
        var providerArgument = TryGetArgValue(args, "--provider");
        if (Enum.TryParse<DatabaseProvider>(providerArgument, ignoreCase: true, out var provider))
        {
            return provider;
        }

        return DependencyInjection.ResolveDatabaseProvider(configuration);
    }

    private static string ResolveConnectionString(
        string[] args,
        IConfiguration configuration,
        DatabaseProvider databaseProvider)
    {
        var explicitConnectionString = TryGetArgValue(args, "--connection");
        return !string.IsNullOrWhiteSpace(explicitConnectionString)
            ? explicitConnectionString
            : DependencyInjection.GetConnectionString(configuration, databaseProvider);
    }

    private static string? TryGetArgValue(string[] args, string optionName)
    {
        for (var i = 0; i < args.Length - 1; i++)
        {
            if (string.Equals(args[i], optionName, StringComparison.OrdinalIgnoreCase))
            {
                return args[i + 1];
            }
        }

        return null;
    }
}
