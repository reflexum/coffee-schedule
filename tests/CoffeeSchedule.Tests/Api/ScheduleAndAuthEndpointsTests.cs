using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using CoffeeSchedule.Application.Auth;
using CoffeeSchedule.Application.CoffeeShops;
using CoffeeSchedule.Application.Employees;
using CoffeeSchedule.Application.Preferences;
using CoffeeSchedule.Application.Schedule;
using CoffeeSchedule.Application.Shifts;
using CoffeeSchedule.Domain.Enums;

namespace CoffeeSchedule.Tests.Api;

public class ScheduleAndAuthEndpointsTests : IAsyncLifetime
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly ApiTestWebApplicationFactory _factory = new();
    private HttpClient _managerClient = null!;

    public async Task InitializeAsync()
    {
        await _factory.InitializeAsync();
        _managerClient = _factory.CreateClient();
        await _factory.AuthenticateAsManagerAsync(_managerClient);
    }

    public async Task DisposeAsync()
    {
        await _factory.DisposeAsync();
    }

    [Fact]
    public async Task UnauthenticatedRequestToMySchedule_Returns401Unauthorized()
    {
        var anonymousClient = _factory.CreateClient();

        var response = await anonymousClient.GetAsync("/api/v1/schedule/my");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task SecondBootstrapAttempt_Returns409Conflict()
    {
        var anonymousClient = _factory.CreateClient();

        var response = await anonymousClient.PostAsJsonAsync(
            "/api/v1/auth/bootstrap",
            new BootstrapManagerRequest("Другой Управляющий", "other@coffee.ru", "+79990009999", "Password123!"),
            JsonOptions);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task InvalidLogin_Returns401UnauthorizedProblemDetails()
    {
        var anonymousClient = _factory.CreateClient();

        var response = await anonymousClient.PostAsJsonAsync(
            "/api/v1/auth/login",
            new LoginRequest("manager@coffee.ru", "WrongPassword!"),
            JsonOptions);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task GetMySchedule_ReturnsOnlyPublishedShiftsAndOverlappingCoworkersInSameShop()
    {
        var shop1 = await CreateShopAsync("Покровка", "ул. Покровка, 10");
        var shop2 = await CreateShopAsync("Арбат", "ул. Арбат, 20");

        var baristaA = await CreateEmployeeAsync("Анна Смирнова", "anna@coffee.ru", "+79991112233", Position.Barista, shop1.Id, "BaristaPass123!");
        var baristaB = await CreateEmployeeAsync("Борис Волков", "boris@coffee.ru", "+79991112244", Position.Barista, shop1.Id, "BaristaPass123!");
        var shiftLeadC = await CreateEmployeeAsync("Вера Соколова", "vera@coffee.ru", "+79991112255", Position.ShiftLead, shop1.Id, "LeadPass123!");

        await _managerClient.PostAsync($"/api/v1/employees/{baristaA.Id}/shops/{shop2.Id}", null);
        await _managerClient.PostAsync($"/api/v1/employees/{baristaB.Id}/shops/{shop2.Id}", null);

        var day1 = new DateTimeOffset(2026, 12, 15, 8, 0, 0, TimeSpan.Zero);

        var shiftA1 = await CreateShiftAsync(shop1.Id, day1, day1.AddHours(8), Position.Barista, baristaA.Id, "Открытие");
        var shiftB1 = await CreateShiftAsync(shop1.Id, day1.AddHours(4), day1.AddHours(12), Position.Barista, baristaB.Id, null);
        var shiftCAdjacent = await CreateShiftAsync(shop1.Id, day1.AddHours(8), day1.AddHours(14), Position.ShiftLead, shiftLeadC.Id, null);
        var shiftCDraft = await CreateShiftAsync(shop1.Id, day1.AddHours(2), day1.AddHours(6), Position.ShiftLead, shiftLeadC.Id, null);

        var day2 = day1.AddDays(1);
        var shiftA2 = await CreateShiftAsync(shop2.Id, day2, day2.AddHours(6), Position.Barista, baristaA.Id, null);
        var shiftB2 = await CreateShiftAsync(shop2.Id, day2.AddHours(1), day2.AddHours(5), Position.Barista, baristaB.Id, null);

        var day3 = day1.AddDays(2);
        await CreateShiftAsync(shop1.Id, day3, day3.AddHours(8), Position.Barista, baristaA.Id, "Черновик");

        await _managerClient.PostAsync($"/api/v1/shifts/{shiftA1.Id}/publish", null);
        await _managerClient.PostAsync($"/api/v1/shifts/{shiftB1.Id}/publish", null);
        await _managerClient.PostAsync($"/api/v1/shifts/{shiftCAdjacent.Id}/publish", null);
        await _managerClient.PostAsync($"/api/v1/shifts/{shiftA2.Id}/publish", null);
        await _managerClient.PostAsync($"/api/v1/shifts/{shiftB2.Id}/publish", null);
        Assert.NotNull(shiftCDraft);

        var baristaClient = _factory.CreateClient();
        await _factory.LoginAsAsync(baristaClient, "anna@coffee.ru", "BaristaPass123!");

        var createShopForbidden = await baristaClient.PostAsJsonAsync(
            "/api/v1/shops",
            new CreateCoffeeShopRequest("Нельзя", "Адрес", "Europe/Moscow", new TimeOnly(8, 0), new TimeOnly(22, 0)),
            JsonOptions);
        Assert.Equal(HttpStatusCode.Forbidden, createShopForbidden.StatusCode);

        var fromParam = Uri.EscapeDataString(day1.AddHours(-1).ToString("O"));
        var toParam = Uri.EscapeDataString(day3.AddDays(1).ToString("O"));
        var schedule = await baristaClient.GetFromJsonAsync<MyScheduleResponse>(
            $"/api/v1/schedule/my?from={fromParam}&to={toParam}",
            JsonOptions);

        Assert.NotNull(schedule);
        Assert.Equal(baristaA.Id, schedule.EmployeeId);
        Assert.Equal(14.0, schedule.TotalScheduledHours);
        Assert.Equal(2, schedule.Shifts.Count);

        var firstShift = schedule.Shifts[0];
        Assert.Equal(shop1.Id, firstShift.CoffeeShopId);
        Assert.Equal("Открытие", firstShift.Notes);
        Assert.Single(firstShift.Coworkers);
        Assert.Equal(baristaB.Id, firstShift.Coworkers[0].EmployeeId);
        Assert.Equal("+79991112244", firstShift.Coworkers[0].Phone);

        var secondShift = schedule.Shifts[1];
        Assert.Equal(shop2.Id, secondShift.CoffeeShopId);
        Assert.Single(secondShift.Coworkers);
        Assert.Equal(baristaB.Id, secondShift.Coworkers[0].EmployeeId);

        var shop1VisibleToBarista = await baristaClient.GetFromJsonAsync<List<ShiftResponse>>(
            $"/api/v1/shops/{shop1.Id}/shifts?from={fromParam}&to={toParam}&status=Draft",
            JsonOptions);
        Assert.NotNull(shop1VisibleToBarista);
        Assert.All(shop1VisibleToBarista, s => Assert.Equal(ShiftStatus.Published, s.Status));

        var tomorrow = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(1);
        var myPrefResp = await baristaClient.PostAsJsonAsync(
            "/api/v1/preferences/my",
            new SubmitPreferencesRequest(tomorrow, tomorrow, null, null, PreferenceType.Preferred, "Удобно"),
            JsonOptions);
        Assert.Equal(HttpStatusCode.Created, myPrefResp.StatusCode);
    }

    private async Task<CoffeeShopResponse> CreateShopAsync(string name, string address)
    {
        var resp = await _managerClient.PostAsJsonAsync(
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
        Guid primaryShopId,
        string password)
    {
        var resp = await _managerClient.PostAsJsonAsync(
            "/api/v1/employees",
            new CreateEmployeeRequest(fullName, email, phone, position, primaryShopId, password),
            JsonOptions);
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<EmployeeResponse>(JsonOptions))!;
    }

    private async Task<ShiftResponse> CreateShiftAsync(
        Guid shopId,
        DateTimeOffset start,
        DateTimeOffset end,
        Position role,
        Guid employeeId,
        string? notes)
    {
        var resp = await _managerClient.PostAsJsonAsync(
            "/api/v1/shifts",
            new CreateShiftRequest(shopId, start, end, role, employeeId, notes),
            JsonOptions);
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<ShiftResponse>(JsonOptions))!;
    }
}
