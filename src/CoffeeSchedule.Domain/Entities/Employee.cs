using CoffeeSchedule.Domain.Enums;
using CoffeeSchedule.Domain.Exceptions;

namespace CoffeeSchedule.Domain.Entities;

public sealed class Employee
{
    private readonly List<EmployeeShopAccess> _shopAccesses = [];

    private Employee()
    {
        FullName = string.Empty;
        Email = string.Empty;
        Phone = string.Empty;
    }

    private Employee(
        Guid id,
        string fullName,
        string email,
        string phone,
        Position position,
        Guid? primaryCoffeeShopId,
        DateTimeOffset createdAtUtc)
    {
        Id = id;
        FullName = fullName;
        Email = email;
        Phone = phone;
        Position = position;
        PrimaryCoffeeShopId = primaryCoffeeShopId;
        IsActive = true;
        CreatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; private set; }

    public string FullName { get; private set; }

    public string Email { get; private set; }

    public string Phone { get; private set; }

    public Position Position { get; private set; }

    public Guid? PrimaryCoffeeShopId { get; private set; }

    public CoffeeShop? PrimaryCoffeeShop { get; private set; }

    public bool IsActive { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public IReadOnlyCollection<EmployeeShopAccess> ShopAccesses => _shopAccesses.AsReadOnly();

    public static Employee Create(
        string fullName,
        string email,
        string phone,
        Position position,
        Guid? primaryCoffeeShopId = null,
        DateTimeOffset? createdAtUtc = null)
    {
        var normalizedFullName = ValidateFullName(fullName);
        var normalizedEmail = ValidateAndNormalizeEmail(email);
        var normalizedPhone = ValidateAndNormalizePhone(phone);

        if (!Enum.IsDefined(position))
        {
            throw new DomainException($"Недопустимая должность сотрудника: '{position}'.");
        }

        if (primaryCoffeeShopId == Guid.Empty)
        {
            throw new DomainException("Идентификатор основной кофейни не может быть пустым.");
        }

        var timestamp = createdAtUtc ?? DateTimeOffset.UtcNow;
        var employee = new Employee(
            Guid.NewGuid(),
            normalizedFullName,
            normalizedEmail,
            normalizedPhone,
            position,
            primaryCoffeeShopId,
            timestamp);

        if (primaryCoffeeShopId.HasValue)
        {
            employee.GrantShopAccess(primaryCoffeeShopId.Value, timestamp);
        }

        return employee;
    }

    public void UpdateProfile(string fullName, string phone)
    {
        FullName = ValidateFullName(fullName);
        Phone = ValidateAndNormalizePhone(phone);
    }

    public void ChangePosition(Position newPosition)
    {
        if (!Enum.IsDefined(newPosition))
        {
            throw new DomainException($"Недопустимая должность сотрудника: '{newPosition}'.");
        }

        Position = newPosition;
    }

    public void AssignPrimaryShop(Guid coffeeShopId, DateTimeOffset? assignedAtUtc = null)
    {
        if (coffeeShopId == Guid.Empty)
        {
            throw new DomainException("Идентификатор кофейни не может быть пустым.");
        }

        PrimaryCoffeeShopId = coffeeShopId;
        GrantShopAccess(coffeeShopId, assignedAtUtc);
    }

    public void GrantShopAccess(Guid coffeeShopId, DateTimeOffset? assignedAtUtc = null)
    {
        if (coffeeShopId == Guid.Empty)
        {
            throw new DomainException("Идентификатор кофейни не может быть пустым.");
        }

        if (_shopAccesses.Any(x => x.CoffeeShopId == coffeeShopId))
        {
            return;
        }

        _shopAccesses.Add(new EmployeeShopAccess(Id, coffeeShopId, assignedAtUtc ?? DateTimeOffset.UtcNow));
    }

    public void RevokeShopAccess(Guid coffeeShopId)
    {
        if (PrimaryCoffeeShopId == coffeeShopId)
        {
            throw new DomainException("Нельзя отозвать доступ к основной кофейне сотрудника.");
        }

        var access = _shopAccesses.FirstOrDefault(x => x.CoffeeShopId == coffeeShopId);
        if (access is not null)
        {
            _shopAccesses.Remove(access);
        }
    }

    public bool HasAccessToShop(Guid coffeeShopId) =>
        _shopAccesses.Any(x => x.CoffeeShopId == coffeeShopId);

    public void Deactivate() => IsActive = false;

    public void Activate() => IsActive = true;

    private static string ValidateFullName(string fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName))
        {
            throw new DomainException("ФИО сотрудника не может быть пустым.");
        }

        var normalized = fullName.Trim();
        if (normalized.Length > 200)
        {
            throw new DomainException("ФИО сотрудника не должно превышать 200 символов.");
        }

        return normalized;
    }

    private static string ValidateAndNormalizeEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new DomainException("Email сотрудника не может быть пустым.");
        }

        var normalized = email.Trim().ToLowerInvariant();
        if (normalized.Length > 256 || normalized.Contains(' '))
        {
            throw new DomainException("Некорректный формат email.");
        }

        var atIndex = normalized.IndexOf('@');
        if (atIndex <= 0 || atIndex == normalized.Length - 1)
        {
            throw new DomainException("Некорректный формат email.");
        }

        var dotIndex = normalized.IndexOf('.', atIndex + 1);
        if (dotIndex <= atIndex + 1 || dotIndex == normalized.Length - 1)
        {
            throw new DomainException("Некорректный формат email.");
        }

        return normalized;
    }

    private static string ValidateAndNormalizePhone(string phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
        {
            throw new DomainException("Телефон сотрудника не может быть пустым.");
        }

        var normalized = phone.Trim();
        if (normalized.Length > 32)
        {
            throw new DomainException("Телефон сотрудника не должен превышать 32 символа.");
        }

        var digitsCount = normalized.Count(char.IsDigit);
        if (digitsCount < 10)
        {
            throw new DomainException("Телефон должен содержать минимум 10 цифр.");
        }

        return normalized;
    }
}
