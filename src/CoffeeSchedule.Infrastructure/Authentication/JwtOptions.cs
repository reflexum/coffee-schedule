namespace CoffeeSchedule.Infrastructure.Authentication;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "CoffeeSchedule";

    public string Audience { get; set; } = "CoffeeSchedule.Clients";

    public string SigningKey { get; set; } = string.Empty;

    public int LifetimeMinutes { get; set; } = 480;
}
