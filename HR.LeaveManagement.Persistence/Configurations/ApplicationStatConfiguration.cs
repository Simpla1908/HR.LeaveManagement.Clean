using HR.LeaveManagement.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HR.LeaveManagement.Persistence.Configurations;

public sealed class ApplicationStatConfiguration : IEntityTypeConfiguration<ApplicationStat>
{
    public void Configure(EntityTypeBuilder<ApplicationStat> builder)
    {
        builder.ToTable("stats");
        builder.HasKey(stat => stat.Id);
        builder.HasIndex(stat => stat.CapturedAtUtc);
        builder.Property(stat => stat.PendingLeaveRequestAverageDays).HasPrecision(18, 2);
        builder.Property(stat => stat.ApprovedLeaveRequestAverageDays).HasPrecision(18, 2);
        builder.Property(stat => stat.CancelledLeaveRequestAverageDays).HasPrecision(18, 2);
        builder.Property(stat => stat.PendingLeaveRequestPercentage).HasPrecision(5, 2);
        builder.Property(stat => stat.ApprovedLeaveRequestPercentage).HasPrecision(5, 2);
        builder.Property(stat => stat.CancelledLeaveRequestPercentage).HasPrecision(5, 2);
    }
}
