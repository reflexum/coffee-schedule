using CoffeeSchedule.Application.Abstractions;
using CoffeeSchedule.Infrastructure.Authentication;
using CoffeeSchedule.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CoffeeSchedule.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Postgres")
            ?? throw new InvalidOperationException("Не задана строка подключения 'ConnectionStrings:Postgres'.");

        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>());

        var jwtSection = configuration.GetSection(JwtOptions.SectionName);
        services.Configure<JwtOptions>(options =>
        {
            options.Issuer = jwtSection[nameof(JwtOptions.Issuer)] ?? options.Issuer;
            options.Audience = jwtSection[nameof(JwtOptions.Audience)] ?? options.Audience;
            options.SigningKey = jwtSection[nameof(JwtOptions.SigningKey)] ?? options.SigningKey;
            if (int.TryParse(jwtSection[nameof(JwtOptions.LifetimeMinutes)], out var lifetime))
            {
                options.LifetimeMinutes = lifetime;
            }
        });

        services.AddSingleton<IPasswordHasherService, PasswordHasherService>();
        services.AddSingleton<IJwtTokenGenerator, JwtTokenGenerator>();

        return services;
    }
}
