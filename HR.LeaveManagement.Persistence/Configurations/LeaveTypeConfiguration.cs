
using HR.LeaveManagement.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HR.LeaveManagement.Persistence.Configurations;

public class LeaveTypeConfiguration
    : IEntityTypeConfiguration<LeaveType>
{
    public void Configure(EntityTypeBuilder<LeaveType> builder)
    {
        // Nom de la table.
        builder.ToTable("LeaveTypes");

        // Clé primaire.
        builder.HasKey(x => x.Id);

        // Nom obligatoire et longueur maximale.
        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(100);

        // Nombre de jours obligatoire.
        builder.Property(x => x.DefaultDays)
            .IsRequired();

        // Donnée initiale.
        builder.HasData(
            new LeaveType
            {
                Id = 1,
                Name = "Vacation",
                DefaultDays = 10
            });
    }
}
