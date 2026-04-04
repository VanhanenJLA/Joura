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
        var connectionString = configuration.GetConnectionString("PostgreSql")
            ?? "Host=localhost;Port=5432;Database=joura;Username=joura;Password=joura";

        services.AddHttpContextAccessor();
        services.AddDbContext<JouraDbContext>(
            options => options.UseNpgsql(connectionString),
            optionsLifetime: ServiceLifetime.Singleton);
        services.AddDbContextFactory<JouraDbContext>(options => options.UseNpgsql(connectionString));
        services.AddScoped<ICurrentUserContext, HttpCurrentUserContext>();
        services.AddScoped<IWorkTrackingService, WorkTrackingService>();
        return services;
    }
}
