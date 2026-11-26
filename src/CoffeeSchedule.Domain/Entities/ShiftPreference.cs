using CoffeeSchedule.Domain.Enums;
using CoffeeSchedule.Domain.Exceptions;

namespace CoffeeSchedule.Domain.Entities;

public sealed class ShiftPreference
{
    private ShiftPreference()
    {
    }

    private ShiftPreference(
        Guid id,
        Guid employeeId,
        DateOnly date,
        TimeOnly? startTime,
        TimeOnly? endTime,
        PreferenceType type,
        string? comment,
        DateTimeOffset createdAtUtc)
    {
        Id = id;
        EmployeeId = employeeId;
        Date = date;
        StartTime = startTime;
        EndTime = endTime;
        Type = type;
        Comment = comment;
        CreatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; private set; }

    public Guid EmployeeId { get; private set; }

    public Employee Employee { get; private set; } = null!;

    public DateOnly Date { get; private set; }

    public TimeOnly? StartTime { get; private set; }

    public TimeOnly? EndTime { get; private set; }

    public PreferenceType Type { get; private set; }

    public string? Comment { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset? UpdatedAtUtc { get; private set; }

    public static ShiftPreference Create(
        Guid employeeId,
        DateOnly date,
        TimeOnly? startTime,
        TimeOnly? endTime,
        PreferenceType type,
        string? comment,
        DateOnly currentDate,
        DateTimeOffset? createdAtUtc = null)
    {
        if (employeeId == Guid.Empty)
        {
            throw new DomainException("Идентификатор сотрудника не может быть пустым.");
        }

        if (date < currentDate)
        {
            throw new DomainException("Нельзя указать пожелание на прошедшую дату.");
        }

        var normalizedComment = ValidateParameters(startTime, endTime, type, comment);

        return new ShiftPreference(
            Guid.NewGuid(),
            employeeId,
            date,
            startTime,
            endTime,
            type,
            normalizedComment,
            createdAtUtc ?? DateTimeOffset.UtcNow);
    }

    public void Update(
        TimeOnly? startTime,
        TimeOnly? endTime,
        PreferenceType type,
        string? comment,
        DateTimeOffset? updatedAtUtc = null)
    {
        var normalizedComment = ValidateParameters(startTime, endTime, type, comment);

        StartTime = startTime;
        EndTime = endTime;
        Type = type;
        Comment = normalizedComment;
        UpdatedAtUtc = updatedAtUtc ?? DateTimeOffset.UtcNow;
    }

    public bool OverlapsTimeWindow(TimeOnly? otherStart, TimeOnly? otherEnd)
    {
        if (StartTime is null || EndTime is null || otherStart is null || otherEnd is null)
        {
            return true;
        }

        return StartTime.Value < otherEnd.Value && otherStart.Value < EndTime.Value;
    }

    public bool ConflictsWithShift(TimeOnly shiftStart, TimeOnly shiftEnd) =>
        Type == PreferenceType.Unavailable && OverlapsTimeWindow(shiftStart, shiftEnd);

    private static string? ValidateParameters(
        TimeOnly? startTime,
        TimeOnly? endTime,
        PreferenceType type,
        string? comment)
    {
        if (startTime.HasValue != endTime.HasValue)
        {
            throw new DomainException("Необходимо указать как время начала, так и время окончания, либо оставить оба поля пустыми.");
        }

        if (startTime.HasValue && endTime.HasValue && startTime.Value >= endTime.Value)
        {
            throw new DomainException("Время окончания пожелания должно быть позже времени начала.");
        }

        if (!Enum.IsDefined(type))
        {
            throw new DomainException($"Недопустимый тип пожелания: '{type}'.");
        }

        if (string.IsNullOrWhiteSpace(comment))
        {
            return null;
        }

        var normalized = comment.Trim();
        if (normalized.Length > 300)
        {
            throw new DomainException("Комментарий к пожеланию не должен превышать 300 символов.");
        }

        return normalized;
    }
}
