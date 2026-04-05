using Joura.Application.Abstractions;
using Joura.Infrastructure.Persistence;
using Joura.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Joura.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var databaseProvider = ResolveDatabaseProvider(configuration);
        var connectionString = GetConnectionString(configuration, databaseProvider);

        services.AddHttpContextAccessor();
        services.AddDbContext<JouraDbContext>(
            options => ConfigureDbContext(options, databaseProvider, connectionString),
            optionsLifetime: ServiceLifetime.Singleton);
        services.AddDbContextFactory<JouraDbContext>(options => ConfigureDbContext(options, databaseProvider, connectionString));
        services.AddScoped<ICurrentUserContext, HttpCurrentUserContext>();
        services.AddScoped<IWorkTrackingService, WorkTrackingService>();
        return services;
    }

    public static DatabaseProvider ResolveDatabaseProvider(IConfiguration configuration)
    {
        var configuredProvider = configuration["DatabaseProvider"];
        return Enum.TryParse<DatabaseProvider>(configuredProvider, ignoreCase: true, out var provider)
            ? provider
            : DatabaseProvider.PostgreSql;
    }

    public static string GetConnectionString(IConfiguration configuration, DatabaseProvider databaseProvider) =>
        databaseProvider switch
        {
            DatabaseProvider.PostgreSql => configuration.GetConnectionString("PostgreSql")
                ?? "Host=localhost;Port=5432;Database=joura;Username=joura;Password=joura",
            DatabaseProvider.SqlServer => configuration.GetConnectionString("SqlServer")
                ?? "Server=(localdb)\\MSSQLLocalDB;Database=Joura;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True",
            _ => throw new InvalidOperationException($"Unsupported database provider '{databaseProvider}'.")
        };

    public static void ConfigureDbContext(
        DbContextOptionsBuilder options,
        DatabaseProvider databaseProvider,
        string connectionString)
    {
        switch (databaseProvider)
        {
            case DatabaseProvider.PostgreSql:
                options.UseNpgsql(connectionString);
                break;
            case DatabaseProvider.SqlServer:
                options.UseSqlServer(connectionString, sqlServerOptions =>
                {
                    sqlServerOptions.EnableRetryOnFailure(
                        maxRetryCount: 5,
                        maxRetryDelay: TimeSpan.FromSeconds(30),
                        errorNumbersToAdd: null);
                });
                break;
            default:
                throw new InvalidOperationException($"Unsupported database provider '{databaseProvider}'.");
        }
    }
}
