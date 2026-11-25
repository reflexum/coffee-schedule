using CoffeeSchedule.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoffeeSchedule.Infrastructure.Persistence.Configurations;

internal sealed class EmployeeConfiguration : IEntityTypeConfiguration<Employee>
{
    public void Configure(EntityTypeBuilder<Employee> builder)
    {
        builder.ToTable("employees");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.FullName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.Email)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(x => x.Phone)
            .IsRequired()
            .HasMaxLength(32);

        builder.Property(x => x.Position)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(32);

        builder.Property(x => x.IsActive)
            .IsRequired();

        builder.Property(x => x.CreatedAtUtc)
            .IsRequired();

        builder.HasIndex(x => x.Email)
            .IsUnique();

        builder.HasIndex(x => x.Phone)
            .IsUnique();

        builder.HasOne(x => x.PrimaryCoffeeShop)
            .WithMany()
            .HasForeignKey(x => x.PrimaryCoffeeShopId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Navigation(x => x.ShopAccesses)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
