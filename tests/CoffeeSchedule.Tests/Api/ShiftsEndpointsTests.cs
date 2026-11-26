using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using CoffeeSchedule.Application.CoffeeShops;
using CoffeeSchedule.Application.Employees;
using CoffeeSchedule.Application.Shifts;
using CoffeeSchedule.Domain.Enums;

namespace CoffeeSchedule.Tests.Api;

public class ShiftsEndpointsTests : IAsyncLifetime
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly ApiTestWebApplicationFactory _factory = new();
    private HttpClient _client = null!;

    public async Task InitializeAsync()
    {
        await _factory.InitializeAsync();
        _client = _factory.CreateClient();
    }

    public async Task DisposeAsync()
    {
        await _factory.DisposeAsync();
    }

    [Fact]
    public async Task CreateAndPublishShift_ValidWorkflow_Succeeds()
    {
        var shop = await CreateShopAsync("Покровка", "ул. Покровка, 10");
        var barista = await CreateEmployeeAsync("Анна Смирнова", "anna@coffee.ru", "+79991112233", Position.Barista, shop.Id);

        var start = new DateTimeOffset(2026, 12, 10, 9, 0, 0, TimeSpan.FromHours(3));
        var end = new DateTimeOffset(2026, 12, 10, 17, 0, 0, TimeSpan.FromHours(3));

        var createResponse = await _client.PostAsJsonAsync(
            "/api/v1/shifts",
            new CreateShiftRequest(shop.Id, start, end, Position.Barista, barista.Id, "Утренняя смена"),
            JsonOptions);

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = await createResponse.Content.ReadFromJsonAsync<ShiftResponse>(JsonOptions);
        Assert.NotNull(created);
        Assert.Equal(ShiftStatus.Draft, created.Status);
        Assert.Equal(barista.Id, created.AssignedEmployeeId);

        var publishResponse = await _client.PostAsync($"/api/v1/shifts/{created.Id}/publish", null);
        Assert.Equal(HttpStatusCode.OK, publishResponse.StatusCode);
        var published = await publishResponse.Content.ReadFromJsonAsync<ShiftResponse>(JsonOptions);
        Assert.NotNull(published);
        Assert.Equal(ShiftStatus.Published, published.Status);
        Assert.NotNull(published.PublishedAtUtc);
    }

    [Fact]
    public async Task AssignOverlappingShiftAcrossDifferentShops_Returns409Conflict()
    {
        var shopA = await CreateShopAsync("Кофейня А", "ул. Ленина, 1");
        var shopB = await CreateShopAsync("Кофейня Б", "ул. Мира, 2");
        var barista = await CreateEmployeeAsync("Иван Иванов", "ivan@coffee.ru", "+79992223344", Position.Barista, shopA.Id);

        var accessResp = await _client.PostAsync($"/api/v1/employees/{barista.Id}/shops/{shopB.Id}", null);
        Assert.Equal(HttpStatusCode.NoContent, accessResp.StatusCode);

        var day = new DateTimeOffset(2026, 12, 11, 9, 0, 0, TimeSpan.Zero);
        var firstShiftResp = await _client.PostAsJsonAsync(
            "/api/v1/shifts",
            new CreateShiftRequest(shopA.Id, day, day.AddHours(8), Position.Barista, barista.Id, null),
            JsonOptions);
        Assert.Equal(HttpStatusCode.Created, firstShiftResp.StatusCode);

        var overlappingResp = await _client.PostAsJsonAsync(
            "/api/v1/shifts",
            new CreateShiftRequest(shopB.Id, day.AddHours(6), day.AddHours(12), Position.Barista, barista.Id, null),
            JsonOptions);
        Assert.Equal(HttpStatusCode.Conflict, overlappingResp.StatusCode);

        var adjacentResp = await _client.PostAsJsonAsync(
            "/api/v1/shifts",
            new CreateShiftRequest(shopB.Id, day.AddHours(8), day.AddHours(13), Position.Barista, barista.Id, null),
            JsonOptions);
        Assert.Equal(HttpStatusCode.Created, adjacentResp.StatusCode);
    }

    [Fact]
    public async Task CancelledShift_DoesNotBlockNewShiftInSameWindow()
    {
        var shop = await CreateShopAsync("Тверская", "Тверская, 5");
        var barista = await CreateEmployeeAsync("Ольга Козлова", "olga@coffee.ru", "+79993334455", Position.Barista, shop.Id);

        var start = new DateTimeOffset(2026, 12, 12, 8, 0, 0, TimeSpan.Zero);
        var createResp = await _client.PostAsJsonAsync(
            "/api/v1/shifts",
            new CreateShiftRequest(shop.Id, start, start.AddHours(8), Position.Barista, barista.Id, null),
            JsonOptions);
        var shift = await createResp.Content.ReadFromJsonAsync<ShiftResponse>(JsonOptions);
        Assert.NotNull(shift);

        var cancelResp = await _client.PostAsync($"/api/v1/shifts/{shift.Id}/cancel", null);
        Assert.Equal(HttpStatusCode.OK, cancelResp.StatusCode);

        var replacementResp = await _client.PostAsJsonAsync(
            "/api/v1/shifts",
            new CreateShiftRequest(shop.Id, start, start.AddHours(8), Position.Barista, barista.Id, null),
            JsonOptions);
        Assert.Equal(HttpStatusCode.Created, replacementResp.StatusCode);
    }

    [Fact]
    public async Task TraineeAssignedToBaristaShift_Returns422UnprocessableEntity()
    {
        var shop = await CreateShopAsync("Арбат", "Арбат, 15");
        var trainee = await CreateEmployeeAsync("Егор Новиков", "egor@coffee.ru", "+79994445566", Position.Trainee, shop.Id);

        var start = new DateTimeOffset(2026, 12, 13, 9, 0, 0, TimeSpan.Zero);
        var response = await _client.PostAsJsonAsync(
            "/api/v1/shifts",
            new CreateShiftRequest(shop.Id, start, start.AddHours(6), Position.Barista, trainee.Id, null),
            JsonOptions);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task PublishShopSchedule_PublishesAssignedDraftsAndBlocksWhenUnassignedExist()
    {
        var shop = await CreateShopAsync("Садовая", "Садовая, 3");
        var barista = await CreateEmployeeAsync("Дарья Попова", "daria@coffee.ru", "+79995556677", Position.Barista, shop.Id);

        var start = new DateTimeOffset(2026, 12, 14, 9, 0, 0, TimeSpan.Zero);
        var assignedResp = await _client.PostAsJsonAsync(
            "/api/v1/shifts",
            new CreateShiftRequest(shop.Id, start, start.AddHours(8), Position.Barista, barista.Id, null),
            JsonOptions);
        Assert.Equal(HttpStatusCode.Created, assignedResp.StatusCode);

        var unassignedResp = await _client.PostAsJsonAsync(
            "/api/v1/shifts",
            new CreateShiftRequest(shop.Id, start.AddDays(1), start.AddDays(1).AddHours(8), Position.Barista, null, null),
            JsonOptions);
        var unassigned = await unassignedResp.Content.ReadFromJsonAsync<ShiftResponse>(JsonOptions);
        Assert.NotNull(unassigned);

        var failedPublish = await _client.PostAsJsonAsync(
            $"/api/v1/shops/{shop.Id}/schedule/publish",
            new PublishScheduleRequest(start.AddHours(-1), start.AddDays(3)),
            JsonOptions);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, failedPublish.StatusCode);

        var assignResp = await _client.PutAsJsonAsync(
            $"/api/v1/shifts/{unassigned.Id}",
            new UpdateShiftRequest(start.AddDays(1), start.AddDays(1).AddHours(8), Position.Barista, barista.Id, "Закрыто"),
            JsonOptions);
        Assert.Equal(HttpStatusCode.OK, assignResp.StatusCode);

        var okPublish = await _client.PostAsJsonAsync(
            $"/api/v1/shops/{shop.Id}/schedule/publish",
            new PublishScheduleRequest(start.AddHours(-1), start.AddDays(3)),
            JsonOptions);
        Assert.Equal(HttpStatusCode.OK, okPublish.StatusCode);

        var publishResult = await okPublish.Content.ReadFromJsonAsync<PublishScheduleResponse>(JsonOptions);
        Assert.NotNull(publishResult);
        Assert.Equal(2, publishResult.PublishedCount);
    }

    private async Task<CoffeeShopResponse> CreateShopAsync(string name, string address)
    {
        var resp = await _client.PostAsJsonAsync(
            "/api/v1/shops",
            new CreateCoffeeShopRequest(name, address, "Europe/Moscow", new TimeOnly(8, 0), new TimeOnly(22, 0)),
            JsonOptions);
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<CoffeeShopResponse>(JsonOptions))!;
    }

    private async Task<EmployeeResponse> CreateEmployeeAsync(
        string fullName,
        string email,
        string phone,
        Position position,
        Guid primaryShopId)
    {
        var resp = await _client.PostAsJsonAsync(
            "/api/v1/employees",
            new CreateEmployeeRequest(fullName, email, phone, position, primaryShopId),
            JsonOptions);
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<EmployeeResponse>(JsonOptions))!;
    }
}
