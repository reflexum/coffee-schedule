using System.Security.Claims;
using CoffeeSchedule.Application.Abstractions;
using CoffeeSchedule.Domain.Enums;

namespace CoffeeSchedule.Api.Services;

public sealed class CurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUserService
{
    public Guid? EmployeeId
    {
        get
        {
            var value = httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(value, out var id) ? id : null;
        }
    }

    public Position? Position
    {
        get
        {
            var value = httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.Role);
            return Enum.TryParse<Position>(value, out var position) ? position : null;
        }
    }
}
