using CoffeeSchedule.Domain.Enums;

namespace CoffeeSchedule.Application.Schedule;

public sealed record ShiftCoworkerDto(
    Guid EmployeeId,
    string FullName,
    Position Position,
    string Phone,
    DateTimeOffset ShiftStartsAtUtc,
    DateTimeOffset ShiftEndsAtUtc);

public sealed record MyShiftDto(
    Guid ShiftId,
    Guid CoffeeShopId,
    string CoffeeShopName,
    string CoffeeShopAddress,
    string TimeZoneId,
    DateTimeOffset StartsAtUtc,
    DateTimeOffset EndsAtUtc,
    Position Role,
    string? Notes,
    IReadOnlyList<ShiftCoworkerDto> Coworkers);

public sealed record MyScheduleResponse(
    Guid EmployeeId,
    DateTimeOffset FromUtc,
    DateTimeOffset ToUtc,
    double TotalScheduledHours,
    IReadOnlyList<MyShiftDto> Shifts);
