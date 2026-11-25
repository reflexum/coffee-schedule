using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CoffeeSchedule.Domain.Exceptions;
using FluentValidation;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CoffeeSchedule.Tests;

public class ExceptionHandlingAndValidationTests
{
    private static WebApplicationFactory<Program> CreateFactory(InMemoryLoggerProvider? loggerProvider = null)
    {
        return new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Production");
            builder.ConfigureTestServices(services =>
            {
                services.AddScoped<IValidator<TestValidationPayload>, TestValidationPayloadValidator>();
                services.AddControllers().AddApplicationPart(typeof(TestDiagnosticsController).Assembly);
            });

            if (loggerProvider is not null)
            {
                builder.ConfigureLogging(logging =>
                {
                    logging.ClearProviders();
                    logging.AddProvider(loggerProvider);
                });
            }
        });
    }

    [Fact]
    public async Task ValidationFailure_Returns400ProblemDetailsWithFieldErrors()
    {
        await using var factory = CreateFactory();
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/__test/validate", new TestValidationPayload(string.Empty, 0));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(400, body.GetProperty("status").GetInt32());
        Assert.True(body.TryGetProperty("traceId", out _));

        var errors = body.GetProperty("errors");
        Assert.True(errors.TryGetProperty(nameof(TestValidationPayload.Name), out var nameErrors));
        Assert.True(nameErrors.GetArrayLength() > 0);
        Assert.True(errors.TryGetProperty(nameof(TestValidationPayload.Hours), out var hoursErrors));
        Assert.True(hoursErrors.GetArrayLength() > 0);
    }

    [Fact]
    public async Task ValidPayload_PassesValidationFilter()
    {
        await using var factory = CreateFactory();
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/__test/validate", new TestValidationPayload("Бариста", 8));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Theory]
    [InlineData("not-found", HttpStatusCode.NotFound, "Сотрудник не найден")]
    [InlineData("conflict", HttpStatusCode.Conflict, "Смена пересекается")]
    [InlineData("forbidden", HttpStatusCode.Forbidden, "Недостаточно прав")]
    [InlineData("domain", HttpStatusCode.UnprocessableEntity, "Некорректный статус смены")]
    public async Task DomainExceptions_ReturnExpectedProblemDetails(
        string kind,
        HttpStatusCode expectedStatus,
        string expectedDetail)
    {
        await using var factory = CreateFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync($"/__test/throw/{kind}");

        Assert.Equal(expectedStatus, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal((int)expectedStatus, problem.Status);
        Assert.Equal(expectedDetail, problem.Detail);
    }

    [Fact]
    public async Task UnhandledException_LogsErrorAndReturns500WithoutInternalDetails()
    {
        var loggerProvider = new InMemoryLoggerProvider();
        await using var factory = CreateFactory(loggerProvider);
        var client = factory.CreateClient();

        var response = await client.GetAsync("/__test/throw/unhandled");

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal(500, problem.Status);
        Assert.DoesNotContain("internal-secret-error", problem.Detail ?? string.Empty);

        Assert.Contains(
            loggerProvider.Entries,
            e => e.Level == LogLevel.Error && e.Exception is InvalidOperationException);
    }

    private sealed class InMemoryLoggerProvider : ILoggerProvider
    {
        public List<(LogLevel Level, string Message, Exception? Exception)> Entries { get; } = [];

        public ILogger CreateLogger(string categoryName) => new InMemoryLogger(Entries);

        public void Dispose()
        {
        }

        private sealed class InMemoryLogger(
            List<(LogLevel Level, string Message, Exception? Exception)> entries) : ILogger
        {
            public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

            public bool IsEnabled(LogLevel logLevel) => true;

            public void Log<TState>(
                LogLevel logLevel,
                EventId eventId,
                TState state,
                Exception? exception,
                Func<TState, Exception?, string> formatter)
            {
                lock (entries)
                {
                    entries.Add((logLevel, formatter(state, exception), exception));
                }
            }
        }
    }
}

public sealed record TestValidationPayload(string Name, int Hours);

public sealed class TestValidationPayloadValidator : AbstractValidator<TestValidationPayload>
{
    public TestValidationPayloadValidator()
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage("Имя обязательно");
        RuleFor(x => x.Hours).GreaterThan(0).WithMessage("Часы должны быть больше нуля");
    }
}

[ApiController]
[Route("__test")]
public sealed class TestDiagnosticsController : ControllerBase
{
    [HttpPost("validate")]
    public IActionResult Validate([FromBody] TestValidationPayload payload) => Ok(payload);

    [HttpGet("throw/{kind}")]
    public IActionResult Throw(string kind)
    {
        throw kind switch
        {
            "not-found" => new NotFoundException("Сотрудник не найден"),
            "conflict" => new ConflictException("Смена пересекается"),
            "forbidden" => new ForbiddenException("Недостаточно прав"),
            "domain" => new DomainException("Некорректный статус смены"),
            _ => new InvalidOperationException("internal-secret-error")
        };
    }
}
