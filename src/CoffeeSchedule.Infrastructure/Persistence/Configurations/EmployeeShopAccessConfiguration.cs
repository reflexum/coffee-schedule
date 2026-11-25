using CoffeeSchedule.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoffeeSchedule.Infrastructure.Persistence.Configurations;

internal sealed class EmployeeShopAccessConfiguration : IEntityTypeConfiguration<EmployeeShopAccess>
{
    public void Configure(EntityTypeBuilder<EmployeeShopAccess> builder)
    {
        builder.ToTable("employee_shop_accesses");

        builder.HasKey(x => new { x.EmployeeId, x.CoffeeShopId });

        builder.Property(x => x.AssignedAtUtc)
            .IsRequired();

        builder.HasOne(x => x.Employee)
            .WithMany(x => x.ShopAccesses)
            .HasForeignKey(x => x.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.CoffeeShop)
            .WithMany(x => x.EmployeeAccesses)
            .HasForeignKey(x => x.CoffeeShopId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
