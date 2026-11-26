using CoffeeSchedule.Application.Abstractions;
using CoffeeSchedule.Domain.Entities;
using CoffeeSchedule.Domain.Enums;
using CoffeeSchedule.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace CoffeeSchedule.Application.Preferences;

public sealed class PreferenceService(IAppDbContext dbContext, TimeProvider timeProvider)
{
    public async Task<IReadOnlyList<ShiftPreferenceResponse>> SubmitForEmployeeAsync(
        Guid employeeId,
        SubmitPreferencesRequest request,
        CancellationToken cancellationToken = default)
    {
        var employee = await dbContext.Employees
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == employeeId, cancellationToken)
            ?? throw new NotFoundException($"Сотрудник '{employeeId}' не найден.");

        if (!employee.IsActive)
        {
            throw new DomainException("Неактивный сотрудник не может подавать пожелания.");
        }

        var todayUtc = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
        if (request.FromDate > request.ToDate)
        {
            throw new DomainException("Конечная дата периода не может быть раньше начальной.");
        }

        var existingPreferences = await dbContext.ShiftPreferences
            .AsNoTracking()
            .Where(x => x.EmployeeId == employeeId && x.Date >= request.FromDate && x.Date <= request.ToDate)
            .ToListAsync(cancellationToken);

        var created = new List<ShiftPreference>();
        var nowUtc = timeProvider.GetUtcNow();

        for (var date = request.FromDate; date <= request.ToDate; date = date.AddDays(1))
        {
            var hasOverlap = existingPreferences
                .Where(x => x.Date == date)
                .Any(x => x.OverlapsTimeWindow(request.StartTime, request.EndTime));

            if (hasOverlap)
            {
                throw new ConflictException(
                    $"На дату {date:yyyy-MM-dd} уже существует пересекающееся пожелание сотрудника.");
            }

            var preference = ShiftPreference.Create(
                employeeId,
                date,
                request.StartTime,
                request.EndTime,
                request.Type,
                request.Comment,
                todayUtc,
                nowUtc);

            created.Add(preference);
        }

        dbContext.ShiftPreferences.AddRange(created);
        await dbContext.SaveChangesAsync(cancellationToken);

        return created
            .Select(p => MapToResponse(p, employee.FullName, employee.Position))
            .ToList();
    }

    public async Task<IReadOnlyList<ShiftPreferenceResponse>> GetEmployeePreferencesAsync(
        Guid employeeId,
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken = default)
    {
        if (from > to)
        {
            throw new DomainException("Конечная дата периода не может быть раньше начальной.");
        }

        var employeeExists = await dbContext.Employees
            .AnyAsync(x => x.Id == employeeId, cancellationToken);

        if (!employeeExists)
        {
            throw new NotFoundException($"Сотрудник '{employeeId}' не найден.");
        }

        var preferences = await dbContext.ShiftPreferences
            .AsNoTracking()
            .Include(x => x.Employee)
            .Where(x => x.EmployeeId == employeeId && x.Date >= from && x.Date <= to)
            .OrderBy(x => x.Date)
            .ThenBy(x => x.StartTime)
            .ToListAsync(cancellationToken);

        return preferences
            .Select(p => MapToResponse(p, p.Employee.FullName, p.Employee.Position))
            .ToList();
    }

    public async Task<IReadOnlyList<ShiftPreferenceResponse>> GetShopPreferencesAsync(
        Guid coffeeShopId,
        DateOnly from,
        DateOnly to,
        PreferenceType? type = null,
        CancellationToken cancellationToken = default)
    {
        if (from > to)
        {
            throw new DomainException("Конечная дата периода не может быть раньше начальной.");
        }

        var shopExists = await dbContext.CoffeeShops
            .AnyAsync(x => x.Id == coffeeShopId, cancellationToken);

        if (!shopExists)
        {
            throw new NotFoundException($"Кофейня '{coffeeShopId}' не найдена.");
        }

        var shopEmployeeIds = dbContext.EmployeeShopAccesses
            .Where(a => a.CoffeeShopId == coffeeShopId)
            .Select(a => a.EmployeeId);

        var query = dbContext.ShiftPreferences
            .AsNoTracking()
            .Include(x => x.Employee)
            .Where(x => x.Employee.IsActive
                        && shopEmployeeIds.Contains(x.EmployeeId)
                        && x.Date >= from
                        && x.Date <= to);

        if (type.HasValue)
        {
            query = query.Where(x => x.Type == type.Value);
        }

        var preferences = await query
            .OrderBy(x => x.Date)
            .ThenBy(x => x.Employee.FullName)
            .ThenBy(x => x.StartTime)
            .ToListAsync(cancellationToken);

        return preferences
            .Select(p => MapToResponse(p, p.Employee.FullName, p.Employee.Position))
            .ToList();
    }

    public async Task DeleteAsync(
        Guid employeeId,
        Guid preferenceId,
        CancellationToken cancellationToken = default)
    {
        var preference = await dbContext.ShiftPreferences
            .FirstOrDefaultAsync(x => x.Id == preferenceId && x.EmployeeId == employeeId, cancellationToken)
            ?? throw new NotFoundException($"Пожелание '{preferenceId}' не найдено.");

        dbContext.ShiftPreferences.Remove(preference);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static ShiftPreferenceResponse MapToResponse(
        ShiftPreference preference,
        string employeeName,
        Position employeePosition) =>
        new(
            preference.Id,
            preference.EmployeeId,
            employeeName,
            employeePosition,
            preference.Date,
            preference.StartTime,
            preference.EndTime,
            preference.Type,
            preference.Comment,
            preference.CreatedAtUtc);
}
