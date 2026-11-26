using CoffeeSchedule.Domain.Enums;
using FluentValidation;

namespace CoffeeSchedule.Application.Preferences;

public sealed record SubmitPreferencesRequest(
    DateOnly FromDate,
    DateOnly ToDate,
    TimeOnly? StartTime,
    TimeOnly? EndTime,
    PreferenceType Type,
    string? Comment);

public sealed record ShiftPreferenceResponse(
    Guid Id,
    Guid EmployeeId,
    string EmployeeName,
    Position EmployeePosition,
    DateOnly Date,
    TimeOnly? StartTime,
    TimeOnly? EndTime,
    PreferenceType Type,
    string? Comment,
    DateTimeOffset CreatedAtUtc);

public sealed class SubmitPreferencesRequestValidator : AbstractValidator<SubmitPreferencesRequest>
{
    public SubmitPreferencesRequestValidator(TimeProvider timeProvider)
    {
        RuleFor(x => x.FromDate)
            .GreaterThanOrEqualTo(_ => DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime))
            .WithMessage("Нельзя подать пожелание на прошедшую дату.");

        RuleFor(x => x.ToDate)
            .GreaterThanOrEqualTo(x => x.FromDate)
            .WithMessage("Конечная дата периода не может быть раньше начальной.");

        RuleFor(x => x)
            .Must(x => x.ToDate.DayNumber - x.FromDate.DayNumber <= 30)
            .WithMessage("За один запрос можно задать период не более 31 дня.");

        RuleFor(x => x)
            .Must(x => x.StartTime.HasValue == x.EndTime.HasValue)
            .WithMessage("Укажите одновременно время начала и окончания, либо оставьте оба поля пустыми для полного дня.");

        RuleFor(x => x)
            .Must(x => !x.StartTime.HasValue || !x.EndTime.HasValue || x.StartTime.Value < x.EndTime.Value)
            .WithMessage("Время окончания должно быть позже времени начала.");

        RuleFor(x => x.Type)
            .IsInEnum()
            .WithMessage("Указан недопустимый тип пожелания.");

        RuleFor(x => x.Comment)
            .MaximumLength(300)
            .When(x => x.Comment is not null);
    }
}
