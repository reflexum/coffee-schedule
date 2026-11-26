using CoffeeSchedule.Application.Preferences;
using CoffeeSchedule.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace CoffeeSchedule.Api.Controllers;

[ApiController]
[Route("api/v1")]
public sealed class PreferencesController(PreferenceService preferenceService) : ControllerBase
{
    [HttpPost("employees/{employeeId:guid}/preferences")]
    public async Task<ActionResult<IReadOnlyList<ShiftPreferenceResponse>>> Submit(
        Guid employeeId,
        [FromBody] SubmitPreferencesRequest request,
        CancellationToken cancellationToken)
    {
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
        var preferences = await preferenceService.GetEmployeePreferencesAsync(employeeId, from, to, cancellationToken);
        return Ok(preferences);
    }

    [HttpDelete("employees/{employeeId:guid}/preferences/{preferenceId:guid}")]
    public async Task<IActionResult> Delete(
        Guid employeeId,
        Guid preferenceId,
        CancellationToken cancellationToken)
    {
        await preferenceService.DeleteAsync(employeeId, preferenceId, cancellationToken);
        return NoContent();
    }

    [HttpGet("shops/{shopId:guid}/preferences")]
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
}
