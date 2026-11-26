using CoffeeSchedule.Domain.Enums;
using FluentValidation;

namespace CoffeeSchedule.Application.Employees;

public sealed record CreateEmployeeRequest(
    string FullName,
    string Email,
    string Phone,
    Position Position,
    Guid? PrimaryCoffeeShopId,
    string? Password = null);

public sealed record SetPasswordRequest(string Password);

public sealed record EmployeeResponse(
    Guid Id,
    string FullName,
    string Email,
    string Phone,
    Position Position,
    Guid? PrimaryCoffeeShopId,
    bool IsActive,
    IReadOnlyList<Guid> AccessibleCoffeeShopIds);

public sealed class CreateEmployeeRequestValidator : AbstractValidator<CreateEmployeeRequest>
{
    public CreateEmployeeRequestValidator()
    {
        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("ФИО сотрудника обязательно.")
            .MaximumLength(200);

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email обязателен.")
            .EmailAddress().WithMessage("Некорректный формат email.")
            .MaximumLength(256);

        RuleFor(x => x.Phone)
            .NotEmpty().WithMessage("Телефон обязателен.")
            .MaximumLength(32);

        RuleFor(x => x.Position)
            .IsInEnum().WithMessage("Указана недопустимая должность.");

        RuleFor(x => x.Password)
            .MinimumLength(8).WithMessage("Пароль должен содержать минимум 8 символов.")
            .When(x => !string.IsNullOrEmpty(x.Password));
    }
}

public sealed class SetPasswordRequestValidator : AbstractValidator<SetPasswordRequest>
{
    public SetPasswordRequestValidator()
    {
        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Пароль обязателен.")
            .MinimumLength(8).WithMessage("Пароль должен содержать минимум 8 символов.");
    }
}
