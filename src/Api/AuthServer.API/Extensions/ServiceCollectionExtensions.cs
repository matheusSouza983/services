using AuthServer.Infrastructure;
using AuthServer.API.Filters;
using Microsoft.AspNetCore.Mvc;

namespace AuthServer.API.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApi(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddControllers(options =>
        {
            options.Filters.Add<ValidationFilter>();
            options.Filters.Add<StandardizeSuccessResponseFilter>();
        });

        services.Configure<ApiBehaviorOptions>(options =>
        {
            options.SuppressModelStateInvalidFilter = true;
        });

        services.AddScoped<ValidationFilter>();
        services.AddScoped<StandardizeSuccessResponseFilter>();

        services.AddAuthorization();
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen();

        services.AddInfrastructure(configuration);

        return services;
    }
}