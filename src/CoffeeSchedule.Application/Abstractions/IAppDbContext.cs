using CoffeeSchedule.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CoffeeSchedule.Application.Abstractions;

public interface IAppDbContext
{
    DbSet<CoffeeShop> CoffeeShops { get; }

    DbSet<Employee> Employees { get; }

    DbSet<EmployeeShopAccess> EmployeeShopAccesses { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
