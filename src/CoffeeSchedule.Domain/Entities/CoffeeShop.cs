using CoffeeSchedule.Domain.Exceptions;

namespace CoffeeSchedule.Domain.Entities;

public sealed class CoffeeShop
{
    private readonly List<EmployeeShopAccess> _employeeAccesses = [];

    private CoffeeShop()
    {
        Name = string.Empty;
        Address = string.Empty;
        TimeZoneId = string.Empty;
    }

    private CoffeeShop(
        Guid id,
        string name,
        string address,
        string timeZoneId,
        TimeOnly openingTime,
        TimeOnly closingTime,
        DateTimeOffset createdAtUtc)
    {
        Id = id;
        Name = name;
        Address = address;
        TimeZoneId = timeZoneId;
        OpeningTime = openingTime;
        ClosingTime = closingTime;
        IsActive = true;
        CreatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; private set; }

    public string Name { get; private set; }

    public string Address { get; private set; }

    public string TimeZoneId { get; private set; }

    public TimeOnly OpeningTime { get; private set; }

    public TimeOnly ClosingTime { get; private set; }

    public bool IsActive { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public IReadOnlyCollection<EmployeeShopAccess> EmployeeAccesses => _employeeAccesses.AsReadOnly();

    public static CoffeeShop Create(
        string name,
        string address,
        string timeZoneId,
        TimeOnly openingTime,
        TimeOnly closingTime,
        DateTimeOffset? createdAtUtc = null)
    {
        var validated = ValidateAndNormalize(name, address, timeZoneId, openingTime, closingTime);
        return new CoffeeShop(
            Guid.NewGuid(),
            validated.Name,
            validated.Address,
            validated.TimeZoneId,
            openingTime,
            closingTime,
            createdAtUtc ?? DateTimeOffset.UtcNow);
    }

    public void UpdateDetails(
        string name,
        string address,
        string timeZoneId,
        TimeOnly openingTime,
        TimeOnly closingTime)
    {
        var validated = ValidateAndNormalize(name, address, timeZoneId, openingTime, closingTime);
        Name = validated.Name;
        Address = validated.Address;
        TimeZoneId = validated.TimeZoneId;
        OpeningTime = openingTime;
        ClosingTime = closingTime;
    }

    public void Deactivate() => IsActive = false;

    public void Activate() => IsActive = true;

    private static (string Name, string Address, string TimeZoneId) ValidateAndNormalize(
        string name,
        string address,
        string timeZoneId,
        TimeOnly openingTime,
        TimeOnly closingTime)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("Название кофейни не может быть пустым.");
        }

        var normalizedName = name.Trim();
        if (normalizedName.Length > 150)
        {
            throw new DomainException("Название кофейни не должно превышать 150 символов.");
        }

        if (string.IsNullOrWhiteSpace(address))
        {
            throw new DomainException("Адрес кофейни не может быть пустым.");
        }

        var normalizedAddress = address.Trim();
        if (normalizedAddress.Length > 300)
        {
            throw new DomainException("Адрес кофейни не должен превышать 300 символов.");
        }

        if (string.IsNullOrWhiteSpace(timeZoneId))
        {
            throw new DomainException("Часовой пояс кофейни обязателен.");
        }

        var normalizedTimeZoneId = timeZoneId.Trim();
        if (!TimeZoneInfo.TryFindSystemTimeZoneById(normalizedTimeZoneId, out _))
        {
            throw new DomainException($"Неизвестный часовой пояс: '{normalizedTimeZoneId}'.");
        }

        if (openingTime == closingTime)
        {
            throw new DomainException("Время открытия и закрытия кофейни не могут совпадать.");
        }

        return (normalizedName, normalizedAddress, normalizedTimeZoneId);
    }
}
