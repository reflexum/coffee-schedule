using FluentValidation;

namespace CoffeeSchedule.Application.CoffeeShops;

public sealed record CreateCoffeeShopRequest(
    string Name,
    string Address,
    string TimeZoneId,
    TimeOnly OpeningTime,
    TimeOnly ClosingTime);

public sealed record CoffeeShopResponse(
    Guid Id,
    string Name,
    string Address,
    string TimeZoneId,
    TimeOnly OpeningTime,
    TimeOnly ClosingTime,
    bool IsActive);

public sealed class CreateCoffeeShopRequestValidator : AbstractValidator<CreateCoffeeShopRequest>
{
    public CreateCoffeeShopRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Название кофейни обязательно.")
            .MaximumLength(150);

        RuleFor(x => x.Address)
            .NotEmpty().WithMessage("Адрес кофейни обязателен.")
            .MaximumLength(300);

        RuleFor(x => x.TimeZoneId)
            .NotEmpty().WithMessage("Часовой пояс обязателен.")
            .Must(tz => TimeZoneInfo.TryFindSystemTimeZoneById(tz.Trim(), out _))
            .WithMessage("Указан неизвестный часовой пояс.");

        RuleFor(x => x)
            .Must(x => x.OpeningTime != x.ClosingTime)
            .WithMessage("Время открытия и закрытия не могут совпадать.");
    }
}
