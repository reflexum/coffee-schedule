using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using CoffeeSchedule.Application.CoffeeShops;
using CoffeeSchedule.Application.Employees;
using CoffeeSchedule.Application.Preferences;
using CoffeeSchedule.Application.Shifts;
using CoffeeSchedule.Domain.Enums;

namespace CoffeeSchedule.Tests.Api;

public class PreferencesEndpointsTests : IAsyncLifetime
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
        await _factory.AuthenticateAsManagerAsync(_client);
    }

    public async Task DisposeAsync()
    {
        await _factory.DisposeAsync();
    }

    [Fact]
    public async Task SubmitAndGetShopPreferences_CreatesRangeAndFiltersByShop()
    {
        var shopA = await CreateShopAsync("Покровка", "ул. Покровка, 10");
        var shopB = await CreateShopAsync("Маросейка", "ул. Маросейка, 4");

        var employeeA = await CreateEmployeeAsync("Анна", "anna@coffee.ru", "+79991112233", Position.Barista, shopA.Id);
        var employeeB = await CreateEmployeeAsync("Борис", "boris@coffee.ru", "+79991112244", Position.Barista, shopB.Id);

        var tomorrow = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(1);
        var endDate = tomorrow.AddDays(2);

        var submitResp = await _client.PostAsJsonAsync(
            $"/api/v1/employees/{employeeA.Id}/preferences",
            new SubmitPreferencesRequest(
                tomorrow,
                endDate,
                new TimeOnly(9, 0),
                new TimeOnly(15, 0),
                PreferenceType.Unavailable,
                "Сессия"),
            JsonOptions);

        Assert.Equal(HttpStatusCode.Created, submitResp.StatusCode);
        var created = await submitResp.Content.ReadFromJsonAsync<List<ShiftPreferenceResponse>>(JsonOptions);
        Assert.NotNull(created);
        Assert.Equal(3, created.Count);

        var submitOtherResp = await _client.PostAsJsonAsync(
            $"/api/v1/employees/{employeeB.Id}/preferences",
            new SubmitPreferencesRequest(
                tomorrow,
                tomorrow,
                null,
                null,
                PreferenceType.Preferred,
                "Готов выйти весь день"),
            JsonOptions);
        Assert.Equal(HttpStatusCode.Created, submitOtherResp.StatusCode);

        var shopAPrefs = await _client.GetFromJsonAsync<List<ShiftPreferenceResponse>>(
            $"/api/v1/shops/{shopA.Id}/preferences?from={tomorrow:yyyy-MM-dd}&to={endDate:yyyy-MM-dd}",
            JsonOptions);

        Assert.NotNull(shopAPrefs);
        Assert.Equal(3, shopAPrefs.Count);
        Assert.All(shopAPrefs, p => Assert.Equal(employeeA.Id, p.EmployeeId));
    }

    [Fact]
    public async Task SubmitPreferences_PastDateOrInvalidRange_Returns400ValidationProblem()
    {
        var shop = await CreateShopAsync("Центр", "Тверская, 1");
        var employee = await CreateEmployeeAsync("Виктор", "viktor@coffee.ru", "+79993334455", Position.Barista, shop.Id);

        var yesterday = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-1);
        var response = await _client.PostAsJsonAsync(
            $"/api/v1/employees/{employee.Id}/preferences",
            new SubmitPreferencesRequest(yesterday, yesterday, null, null, PreferenceType.Unavailable, null),
            JsonOptions);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task SubmitOverlappingPreferenceOnSameDate_Returns409Conflict()
    {
        var shop = await CreateShopAsync("Тверская", "Тверская, 10");
        var employee = await CreateEmployeeAsync("Елена", "elena@coffee.ru", "+79994445566", Position.Barista, shop.Id);

        var tomorrow = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(2);

        var first = await _client.PostAsJsonAsync(
            $"/api/v1/employees/{employee.Id}/preferences",
            new SubmitPreferencesRequest(
                tomorrow,
                tomorrow,
                new TimeOnly(9, 0),
                new TimeOnly(14, 0),
                PreferenceType.Unavailable,
                "Университет"),
            JsonOptions);
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);

        var overlapping = await _client.PostAsJsonAsync(
            $"/api/v1/employees/{employee.Id}/preferences",
            new SubmitPreferencesRequest(
                tomorrow,
                tomorrow,
                new TimeOnly(12, 0),
                new TimeOnly(18, 0),
                PreferenceType.Preferred,
                "Могу после 12"),
            JsonOptions);
        Assert.Equal(HttpStatusCode.Conflict, overlapping.StatusCode);

        var nonOverlapping = await _client.PostAsJsonAsync(
            $"/api/v1/employees/{employee.Id}/preferences",
            new SubmitPreferencesRequest(
                tomorrow,
                tomorrow,
                new TimeOnly(14, 0),
                new TimeOnly(22, 0),
                PreferenceType.Preferred,
                "Вечерняя"),
            JsonOptions);
        Assert.Equal(HttpStatusCode.Created, nonOverlapping.StatusCode);
    }

    [Fact]
    public async Task CreatingShiftAgainstUnavailablePreference_ReturnsWarningInShiftResponse()
    {
        var shop = await CreateShopAsync("Арбат", "Арбат, 20");
        var employee = await CreateEmployeeAsync("Глеб", "gleb@coffee.ru", "+79995556677", Position.Barista, shop.Id);

        var tomorrow = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(3);

        var prefResp = await _client.PostAsJsonAsync(
            $"/api/v1/employees/{employee.Id}/preferences",
            new SubmitPreferencesRequest(
                tomorrow,
                tomorrow,
                new TimeOnly(9, 0),
                new TimeOnly(13, 0),
                PreferenceType.Unavailable,
                "Экзамен"),
            JsonOptions);
        Assert.Equal(HttpStatusCode.Created, prefResp.StatusCode);

        var moscowOffset = TimeSpan.FromHours(3);
        var shiftStart = new DateTimeOffset(tomorrow.ToDateTime(new TimeOnly(10, 0)), moscowOffset);
        var shiftEnd = new DateTimeOffset(tomorrow.ToDateTime(new TimeOnly(18, 0)), moscowOffset);

        var shiftResp = await _client.PostAsJsonAsync(
            "/api/v1/shifts",
            new CreateShiftRequest(shop.Id, shiftStart, shiftEnd, Position.Barista, employee.Id, null),
            JsonOptions);

        Assert.Equal(HttpStatusCode.Created, shiftResp.StatusCode);
        var created = await shiftResp.Content.ReadFromJsonAsync<ShiftResponse>(JsonOptions);
        Assert.NotNull(created);
        Assert.NotNull(created.Warnings);
        Assert.Contains(created.Warnings, w => w.Contains("Экзамен"));
    }

    [Fact]
    public async Task DeletePreference_RemovesRecord()
    {
        var shop = await CreateShopAsync("Китай-город", "Маросейка, 12");
        var employee = await CreateEmployeeAsync("Никита", "nikita@coffee.ru", "+79996667788", Position.Barista, shop.Id);
        var tomorrow = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(1);

        var submitResp = await _client.PostAsJsonAsync(
            $"/api/v1/employees/{employee.Id}/preferences",
            new SubmitPreferencesRequest(tomorrow, tomorrow, null, null, PreferenceType.Preferred, null),
            JsonOptions);
        var created = await submitResp.Content.ReadFromJsonAsync<List<ShiftPreferenceResponse>>(JsonOptions);
        Assert.NotNull(created);
        var preferenceId = created[0].Id;

        var deleteResp = await _client.DeleteAsync($"/api/v1/employees/{employee.Id}/preferences/{preferenceId}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResp.StatusCode);

        var list = await _client.GetFromJsonAsync<List<ShiftPreferenceResponse>>(
            $"/api/v1/employees/{employee.Id}/preferences?from={tomorrow:yyyy-MM-dd}&to={tomorrow:yyyy-MM-dd}",
            JsonOptions);
        Assert.NotNull(list);
        Assert.Empty(list);
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
