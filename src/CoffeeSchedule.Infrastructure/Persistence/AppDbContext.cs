using CoffeeSchedule.Application.Abstractions;
using CoffeeSchedule.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CoffeeSchedule.Infrastructure.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options)
    : DbContext(options), IAppDbContext
{
    public DbSet<CoffeeShop> CoffeeShops => Set<CoffeeShop>();

    public DbSet<Employee> Employees => Set<Employee>();

    public DbSet<EmployeeShopAccess> EmployeeShopAccesses => Set<EmployeeShopAccess>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
