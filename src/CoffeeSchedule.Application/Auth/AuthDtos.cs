using CoffeeSchedule.Domain.Enums;
using FluentValidation;

namespace CoffeeSchedule.Application.Auth;

public sealed record LoginRequest(string Email, string Password);

public sealed record BootstrapManagerRequest(
    string FullName,
    string Email,
    string Phone,
    string Password);

public sealed record AuthResponse(
    string AccessToken,
    Guid EmployeeId,
    string FullName,
    Position Position);

public sealed class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email обязателен.")
            .EmailAddress().WithMessage("Некорректный формат email.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Пароль обязателен.");
    }
}

public sealed class BootstrapManagerRequestValidator : AbstractValidator<BootstrapManagerRequest>
{
    public BootstrapManagerRequestValidator()
    {
        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("ФИО обязательно.")
            .MaximumLength(200);

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email обязателен.")
            .EmailAddress().WithMessage("Некорректный формат email.")
            .MaximumLength(256);

        RuleFor(x => x.Phone)
            .NotEmpty().WithMessage("Телефон обязателен.")
            .MaximumLength(32);

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Пароль обязателен.")
            .MinimumLength(8).WithMessage("Пароль должен содержать минимум 8 символов.");
    }
}
