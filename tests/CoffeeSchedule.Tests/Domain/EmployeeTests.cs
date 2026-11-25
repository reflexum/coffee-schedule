using CoffeeSchedule.Domain.Entities;
using CoffeeSchedule.Domain.Enums;
using CoffeeSchedule.Domain.Exceptions;

namespace CoffeeSchedule.Tests.Domain;

public class EmployeeTests
{
    [Fact]
    public void Create_WithPrimaryShop_NormalizesEmailAndGrantsAccess()
    {
        var shopId = Guid.NewGuid();

        var employee = Employee.Create(
            "  Анна Смирнова  ",
            "  Anna.Smirnova@Coffee.RU  ",
            " +7 999 123-45-67 ",
            Position.Barista,
            shopId);

        Assert.NotEqual(Guid.Empty, employee.Id);
        Assert.Equal("Анна Смирнова", employee.FullName);
        Assert.Equal("anna.smirnova@coffee.ru", employee.Email);
        Assert.Equal("+7 999 123-45-67", employee.Phone);
        Assert.Equal(Position.Barista, employee.Position);
        Assert.Equal(shopId, employee.PrimaryCoffeeShopId);
        Assert.True(employee.IsActive);
        Assert.True(employee.HasAccessToShop(shopId));
        Assert.Single(employee.ShopAccesses);
    }

    [Theory]
    [InlineData("", "a@b.ru", "+79991112233", Position.Barista)]
    [InlineData("Иван", "invalid-email", "+79991112233", Position.Barista)]
    [InlineData("Иван", "a @b.ru", "+79991112233", Position.Barista)]
    [InlineData("Иван", "a@b.ru", "12345", Position.Barista)]
    [InlineData("Иван", "a@b.ru", "+79991112233", (Position)999)]
    public void Create_InvalidParameters_ThrowsDomainException(
        string fullName,
        string email,
        string phone,
        Position position)
    {
        Assert.Throws<DomainException>(() => Employee.Create(
            fullName,
            email,
            phone,
            position));
    }

    [Fact]
    public void GrantAndRevokeShopAccess_PreventsDuplicatesAndProtectsPrimaryShop()
    {
        var primaryShopId = Guid.NewGuid();
        var secondaryShopId = Guid.NewGuid();
        var employee = Employee.Create("Олег Петров", "oleg@coffee.ru", "+79991112233", Position.ShiftLead, primaryShopId);

        employee.GrantShopAccess(primaryShopId);
        employee.GrantShopAccess(secondaryShopId);
        employee.GrantShopAccess(secondaryShopId);

        Assert.Equal(2, employee.ShopAccesses.Count);
        Assert.True(employee.HasAccessToShop(secondaryShopId));

        Assert.Throws<DomainException>(() => employee.RevokeShopAccess(primaryShopId));

        employee.RevokeShopAccess(secondaryShopId);
        Assert.False(employee.HasAccessToShop(secondaryShopId));
        Assert.Single(employee.ShopAccesses);
    }

    [Fact]
    public void ChangePositionAndAssignPrimaryShop_UpdatesState()
    {
        var shopId = Guid.NewGuid();
        var employee = Employee.Create("Мария Иванова", "maria@coffee.ru", "+79990001122", Position.Trainee);

        employee.ChangePosition(Position.Barista);
        employee.AssignPrimaryShop(shopId);

        Assert.Equal(Position.Barista, employee.Position);
        Assert.Equal(shopId, employee.PrimaryCoffeeShopId);
        Assert.True(employee.HasAccessToShop(shopId));
    }
}
