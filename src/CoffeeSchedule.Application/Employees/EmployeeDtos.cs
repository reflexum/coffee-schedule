using CoffeeSchedule.Domain.Enums;
using FluentValidation;

namespace CoffeeSchedule.Application.Employees;

public sealed record CreateEmployeeRequest(
    string FullName,
    string Email,
    string Phone,
    Position Position,
    Guid? PrimaryCoffeeShopId);

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
    }
}
