using AuthServer.Infrastructure;

namespace AuthServer.API.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApi(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddControllers();
        services.AddAuthorization();
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen();

        services.AddInfrastructure(configuration);

        return services;
    }
}