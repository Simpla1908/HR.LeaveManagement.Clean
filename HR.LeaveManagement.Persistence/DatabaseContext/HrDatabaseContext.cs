
using HR.LeaveManagement.Application.Contracts.Identity;
using HR.LeaveManagement.Domain;
using HR.LeaveManagement.Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace HR.LeaveManagement.Persistence.DatabaseContext;

public class HrDatabaseContext : DbContext
{
    // Service utilisé pour identifier l'utilisateur connecté.
    private readonly IUserService? _userService;

    // Constructeur appelé par l'injection de dépendances.
    public HrDatabaseContext(
        DbContextOptions<HrDatabaseContext> options,
        IUserService? userService = null)
        : base(options)
    {
        _userService = userService;
    }

    // Table des types de congés.
    public DbSet<LeaveType> LeaveTypes { get; set; } = null!;

    // Table des allocations de congés.
    public DbSet<LeaveAllocation> LeaveAllocations { get; set; } = null!;

    // Table des demandes de congés.
    public DbSet<LeaveRequest> LeaveRequests { get; set; } = null!;

    // Configuration des entités et relations.
    protected override void OnModelCreating(
        ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Découvrir les configurations Fluent API.
        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(HrDatabaseContext).Assembly);
    }

    // Intercepter les enregistrements pour renseigner l'audit.
    public override async Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        var entries = ChangeTracker
            .Entries<BaseEntity>()
            .Where(entry =>
                entry.State == EntityState.Added ||
                entry.State == EntityState.Modified);

        foreach (var entry in entries)
        {
            // Toujours actualiser la date de modification.
            entry.Entity.DateModified = DateTime.UtcNow;
            entry.Entity.ModifiedBy = _userService?.UserId;

            // Initialiser la création uniquement pour une nouvelle entité.
            if (entry.State == EntityState.Added)
            {
                entry.Entity.DateCreated = DateTime.UtcNow;
                entry.Entity.CreatedBy = _userService?.UserId;
            }
            else
            {
                // Conserver les informations de création existantes.
                entry.Property(x => x.DateCreated)
                    .IsModified = false;

                entry.Property(x => x.CreatedBy)
                    .IsModified = false;
            }
        }

        return await base.SaveChangesAsync(cancellationToken);
    }
}
