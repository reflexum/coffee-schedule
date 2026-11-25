using CoffeeSchedule.Domain.Entities;
using CoffeeSchedule.Domain.Exceptions;

namespace CoffeeSchedule.Tests.Domain;

public class CoffeeShopTests
{
    [Fact]
    public void Create_ValidData_CreatesActiveShopWithTrimmedFields()
    {
        var shop = CoffeeShop.Create(
            "  Кофейня на Покровке  ",
            "  ул. Покровка, д. 10  ",
            "Europe/Moscow",
            new TimeOnly(7, 30),
            new TimeOnly(22, 0));

        Assert.NotEqual(Guid.Empty, shop.Id);
        Assert.Equal("Кофейня на Покровке", shop.Name);
        Assert.Equal("ул. Покровка, д. 10", shop.Address);
        Assert.Equal("Europe/Moscow", shop.TimeZoneId);
        Assert.Equal(new TimeOnly(7, 30), shop.OpeningTime);
        Assert.Equal(new TimeOnly(22, 0), shop.ClosingTime);
        Assert.True(shop.IsActive);
    }

    [Theory]
    [InlineData("", "ул. Покровка, 10", "Europe/Moscow", 8, 22)]
    [InlineData("Покровка", "   ", "Europe/Moscow", 8, 22)]
    [InlineData("Покровка", "ул. Покровка, 10", "", 8, 22)]
    [InlineData("Покровка", "ул. Покровка, 10", "Mars/Olympus", 8, 22)]
    [InlineData("Покровка", "ул. Покровка, 10", "Europe/Moscow", 9, 9)]
    public void Create_InvalidParameters_ThrowsDomainException(
        string name,
        string address,
        string timeZoneId,
        int openHour,
        int closeHour)
    {
        Assert.Throws<DomainException>(() => CoffeeShop.Create(
            name,
            address,
            timeZoneId,
            new TimeOnly(openHour, 0),
            new TimeOnly(closeHour, 0)));
    }

    [Fact]
    public void UpdateDetails_ValidData_UpdatesProperties()
    {
        var shop = CoffeeShop.Create(
            "Покровка",
            "ул. Покровка, 10",
            "Europe/Moscow",
            new TimeOnly(8, 0),
            new TimeOnly(22, 0));

        shop.UpdateDetails(
            "Покровка Флагман",
            "ул. Покровка, 12",
            "UTC",
            new TimeOnly(7, 0),
            new TimeOnly(23, 0));

        Assert.Equal("Покровка Флагман", shop.Name);
        Assert.Equal("ул. Покровка, 12", shop.Address);
        Assert.Equal("UTC", shop.TimeZoneId);
        Assert.Equal(new TimeOnly(7, 0), shop.OpeningTime);
        Assert.Equal(new TimeOnly(23, 0), shop.ClosingTime);
    }

    [Fact]
    public void DeactivateAndActivate_TogglesIsActive()
    {
        var shop = CoffeeShop.Create(
            "Покровка",
            "ул. Покровка, 10",
            "Europe/Moscow",
            new TimeOnly(8, 0),
            new TimeOnly(22, 0));

        shop.Deactivate();
        Assert.False(shop.IsActive);

        shop.Activate();
        Assert.True(shop.IsActive);
    }
}
