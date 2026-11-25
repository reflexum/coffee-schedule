namespace CoffeeSchedule.Domain.Entities;

public sealed class EmployeeShopAccess
{
    private EmployeeShopAccess()
    {
    }

    internal EmployeeShopAccess(Guid employeeId, Guid coffeeShopId, DateTimeOffset assignedAtUtc)
    {
        EmployeeId = employeeId;
        CoffeeShopId = coffeeShopId;
        AssignedAtUtc = assignedAtUtc;
    }

    public Guid EmployeeId { get; private set; }

    public Employee Employee { get; private set; } = null!;

    public Guid CoffeeShopId { get; private set; }

    public CoffeeShop CoffeeShop { get; private set; } = null!;

    public DateTimeOffset AssignedAtUtc { get; private set; }
}
