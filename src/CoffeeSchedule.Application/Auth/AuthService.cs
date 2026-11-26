using CoffeeSchedule.Application.Abstractions;
using CoffeeSchedule.Domain.Entities;
using CoffeeSchedule.Domain.Enums;
using CoffeeSchedule.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace CoffeeSchedule.Application.Auth;

public sealed class AuthService(
    IAppDbContext dbContext,
    IPasswordHasherService passwordHasher,
    IJwtTokenGenerator jwtTokenGenerator)
{
    public async Task<AuthResponse> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var employee = await dbContext.Employees
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Email == normalizedEmail, cancellationToken);

        if (employee is null
            || !employee.IsActive
            || string.IsNullOrEmpty(employee.PasswordHash)
            || !passwordHasher.VerifyPassword(request.Password, employee.PasswordHash))
        {
            throw new UnauthorizedException("Неверный email или пароль.");
        }

        var token = jwtTokenGenerator.GenerateToken(employee);
        return new AuthResponse(token, employee.Id, employee.FullName, employee.Position);
    }

    public async Task<AuthResponse> BootstrapManagerAsync(
        BootstrapManagerRequest request,
        CancellationToken cancellationToken = default)
    {
        var hasEmployees = await dbContext.Employees.AnyAsync(cancellationToken);
        if (hasEmployees)
        {
            throw new ConflictException("Начальная инициализация уже выполнена.");
        }

        var manager = Employee.Create(
            request.FullName,
            request.Email,
            request.Phone,
            Position.Manager);

        manager.SetPasswordHash(passwordHasher.HashPassword(request.Password));

        dbContext.Employees.Add(manager);
        await dbContext.SaveChangesAsync(cancellationToken);

        var token = jwtTokenGenerator.GenerateToken(manager);
        return new AuthResponse(token, manager.Id, manager.FullName, manager.Position);
    }
}
