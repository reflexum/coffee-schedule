using CoffeeSchedule.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoffeeSchedule.Infrastructure.Persistence.Configurations;

internal sealed class CoffeeShopConfiguration : IEntityTypeConfiguration<CoffeeShop>
{
    public void Configure(EntityTypeBuilder<CoffeeShop> builder)
    {
        builder.ToTable("coffee_shops");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(x => x.Address)
            .IsRequired()
            .HasMaxLength(300);

        builder.Property(x => x.TimeZoneId)
            .IsRequired()
            .HasMaxLength(64);

        builder.Property(x => x.OpeningTime)
            .IsRequired();

        builder.Property(x => x.ClosingTime)
            .IsRequired();

        builder.Property(x => x.IsActive)
            .IsRequired();

        builder.Property(x => x.CreatedAtUtc)
            .IsRequired();

        builder.HasIndex(x => x.Name);

        builder.Navigation(x => x.EmployeeAccesses)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
