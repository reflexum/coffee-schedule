using CoffeeSchedule.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoffeeSchedule.Infrastructure.Persistence.Configurations;

internal sealed class ShiftPreferenceConfiguration : IEntityTypeConfiguration<ShiftPreference>
{
    public void Configure(EntityTypeBuilder<ShiftPreference> builder)
    {
        builder.ToTable("shift_preferences");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Date)
            .IsRequired();

        builder.Property(x => x.Type)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(32);

        builder.Property(x => x.Comment)
            .HasMaxLength(300);

        builder.Property(x => x.CreatedAtUtc)
            .IsRequired();

        builder.HasOne(x => x.Employee)
            .WithMany()
            .HasForeignKey(x => x.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.EmployeeId, x.Date });
        builder.HasIndex(x => x.Date);
    }
}
