using System.Globalization;
using CoffeeSchedule.Application.Abstractions;
using CoffeeSchedule.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace CoffeeSchedule.Infrastructure.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options)
    : DbContext(options), IAppDbContext
{
    public DbSet<CoffeeShop> CoffeeShops => Set<CoffeeShop>();

    public DbSet<Employee> Employees => Set<Employee>();

    public DbSet<EmployeeShopAccess> EmployeeShopAccesses => Set<EmployeeShopAccess>();

    public DbSet<Shift> Shifts => Set<Shift>();

    public DbSet<ShiftPreference> ShiftPreferences => Set<ShiftPreference>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        if (Database.ProviderName == "Microsoft.EntityFrameworkCore.Sqlite")
        {
            ApplySqliteDateTimeOffsetConverters(modelBuilder);
        }
    }

    private static void ApplySqliteDateTimeOffsetConverters(ModelBuilder modelBuilder)
    {
        var converter = new ValueConverter<DateTimeOffset, string>(
            v => v.UtcDateTime.ToString("O", CultureInfo.InvariantCulture),
            v => DateTimeOffset.Parse(v, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal));

        var nullableConverter = new ValueConverter<DateTimeOffset?, string?>(
            v => v.HasValue ? v.Value.UtcDateTime.ToString("O", CultureInfo.InvariantCulture) : null,
            v => string.IsNullOrEmpty(v)
                ? null
                : DateTimeOffset.Parse(v, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal));

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties())
            {
                if (property.ClrType == typeof(DateTimeOffset))
                {
                    property.SetValueConverter(converter);
                }
                else if (property.ClrType == typeof(DateTimeOffset?))
                {
                    property.SetValueConverter(nullableConverter);
                }
            }
        }
    }
}
