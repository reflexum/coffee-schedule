using CoffeeSchedule.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CoffeeSchedule.Application.Abstractions;

public interface IAppDbContext
{
    DbSet<CoffeeShop> CoffeeShops { get; }

    DbSet<Employee> Employees { get; }

    DbSet<EmployeeShopAccess> EmployeeShopAccesses { get; }

    DbSet<Shift> Shifts { get; }

    DbSet<ShiftPreference> ShiftPreferences { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
