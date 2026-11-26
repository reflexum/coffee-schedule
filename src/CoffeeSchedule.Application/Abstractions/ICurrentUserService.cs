using CoffeeSchedule.Domain.Enums;

namespace CoffeeSchedule.Application.Abstractions;

public interface ICurrentUserService
{
    Guid? EmployeeId { get; }

    Position? Position { get; }
}
