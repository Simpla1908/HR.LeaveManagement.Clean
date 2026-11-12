namespace HR.LeaveManagement.Api;

public static class CorsServiceRegistration
{
    public static IServiceCollection AddClientCors(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddCors(options => options.AddPolicy("BlazorClient", policy =>
            policy.WithOrigins(configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
                ?? ["https://localhost:7040"])
                .AllowAnyHeader()
                .AllowAnyMethod()));
        return services;
    }
}
