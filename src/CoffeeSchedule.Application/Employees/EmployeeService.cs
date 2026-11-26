using CoffeeSchedule.Application.Abstractions;
using CoffeeSchedule.Domain.Entities;
using CoffeeSchedule.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace CoffeeSchedule.Application.Employees;

public sealed class EmployeeService(
    IAppDbContext dbContext,
    IPasswordHasherService passwordHasher)
{
    public async Task<EmployeeResponse> CreateAsync(
        CreateEmployeeRequest request,
        CancellationToken cancellationToken = default)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var normalizedPhone = request.Phone.Trim();

        var emailOrPhoneExists = await dbContext.Employees
            .AnyAsync(
                x => x.Email == normalizedEmail || x.Phone == normalizedPhone,
                cancellationToken);

        if (emailOrPhoneExists)
        {
            throw new ConflictException("Сотрудник с таким email или телефоном уже существует.");
        }

        if (request.PrimaryCoffeeShopId.HasValue)
        {
            var shopExists = await dbContext.CoffeeShops
                .AnyAsync(x => x.Id == request.PrimaryCoffeeShopId.Value && x.IsActive, cancellationToken);

            if (!shopExists)
            {
                throw new NotFoundException($"Кофейня '{request.PrimaryCoffeeShopId.Value}' не найдена или неактивна.");
            }
        }

        var employee = Employee.Create(
            request.FullName,
            request.Email,
            request.Phone,
            request.Position,
            request.PrimaryCoffeeShopId);

        if (!string.IsNullOrWhiteSpace(request.Password))
        {
            employee.SetPasswordHash(passwordHasher.HashPassword(request.Password));
        }

        dbContext.Employees.Add(employee);
        await dbContext.SaveChangesAsync(cancellationToken);

        return MapToResponse(employee);
    }

    public async Task<IReadOnlyList<EmployeeResponse>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var employees = await dbContext.Employees
            .AsNoTracking()
            .Include(x => x.ShopAccesses)
            .OrderBy(x => x.FullName)
            .ToListAsync(cancellationToken);

        return employees.Select(MapToResponse).ToList();
    }

    public async Task GrantShopAccessAsync(
        Guid employeeId,
        Guid coffeeShopId,
        CancellationToken cancellationToken = default)
    {
        var employee = await dbContext.Employees
            .Include(x => x.ShopAccesses)
            .FirstOrDefaultAsync(x => x.Id == employeeId, cancellationToken)
            ?? throw new NotFoundException($"Сотрудник '{employeeId}' не найден.");

        var shopExists = await dbContext.CoffeeShops
            .AnyAsync(x => x.Id == coffeeShopId && x.IsActive, cancellationToken);

        if (!shopExists)
        {
            throw new NotFoundException($"Кофейня '{coffeeShopId}' не найдена или неактивна.");
        }

        employee.GrantShopAccess(coffeeShopId);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task SetPasswordAsync(
        Guid employeeId,
        SetPasswordRequest request,
        CancellationToken cancellationToken = default)
    {
        var employee = await dbContext.Employees
            .FirstOrDefaultAsync(x => x.Id == employeeId, cancellationToken)
            ?? throw new NotFoundException($"Сотрудник '{employeeId}' не найден.");

        employee.SetPasswordHash(passwordHasher.HashPassword(request.Password));
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static EmployeeResponse MapToResponse(Employee employee) =>
        new(
            employee.Id,
            employee.FullName,
            employee.Email,
            employee.Phone,
            employee.Position,
            employee.PrimaryCoffeeShopId,
            employee.IsActive,
            employee.ShopAccesses.Select(x => x.CoffeeShopId).ToList());
}
