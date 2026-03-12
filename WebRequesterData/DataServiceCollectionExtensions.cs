using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using WebRequesterData.Models;

namespace WebRequesterData;

public static class DataServiceCollectionExtensions
{
    public static IServiceCollection AddMyDatabaseServices(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<WebRequesterContext>(options =>
            options.UseSqlServer(connectionString));

        services.AddScoped<IDatabaseService, DatabaseService>();

        return services;
    }
}