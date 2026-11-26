using CoffeeSchedule.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoffeeSchedule.Infrastructure.Persistence.Configurations;

internal sealed class ShiftConfiguration : IEntityTypeConfiguration<Shift>
{
    public void Configure(EntityTypeBuilder<Shift> builder)
    {
        builder.ToTable("shifts");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.StartsAtUtc)
            .IsRequired();

        builder.Property(x => x.EndsAtUtc)
            .IsRequired();

        builder.Property(x => x.RequiredPosition)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(32);

        builder.Property(x => x.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(32);

        builder.Property(x => x.Notes)
            .HasMaxLength(500);

        builder.Property(x => x.CreatedAtUtc)
            .IsRequired();

        builder.HasOne(x => x.CoffeeShop)
            .WithMany()
            .HasForeignKey(x => x.CoffeeShopId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.AssignedEmployee)
            .WithMany()
            .HasForeignKey(x => x.AssignedEmployeeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.CoffeeShopId, x.StartsAtUtc, x.EndsAtUtc });
        builder.HasIndex(x => new { x.AssignedEmployeeId, x.StartsAtUtc, x.EndsAtUtc });
    }
}
