
using HR.LeaveManagement.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HR.LeaveManagement.Persistence.Configurations;

public class LeaveRequestConfiguration
    : IEntityTypeConfiguration<LeaveRequest>
{
    public void Configure(
        EntityTypeBuilder<LeaveRequest> builder)
    {
        // Nom de la table.
        builder.ToTable("LeaveRequests");

        // Clé primaire.
        builder.HasKey(x => x.Id);

        // Identifiant du demandeur.
        builder.Property(x => x.RequestingEmployeeId)
            .IsRequired()
            .HasMaxLength(450);

        // Commentaire facultatif.
        builder.Property(x => x.RequestComments)
            .HasMaxLength(1000);

        // Relation avec LeaveType.
        builder.HasOne(x => x.LeaveType)
            .WithMany()
            .HasForeignKey(x => x.LeaveTypeId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);

        // Index de recherche.
        builder.HasIndex(x => x.RequestingEmployeeId);
    }
}
