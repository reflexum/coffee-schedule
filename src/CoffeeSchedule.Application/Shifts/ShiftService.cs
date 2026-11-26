using CoffeeSchedule.Application.Abstractions;
using CoffeeSchedule.Domain.Entities;
using CoffeeSchedule.Domain.Enums;
using CoffeeSchedule.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace CoffeeSchedule.Application.Shifts;

public sealed class ShiftService(IAppDbContext dbContext)
{
    public async Task<ShiftResponse> CreateAsync(
        CreateShiftRequest request,
        CancellationToken cancellationToken = default)
    {
        var shop = await dbContext.CoffeeShops
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == request.CoffeeShopId, cancellationToken)
            ?? throw new NotFoundException($"Кофейня '{request.CoffeeShopId}' не найдена.");

        if (!shop.IsActive)
        {
            throw new DomainException("Нельзя создать смену в неактивной кофейне.");
        }

        var shift = Shift.Create(
            request.CoffeeShopId,
            request.StartsAt,
            request.EndsAt,
            request.RequiredPosition,
            request.AssignedEmployeeId,
            request.Notes);

        Employee? employee = null;
        IReadOnlyList<string> warnings = [];
        if (request.AssignedEmployeeId.HasValue)
        {
            employee = await ValidateEmployeeAssignmentAsync(
                request.AssignedEmployeeId.Value,
                request.CoffeeShopId,
                request.RequiredPosition,
                shift.StartsAtUtc,
                shift.EndsAtUtc,
                excludeShiftId: null,
                cancellationToken);

            warnings = await GetPreferenceConflictWarningsAsync(
                employee.Id,
                shop.TimeZoneId,
                shift.StartsAtUtc,
                shift.EndsAtUtc,
                cancellationToken);
        }

        dbContext.Shifts.Add(shift);
        await dbContext.SaveChangesAsync(cancellationToken);

        return MapToResponse(shift, shop.Name, employee?.FullName, warnings);
    }

    public async Task<ShiftResponse> UpdateAsync(
        Guid shiftId,
        UpdateShiftRequest request,
        CancellationToken cancellationToken = default)
    {
        var shift = await dbContext.Shifts
            .Include(x => x.CoffeeShop)
            .FirstOrDefaultAsync(x => x.Id == shiftId, cancellationToken)
            ?? throw new NotFoundException($"Смена '{shiftId}' не найдена.");

        var startsAtUtc = request.StartsAt.ToUniversalTime();
        var endsAtUtc = request.EndsAt.ToUniversalTime();

        Employee? employee = null;
        IReadOnlyList<string> warnings = [];
        if (request.AssignedEmployeeId.HasValue)
        {
            employee = await ValidateEmployeeAssignmentAsync(
                request.AssignedEmployeeId.Value,
                shift.CoffeeShopId,
                request.RequiredPosition,
                startsAtUtc,
                endsAtUtc,
                excludeShiftId: shift.Id,
                cancellationToken);

            warnings = await GetPreferenceConflictWarningsAsync(
                employee.Id,
                shift.CoffeeShop.TimeZoneId,
                startsAtUtc,
                endsAtUtc,
                cancellationToken);
        }

        shift.UpdateSchedule(
            request.StartsAt,
            request.EndsAt,
            request.RequiredPosition,
            request.AssignedEmployeeId,
            request.Notes);

        await dbContext.SaveChangesAsync(cancellationToken);

        return MapToResponse(shift, shift.CoffeeShop.Name, employee?.FullName, warnings);
    }

    public async Task<ShiftResponse> PublishAsync(
        Guid shiftId,
        CancellationToken cancellationToken = default)
    {
        var shift = await dbContext.Shifts
            .Include(x => x.CoffeeShop)
            .Include(x => x.AssignedEmployee)
            .FirstOrDefaultAsync(x => x.Id == shiftId, cancellationToken)
            ?? throw new NotFoundException($"Смена '{shiftId}' не найдена.");

        shift.Publish();
        await dbContext.SaveChangesAsync(cancellationToken);

        return MapToResponse(shift, shift.CoffeeShop.Name, shift.AssignedEmployee?.FullName);
    }

    public async Task<PublishScheduleResponse> PublishShopScheduleAsync(
        Guid coffeeShopId,
        PublishScheduleRequest request,
        CancellationToken cancellationToken = default)
    {
        var shop = await dbContext.CoffeeShops
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == coffeeShopId, cancellationToken)
            ?? throw new NotFoundException($"Кофейня '{coffeeShopId}' не найдена.");

        var fromUtc = request.From.ToUniversalTime();
        var toUtc = request.To.ToUniversalTime();

        var draftShifts = await dbContext.Shifts
            .Include(x => x.AssignedEmployee)
            .Where(x => x.CoffeeShopId == coffeeShopId
                        && x.Status == ShiftStatus.Draft
                        && x.StartsAtUtc < toUtc
                        && fromUtc < x.EndsAtUtc)
            .OrderBy(x => x.StartsAtUtc)
            .ToListAsync(cancellationToken);

        if (draftShifts.Any(x => x.AssignedEmployeeId is null))
        {
            throw new DomainException(
                "Нельзя опубликовать график: в выбранном периоде есть смены без назначенного сотрудника.");
        }

        var publishedAt = DateTimeOffset.UtcNow;
        foreach (var shift in draftShifts)
        {
            shift.Publish(publishedAt);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        var responses = draftShifts
            .Select(s => MapToResponse(s, shop.Name, s.AssignedEmployee?.FullName))
            .ToList();

        return new PublishScheduleResponse(coffeeShopId, responses.Count, responses);
    }

    public async Task<ShiftResponse> CancelAsync(
        Guid shiftId,
        CancellationToken cancellationToken = default)
    {
        var shift = await dbContext.Shifts
            .Include(x => x.CoffeeShop)
            .Include(x => x.AssignedEmployee)
            .FirstOrDefaultAsync(x => x.Id == shiftId, cancellationToken)
            ?? throw new NotFoundException($"Смена '{shiftId}' не найдена.");

        shift.Cancel();
        await dbContext.SaveChangesAsync(cancellationToken);

        return MapToResponse(shift, shift.CoffeeShop.Name, shift.AssignedEmployee?.FullName);
    }

    public async Task<IReadOnlyList<ShiftResponse>> GetShopScheduleAsync(
        Guid coffeeShopId,
        DateTimeOffset from,
        DateTimeOffset to,
        ShiftStatus? status = null,
        CancellationToken cancellationToken = default)
    {
        var shopExists = await dbContext.CoffeeShops
            .AnyAsync(x => x.Id == coffeeShopId, cancellationToken);

        if (!shopExists)
        {
            throw new NotFoundException($"Кофейня '{coffeeShopId}' не найдена.");
        }

        var fromUtc = from.ToUniversalTime();
        var toUtc = to.ToUniversalTime();

        if (fromUtc >= toUtc)
        {
            throw new DomainException("Дата окончания периода должна быть больше даты начала.");
        }

        var query = dbContext.Shifts
            .AsNoTracking()
            .Include(x => x.CoffeeShop)
            .Include(x => x.AssignedEmployee)
            .Where(x => x.CoffeeShopId == coffeeShopId
                        && x.StartsAtUtc < toUtc
                        && fromUtc < x.EndsAtUtc);

        if (status.HasValue)
        {
            query = query.Where(x => x.Status == status.Value);
        }

        var shifts = await query
            .OrderBy(x => x.StartsAtUtc)
            .ToListAsync(cancellationToken);

        return shifts
            .Select(s => MapToResponse(s, s.CoffeeShop.Name, s.AssignedEmployee?.FullName))
            .ToList();
    }

    private async Task<Employee> ValidateEmployeeAssignmentAsync(
        Guid employeeId,
        Guid coffeeShopId,
        Position requiredPosition,
        DateTimeOffset startsAtUtc,
        DateTimeOffset endsAtUtc,
        Guid? excludeShiftId,
        CancellationToken cancellationToken)
    {
        var employee = await dbContext.Employees
            .AsNoTracking()
            .Include(x => x.ShopAccesses)
            .FirstOrDefaultAsync(x => x.Id == employeeId, cancellationToken)
            ?? throw new NotFoundException($"Сотрудник '{employeeId}' не найден.");

        if (!employee.IsActive)
        {
            throw new DomainException("Нельзя назначить неактивного сотрудника на смену.");
        }

        if (!employee.HasAccessToShop(coffeeShopId))
        {
            throw new DomainException("Сотрудник не закреплён за выбранной кофейней.");
        }

        if (!Shift.IsPositionCompatible(employee.Position, requiredPosition))
        {
            throw new DomainException(
                $"Должность сотрудника ({employee.Position}) недостаточна для позиции '{requiredPosition}'.");
        }

        var overlappingShift = await dbContext.Shifts
            .AsNoTracking()
            .Include(x => x.CoffeeShop)
            .FirstOrDefaultAsync(
                x => x.AssignedEmployeeId == employeeId
                     && x.Status != ShiftStatus.Cancelled
                     && (excludeShiftId == null || x.Id != excludeShiftId.Value)
                     && x.StartsAtUtc < endsAtUtc
                     && startsAtUtc < x.EndsAtUtc,
                cancellationToken);

        if (overlappingShift is not null)
        {
            throw new ConflictException(
                $"Сотрудник '{employee.FullName}' уже назначен на пересекающуюся смену в кофейне '{overlappingShift.CoffeeShop.Name}' ({overlappingShift.StartsAtUtc:HH:mm}–{overlappingShift.EndsAtUtc:HH:mm} UTC).");
        }

        return employee;
    }

    private async Task<IReadOnlyList<string>> GetPreferenceConflictWarningsAsync(
        Guid employeeId,
        string timeZoneId,
        DateTimeOffset startsAtUtc,
        DateTimeOffset endsAtUtc,
        CancellationToken cancellationToken)
    {
        var timeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
        var localStart = TimeZoneInfo.ConvertTime(startsAtUtc, timeZone);
        var localEnd = TimeZoneInfo.ConvertTime(endsAtUtc, timeZone);

        var startDate = DateOnly.FromDateTime(localStart.DateTime);
        var endDate = DateOnly.FromDateTime(localEnd.DateTime);

        var preferences = await dbContext.ShiftPreferences
            .AsNoTracking()
            .Where(x => x.EmployeeId == employeeId
                        && x.Type == PreferenceType.Unavailable
                        && x.Date >= startDate
                        && x.Date <= endDate)
            .ToListAsync(cancellationToken);

        var warnings = new List<string>();
        var shiftStartTime = TimeOnly.FromDateTime(localStart.DateTime);
        var shiftEndTime = TimeOnly.FromDateTime(localEnd.DateTime);

        foreach (var preference in preferences)
        {
            bool conflicts;
            if (startDate == endDate)
            {
                conflicts = preference.ConflictsWithShift(shiftStartTime, shiftEndTime);
            }
            else if (preference.Date == startDate)
            {
                conflicts = preference.ConflictsWithShift(shiftStartTime, TimeOnly.MaxValue);
            }
            else if (preference.Date == endDate)
            {
                conflicts = shiftEndTime > TimeOnly.MinValue && preference.ConflictsWithShift(TimeOnly.MinValue, shiftEndTime);
            }
            else
            {
                conflicts = true;
            }

            if (conflicts)
            {
                var window = preference.StartTime.HasValue && preference.EndTime.HasValue
                    ? $"{preference.StartTime.Value:HH:mm}–{preference.EndTime.Value:HH:mm}"
                    : "весь день";
                var commentPart = string.IsNullOrWhiteSpace(preference.Comment) ? string.Empty : $" ({preference.Comment})";
                warnings.Add($"У сотрудника отмечена недоступность на {preference.Date:yyyy-MM-dd} ({window}){commentPart}.");
            }
        }

        return warnings;
    }

    private static ShiftResponse MapToResponse(
        Shift shift,
        string coffeeShopName,
        string? assignedEmployeeName,
        IReadOnlyList<string>? warnings = null) =>
        new(
            shift.Id,
            shift.CoffeeShopId,
            coffeeShopName,
            shift.StartsAtUtc,
            shift.EndsAtUtc,
            shift.RequiredPosition,
            shift.AssignedEmployeeId,
            assignedEmployeeName,
            shift.Status,
            shift.Notes,
            shift.PublishedAtUtc,
            warnings is { Count: > 0 } ? warnings : null);
}
