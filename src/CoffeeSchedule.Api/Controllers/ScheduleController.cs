using CoffeeSchedule.Application.Abstractions;
using CoffeeSchedule.Application.Schedule;
using CoffeeSchedule.Domain.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoffeeSchedule.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/schedule")]
public sealed class ScheduleController(
    ScheduleService scheduleService,
    ICurrentUserService currentUserService) : ControllerBase
{
    [HttpGet("my")]
    public async Task<ActionResult<MyScheduleResponse>> GetMySchedule(
        [FromQuery] DateTimeOffset? from,
        [FromQuery] DateTimeOffset? to,
        CancellationToken cancellationToken)
    {
        var employeeId = currentUserService.EmployeeId
            ?? throw new UnauthorizedException("Не удалось определить текущего сотрудника.");

        var schedule = await scheduleService.GetMyScheduleAsync(employeeId, from, to, cancellationToken);
        return Ok(schedule);
    }
}
