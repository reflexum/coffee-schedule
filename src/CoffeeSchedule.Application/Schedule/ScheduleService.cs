using CoffeeSchedule.Application.Abstractions;
using CoffeeSchedule.Domain.Enums;
using CoffeeSchedule.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace CoffeeSchedule.Application.Schedule;

public sealed class ScheduleService(IAppDbContext dbContext, TimeProvider timeProvider)
{
    public async Task<MyScheduleResponse> GetMyScheduleAsync(
        Guid employeeId,
        DateTimeOffset? from,
        DateTimeOffset? to,
        CancellationToken cancellationToken = default)
    {
        var employee = await dbContext.Employees
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == employeeId, cancellationToken)
            ?? throw new NotFoundException($"Сотрудник '{employeeId}' не найден.");

        if (!employee.IsActive)
        {
            throw new ForbiddenException("Учётная запись сотрудника деактивирована.");
        }

        var fromUtc = (from ?? new DateTimeOffset(timeProvider.GetUtcNow().UtcDateTime.Date, TimeSpan.Zero))
            .ToUniversalTime();
        var toUtc = (to ?? fromUtc.AddDays(14)).ToUniversalTime();

        if (fromUtc >= toUtc)
        {
            throw new DomainException("Дата окончания периода должна быть позже даты начала.");
        }

        if (toUtc - fromUtc > TimeSpan.FromDays(90))
        {
            throw new DomainException("Максимальный период запроса расписания — 90 дней.");
        }

        var myShifts = await dbContext.Shifts
            .AsNoTracking()
            .Include(x => x.CoffeeShop)
            .Where(x => x.AssignedEmployeeId == employeeId
                        && x.Status == ShiftStatus.Published
                        && x.StartsAtUtc < toUtc
                        && fromUtc < x.EndsAtUtc)
            .OrderBy(x => x.StartsAtUtc)
            .ToListAsync(cancellationToken);

        if (myShifts.Count == 0)
        {
            return new MyScheduleResponse(employeeId, fromUtc, toUtc, 0, []);
        }

        var shopIds = myShifts.Select(x => x.CoffeeShopId).Distinct().ToList();
        var minStart = myShifts.Min(x => x.StartsAtUtc);
        var maxEnd = myShifts.Max(x => x.EndsAtUtc);

        var potentialCoworkerShifts = await dbContext.Shifts
            .AsNoTracking()
            .Include(x => x.AssignedEmployee)
            .Where(x => shopIds.Contains(x.CoffeeShopId)
                        && x.Status == ShiftStatus.Published
                        && x.AssignedEmployeeId != null
                        && x.AssignedEmployeeId != employeeId
                        && x.StartsAtUtc < maxEnd
                        && minStart < x.EndsAtUtc)
            .OrderBy(x => x.StartsAtUtc)
            .ToListAsync(cancellationToken);

        var shifts = myShifts.Select(shift =>
        {
            var coworkers = potentialCoworkerShifts
                .Where(other => other.CoffeeShopId == shift.CoffeeShopId
                                && other.StartsAtUtc < shift.EndsAtUtc
                                && shift.StartsAtUtc < other.EndsAtUtc
                                && other.AssignedEmployee is not null)
                .OrderBy(other => other.StartsAtUtc)
                .ThenBy(other => other.AssignedEmployee!.FullName)
                .Select(other => new ShiftCoworkerDto(
                    other.AssignedEmployee!.Id,
                    other.AssignedEmployee.FullName,
                    other.AssignedEmployee.Position,
                    other.AssignedEmployee.Phone,
                    other.StartsAtUtc,
                    other.EndsAtUtc))
                .ToList();

            return new MyShiftDto(
                shift.Id,
                shift.CoffeeShopId,
                shift.CoffeeShop.Name,
                shift.CoffeeShop.Address,
                shift.CoffeeShop.TimeZoneId,
                shift.StartsAtUtc,
                shift.EndsAtUtc,
                shift.RequiredPosition,
                shift.Notes,
                coworkers);
        }).ToList();

        var totalHours = Math.Round(
            shifts.Sum(s => (s.EndsAtUtc - s.StartsAtUtc).TotalHours),
            2);

        return new MyScheduleResponse(employeeId, fromUtc, toUtc, totalHours, shifts);
    }
}
