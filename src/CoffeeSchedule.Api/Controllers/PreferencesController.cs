using CoffeeSchedule.Application.Abstractions;
using CoffeeSchedule.Application.Preferences;
using CoffeeSchedule.Domain.Enums;
using CoffeeSchedule.Domain.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoffeeSchedule.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1")]
public sealed class PreferencesController(
    PreferenceService preferenceService,
    ICurrentUserService currentUserService) : ControllerBase
{
    [HttpPost("preferences/my")]
    public async Task<ActionResult<IReadOnlyList<ShiftPreferenceResponse>>> SubmitMy(
        [FromBody] SubmitPreferencesRequest request,
        CancellationToken cancellationToken)
    {
        var employeeId = currentUserService.EmployeeId
            ?? throw new UnauthorizedException("Не удалось определить текущего сотрудника.");

        var created = await preferenceService.SubmitForEmployeeAsync(employeeId, request, cancellationToken);
        return Created("/api/v1/preferences/my", created);
    }

    [HttpGet("preferences/my")]
    public async Task<ActionResult<IReadOnlyList<ShiftPreferenceResponse>>> GetMy(
        [FromQuery] DateOnly from,
        [FromQuery] DateOnly to,
        CancellationToken cancellationToken)
    {
        var employeeId = currentUserService.EmployeeId
            ?? throw new UnauthorizedException("Не удалось определить текущего сотрудника.");

        var preferences = await preferenceService.GetEmployeePreferencesAsync(employeeId, from, to, cancellationToken);
        return Ok(preferences);
    }

    [HttpDelete("preferences/my/{preferenceId:guid}")]
    public async Task<IActionResult> DeleteMy(
        Guid preferenceId,
        CancellationToken cancellationToken)
    {
        var employeeId = currentUserService.EmployeeId
            ?? throw new UnauthorizedException("Не удалось определить текущего сотрудника.");

        await preferenceService.DeleteAsync(employeeId, preferenceId, cancellationToken);
        return NoContent();
    }

    [HttpPost("employees/{employeeId:guid}/preferences")]
    public async Task<ActionResult<IReadOnlyList<ShiftPreferenceResponse>>> Submit(
        Guid employeeId,
        [FromBody] SubmitPreferencesRequest request,
        CancellationToken cancellationToken)
    {
        EnsureSelfOrLeadership(employeeId);
        var created = await preferenceService.SubmitForEmployeeAsync(employeeId, request, cancellationToken);
        return Created($"/api/v1/employees/{employeeId}/preferences", created);
    }

    [HttpGet("employees/{employeeId:guid}/preferences")]
    public async Task<ActionResult<IReadOnlyList<ShiftPreferenceResponse>>> GetEmployeePreferences(
        Guid employeeId,
        [FromQuery] DateOnly from,
        [FromQuery] DateOnly to,
        CancellationToken cancellationToken)
    {
        EnsureSelfOrLeadership(employeeId);
        var preferences = await preferenceService.GetEmployeePreferencesAsync(employeeId, from, to, cancellationToken);
        return Ok(preferences);
    }

    [HttpDelete("employees/{employeeId:guid}/preferences/{preferenceId:guid}")]
    public async Task<IActionResult> Delete(
        Guid employeeId,
        Guid preferenceId,
        CancellationToken cancellationToken)
    {
        EnsureSelfOrLeadership(employeeId);
        await preferenceService.DeleteAsync(employeeId, preferenceId, cancellationToken);
        return NoContent();
    }

    [HttpGet("shops/{shopId:guid}/preferences")]
    [Authorize(Roles = $"{nameof(Position.Manager)},{nameof(Position.ShiftLead)}")]
    public async Task<ActionResult<IReadOnlyList<ShiftPreferenceResponse>>> GetShopPreferences(
        Guid shopId,
        [FromQuery] DateOnly from,
        [FromQuery] DateOnly to,
        [FromQuery] PreferenceType? type,
        CancellationToken cancellationToken)
    {
        var preferences = await preferenceService.GetShopPreferencesAsync(shopId, from, to, type, cancellationToken);
        return Ok(preferences);
    }

    private void EnsureSelfOrLeadership(Guid targetEmployeeId)
    {
        var isLeadership = currentUserService.Position is Position.Manager or Position.ShiftLead;
        if (!isLeadership && currentUserService.EmployeeId != targetEmployeeId)
        {
            throw new ForbiddenException("Нет прав на управление пожеланиями другого сотрудника.");
        }
    }
}
