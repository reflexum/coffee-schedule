using CoffeeSchedule.Domain.Enums;
using CoffeeSchedule.Domain.Exceptions;

namespace CoffeeSchedule.Domain.Entities;

public sealed class Shift
{
    private Shift()
    {
    }

    private Shift(
        Guid id,
        Guid coffeeShopId,
        DateTimeOffset startsAtUtc,
        DateTimeOffset endsAtUtc,
        Position requiredPosition,
        Guid? assignedEmployeeId,
        string? notes,
        DateTimeOffset createdAtUtc)
    {
        Id = id;
        CoffeeShopId = coffeeShopId;
        StartsAtUtc = startsAtUtc;
        EndsAtUtc = endsAtUtc;
        RequiredPosition = requiredPosition;
        AssignedEmployeeId = assignedEmployeeId;
        Status = ShiftStatus.Draft;
        Notes = notes;
        CreatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; private set; }

    public Guid CoffeeShopId { get; private set; }

    public CoffeeShop CoffeeShop { get; private set; } = null!;

    public DateTimeOffset StartsAtUtc { get; private set; }

    public DateTimeOffset EndsAtUtc { get; private set; }

    public Position RequiredPosition { get; private set; }

    public Guid? AssignedEmployeeId { get; private set; }

    public Employee? AssignedEmployee { get; private set; }

    public ShiftStatus Status { get; private set; }

    public string? Notes { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset? UpdatedAtUtc { get; private set; }

    public DateTimeOffset? PublishedAtUtc { get; private set; }

    public static Shift Create(
        Guid coffeeShopId,
        DateTimeOffset startsAt,
        DateTimeOffset endsAt,
        Position requiredPosition,
        Guid? assignedEmployeeId = null,
        string? notes = null,
        DateTimeOffset? createdAtUtc = null)
    {
        if (coffeeShopId == Guid.Empty)
        {
            throw new DomainException("Идентификатор кофейни не может быть пустым.");
        }

        var (startsAtUtc, endsAtUtc, normalizedNotes) = ValidateScheduleParameters(
            startsAt,
            endsAt,
            requiredPosition,
            assignedEmployeeId,
            notes);

        return new Shift(
            Guid.NewGuid(),
            coffeeShopId,
            startsAtUtc,
            endsAtUtc,
            requiredPosition,
            assignedEmployeeId,
            normalizedNotes,
            createdAtUtc ?? DateTimeOffset.UtcNow);
    }

    public void UpdateSchedule(
        DateTimeOffset startsAt,
        DateTimeOffset endsAt,
        Position requiredPosition,
        Guid? assignedEmployeeId,
        string? notes,
        DateTimeOffset? updatedAtUtc = null)
    {
        if (Status == ShiftStatus.Cancelled)
        {
            throw new DomainException("Нельзя редактировать отменённую смену.");
        }

        if (Status == ShiftStatus.Published && assignedEmployeeId is null)
        {
            throw new DomainException("Нельзя снять сотрудника с опубликованной смены.");
        }

        var (startsAtUtc, endsAtUtc, normalizedNotes) = ValidateScheduleParameters(
            startsAt,
            endsAt,
            requiredPosition,
            assignedEmployeeId,
            notes);

        StartsAtUtc = startsAtUtc;
        EndsAtUtc = endsAtUtc;
        RequiredPosition = requiredPosition;
        AssignedEmployeeId = assignedEmployeeId;
        Notes = normalizedNotes;
        UpdatedAtUtc = updatedAtUtc ?? DateTimeOffset.UtcNow;
    }

    public void Publish(DateTimeOffset? publishedAtUtc = null)
    {
        if (Status == ShiftStatus.Cancelled)
        {
            throw new DomainException("Нельзя опубликовать отменённую смену.");
        }

        if (AssignedEmployeeId is null)
        {
            throw new DomainException("Нельзя опубликовать смену без назначенного сотрудника.");
        }

        if (Status == ShiftStatus.Published)
        {
            return;
        }

        var now = publishedAtUtc ?? DateTimeOffset.UtcNow;
        Status = ShiftStatus.Published;
        PublishedAtUtc = now;
        UpdatedAtUtc = now;
    }

    public void Cancel(DateTimeOffset? updatedAtUtc = null)
    {
        if (Status == ShiftStatus.Cancelled)
        {
            throw new DomainException("Смена уже отменена.");
        }

        Status = ShiftStatus.Cancelled;
        UpdatedAtUtc = updatedAtUtc ?? DateTimeOffset.UtcNow;
    }

    public bool OverlapsWith(DateTimeOffset otherStart, DateTimeOffset otherEnd)
    {
        var otherStartUtc = otherStart.ToUniversalTime();
        var otherEndUtc = otherEnd.ToUniversalTime();
        return StartsAtUtc < otherEndUtc && otherStartUtc < EndsAtUtc;
    }

    public static bool IsPositionCompatible(Position employeePosition, Position requiredPosition) =>
        (int)employeePosition <= (int)requiredPosition;

    private static (DateTimeOffset StartsAtUtc, DateTimeOffset EndsAtUtc, string? Notes) ValidateScheduleParameters(
        DateTimeOffset startsAt,
        DateTimeOffset endsAt,
        Position requiredPosition,
        Guid? assignedEmployeeId,
        string? notes)
    {
        var startsAtUtc = startsAt.ToUniversalTime();
        var endsAtUtc = endsAt.ToUniversalTime();

        if (startsAtUtc >= endsAtUtc)
        {
            throw new DomainException("Время окончания смены должно быть позже времени начала.");
        }

        if (endsAtUtc - startsAtUtc > TimeSpan.FromHours(24))
        {
            throw new DomainException("Длительность смены не может превышать 24 часа.");
        }

        if (!Enum.IsDefined(requiredPosition))
        {
            throw new DomainException($"Недопустимая требуемая должность: '{requiredPosition}'.");
        }

        if (assignedEmployeeId == Guid.Empty)
        {
            throw new DomainException("Идентификатор сотрудника не может быть пустым.");
        }

        string? normalizedNotes = null;
        if (!string.IsNullOrWhiteSpace(notes))
        {
            normalizedNotes = notes.Trim();
            if (normalizedNotes.Length > 500)
            {
                throw new DomainException("Комментарий к смене не должен превышать 500 символов.");
            }
        }

        return (startsAtUtc, endsAtUtc, normalizedNotes);
    }
}
