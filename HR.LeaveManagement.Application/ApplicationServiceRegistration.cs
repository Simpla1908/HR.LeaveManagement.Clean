
using System.Reflection;
using AutoMapper;
using FluentValidation;
using HR.LeaveManagement.Application.Behaviors;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace HR.LeaveManagement.Application;

public static class ApplicationServiceRegistration
{
    public static IServiceCollection AddApplicationServices(
        this IServiceCollection services)
    {
        // Obtenir l'assembly Application.
        var assembly = Assembly.GetExecutingAssembly();
        services.AddScoped<Services.LeaveRequestWorkflow>();

        // Enregistrer les profils AutoMapper.
        services.AddAutoMapper(cfg => { }, assembly);

        // Enregistrer les handlers et le pipeline.
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(assembly);

            cfg.AddOpenBehavior(
                typeof(ValidationBehavior<,>));
        });

        // Découvrir automatiquement les validateurs.
        services.AddValidatorsFromAssembly(assembly);

        return services;
    }
}
