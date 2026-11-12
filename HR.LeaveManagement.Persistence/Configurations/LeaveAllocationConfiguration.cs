
using HR.LeaveManagement.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HR.LeaveManagement.Persistence.Configurations;

public class LeaveAllocationConfiguration
    : IEntityTypeConfiguration<LeaveAllocation>
{
    public void Configure(
        EntityTypeBuilder<LeaveAllocation> builder)
    {
        // Nom de la table.
        builder.ToTable("LeaveAllocations");

        // Clé primaire.
        builder.HasKey(x => x.Id);

        // Nombre de jours obligatoire.
        builder.Property(x => x.NumberOfDays)
            .IsRequired();

        // Période obligatoire.
        builder.Property(x => x.Period)
            .IsRequired();

        // Identifiant de l'employé.
        builder.Property(x => x.EmployeeId)
            .IsRequired()
            .HasMaxLength(450);

        // Relation avec LeaveType.
        builder.HasOne(x => x.LeaveType)
            .WithMany()
            .HasForeignKey(x => x.LeaveTypeId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);

        // Index pour les recherches d'allocations.
        builder.HasIndex(x => new
        {
            x.EmployeeId,
            x.LeaveTypeId,
            x.Period
        }).IsUnique();
    }
}
