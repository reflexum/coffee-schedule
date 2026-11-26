using CoffeeSchedule.Domain.Entities;

namespace CoffeeSchedule.Application.Abstractions;

public interface IJwtTokenGenerator
{
    string GenerateToken(Employee employee);
}
