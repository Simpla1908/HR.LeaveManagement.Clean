
using System.Reflection;
using AutoMapper;
using FluentValidation;
using HR.LeaveManagement.Application.Features.LeaveRequest.Shared;
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
        services.AddScoped<LeaveRequestNotification>();

        // Enregistrer les profils AutoMapper.
        services.AddAutoMapper(cfg => { }, assembly);

        // Enregistrer les handlers MediatR.
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(assembly);
        });

        // Les validateurs sont injectés puis appelés directement dans les handlers.
        services.AddValidatorsFromAssembly(assembly);

        return services;
    }
}
