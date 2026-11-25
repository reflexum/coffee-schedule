using CoffeeSchedule.Domain.Entities;
using CoffeeSchedule.Domain.Enums;
using CoffeeSchedule.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace CoffeeSchedule.Tests.Infrastructure;

public class AppDbContextTests : IAsyncLifetime
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");
    private DbContextOptions<AppDbContext> _options = null!;

    public async Task InitializeAsync()
    {
        await _connection.OpenAsync();
        _options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;

        await using var context = new AppDbContext(_options);
        await context.Database.EnsureCreatedAsync();
    }

    public async Task DisposeAsync()
    {
        await _connection.DisposeAsync();
    }

    [Fact]
    public async Task CanPersistAndLoadCoffeeShopsEmployeesAndAccesses()
    {
        var primaryShop = CoffeeShop.Create(
            "Кофейня Центр",
            "Тверская ул., 1",
            "Europe/Moscow",
            new TimeOnly(8, 0),
            new TimeOnly(22, 0));

        var secondaryShop = CoffeeShop.Create(
            "Кофейня Арбат",
            "Арбат, 24",
            "Europe/Moscow",
            new TimeOnly(9, 0),
            new TimeOnly(23, 0));

        var employee = Employee.Create(
            "Дмитрий Соколов",
            "dmitry@coffee.ru",
            "+79995554433",
            Position.Barista,
            primaryShop.Id);
        employee.GrantShopAccess(secondaryShop.Id);

        await using (var writeContext = new AppDbContext(_options))
        {
            writeContext.CoffeeShops.AddRange(primaryShop, secondaryShop);
            writeContext.Employees.Add(employee);
            await writeContext.SaveChangesAsync();
        }

        await using (var readContext = new AppDbContext(_options))
        {
            var loaded = await readContext.Employees
                .Include(e => e.PrimaryCoffeeShop)
                .Include(e => e.ShopAccesses)
                .SingleAsync(e => e.Id == employee.Id);

            Assert.Equal("dmitry@coffee.ru", loaded.Email);
            Assert.Equal(Position.Barista, loaded.Position);
            Assert.NotNull(loaded.PrimaryCoffeeShop);
            Assert.Equal("Кофейня Центр", loaded.PrimaryCoffeeShop.Name);
            Assert.Equal(2, loaded.ShopAccesses.Count);
        }
    }

    [Fact]
    public async Task DuplicateEmailOrPhone_ThrowsDbUpdateException()
    {
        var first = Employee.Create("Анна", "same@coffee.ru", "+79991110001", Position.Barista);
        var duplicateEmail = Employee.Create("Ольга", "same@coffee.ru", "+79991110002", Position.Trainee);
        var duplicatePhone = Employee.Create("Игорь", "other@coffee.ru", "+79991110001", Position.Manager);

        await using var context = new AppDbContext(_options);
        context.Employees.Add(first);
        await context.SaveChangesAsync();

        context.Employees.Add(duplicateEmail);
        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());

        context.Entry(duplicateEmail).State = EntityState.Detached;
        context.Employees.Add(duplicatePhone);
        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }
}
