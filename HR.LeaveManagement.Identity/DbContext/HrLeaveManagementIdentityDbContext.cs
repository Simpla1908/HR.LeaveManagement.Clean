
using HR.LeaveManagement.Identity.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace HR.LeaveManagement.Identity.DbContext;

public class HrLeaveManagementIdentityDbContext
    : IdentityDbContext<ApplicationUser>
{
    public HrLeaveManagementIdentityDbContext(
        DbContextOptions<HrLeaveManagementIdentityDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        // Configurer les tables et relations standard d'Identity.
        base.OnModelCreating(builder);

        // Charger les configurations Fluent API de Identity.
        builder.ApplyConfigurationsFromAssembly(
            typeof(HrLeaveManagementIdentityDbContext).Assembly);
    }
}
