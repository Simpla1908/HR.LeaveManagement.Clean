
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace HR.LeaveManagement.Identity.Services;

public static class IdentityRoleSeeder
{
    public static async Task SeedRolesAsync(
        IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();

        var roleManager = scope.ServiceProvider
            .GetRequiredService<RoleManager<IdentityRole>>();

        string[] roles = ["Administrator", "Employee"];

        foreach (var roleName in roles)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                var result = await roleManager.CreateAsync(
                    new IdentityRole(roleName));

                if (!result.Succeeded)
                {
                    throw new InvalidOperationException(
                        $"Could not create role: {roleName}");
                }
            }
        }
    }
}
