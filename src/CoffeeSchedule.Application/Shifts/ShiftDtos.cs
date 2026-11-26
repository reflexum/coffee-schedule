using CoffeeSchedule.Domain.Enums;
using FluentValidation;

namespace CoffeeSchedule.Application.Shifts;

public sealed record CreateShiftRequest(
    Guid CoffeeShopId,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt,
    Position RequiredPosition,
    Guid? AssignedEmployeeId,
    string? Notes);

public sealed record UpdateShiftRequest(
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt,
    Position RequiredPosition,
    Guid? AssignedEmployeeId,
    string? Notes);

public sealed record PublishScheduleRequest(
    DateTimeOffset From,
    DateTimeOffset To);

public sealed record ShiftResponse(
    Guid Id,
    Guid CoffeeShopId,
    string CoffeeShopName,
    DateTimeOffset StartsAtUtc,
    DateTimeOffset EndsAtUtc,
    Position RequiredPosition,
    Guid? AssignedEmployeeId,
    string? AssignedEmployeeName,
    ShiftStatus Status,
    string? Notes,
    DateTimeOffset? PublishedAtUtc);

public sealed record PublishScheduleResponse(
    Guid CoffeeShopId,
    int PublishedCount,
    IReadOnlyList<ShiftResponse> Shifts);

public sealed class CreateShiftRequestValidator : AbstractValidator<CreateShiftRequest>
{
    public CreateShiftRequestValidator()
    {
        RuleFor(x => x.CoffeeShopId)
            .NotEmpty().WithMessage("Идентификатор кофейни обязателен.");

        RuleFor(x => x.EndsAt)
            .GreaterThan(x => x.StartsAt)
            .WithMessage("Время окончания смены должно быть позже времени начала.");

        RuleFor(x => x)
            .Must(x => (x.EndsAt.ToUniversalTime() - x.StartsAt.ToUniversalTime()) <= TimeSpan.FromHours(24))
            .WithMessage("Длительность смены не может превышать 24 часа.");

        RuleFor(x => x.RequiredPosition)
            .IsInEnum().WithMessage("Указана недопустимая должность.");

        RuleFor(x => x.Notes)
            .MaximumLength(500).When(x => x.Notes is not null);
    }
}

public sealed class UpdateShiftRequestValidator : AbstractValidator<UpdateShiftRequest>
{
    public UpdateShiftRequestValidator()
    {
        RuleFor(x => x.EndsAt)
            .GreaterThan(x => x.StartsAt)
            .WithMessage("Время окончания смены должно быть позже времени начала.");

        RuleFor(x => x)
            .Must(x => (x.EndsAt.ToUniversalTime() - x.StartsAt.ToUniversalTime()) <= TimeSpan.FromHours(24))
            .WithMessage("Длительность смены не может превышать 24 часа.");

        RuleFor(x => x.RequiredPosition)
            .IsInEnum().WithMessage("Указана недопустимая должность.");

        RuleFor(x => x.Notes)
            .MaximumLength(500).When(x => x.Notes is not null);
    }
}

public sealed class PublishScheduleRequestValidator : AbstractValidator<PublishScheduleRequest>
{
    public PublishScheduleRequestValidator()
    {
        RuleFor(x => x.To)
            .GreaterThan(x => x.From)
            .WithMessage("Конец периода публикации должен быть позже начала.");
    }
}
