using CoffeeSchedule.Application.Abstractions;
using CoffeeSchedule.Application.Shifts;
using CoffeeSchedule.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoffeeSchedule.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1")]
public sealed class ShiftsController(
    ShiftService shiftService,
    ICurrentUserService currentUserService) : ControllerBase
{
    [HttpGet("shops/{shopId:guid}/shifts")]
    public async Task<ActionResult<IReadOnlyList<ShiftResponse>>> GetShopSchedule(
        Guid shopId,
        [FromQuery] DateTimeOffset from,
        [FromQuery] DateTimeOffset to,
        [FromQuery] ShiftStatus? status,
        CancellationToken cancellationToken)
    {
        var canViewDrafts = currentUserService.Position is Position.Manager or Position.ShiftLead;
        var effectiveStatus = canViewDrafts ? status : ShiftStatus.Published;

        var shifts = await shiftService.GetShopScheduleAsync(shopId, from, to, effectiveStatus, cancellationToken);
        return Ok(shifts);
    }

    [HttpPost("shifts")]
    [Authorize(Roles = $"{nameof(Position.Manager)},{nameof(Position.ShiftLead)}")]
    public async Task<ActionResult<ShiftResponse>> Create(
        [FromBody] CreateShiftRequest request,
        CancellationToken cancellationToken)
    {
        var shift = await shiftService.CreateAsync(request, cancellationToken);
        return Created($"/api/v1/shifts/{shift.Id}", shift);
    }

    [HttpPut("shifts/{shiftId:guid}")]
    [Authorize(Roles = $"{nameof(Position.Manager)},{nameof(Position.ShiftLead)}")]
    public async Task<ActionResult<ShiftResponse>> Update(
        Guid shiftId,
        [FromBody] UpdateShiftRequest request,
        CancellationToken cancellationToken)
    {
        var updated = await shiftService.UpdateAsync(shiftId, request, cancellationToken);
        return Ok(updated);
    }

    [HttpPost("shifts/{shiftId:guid}/publish")]
    [Authorize(Roles = $"{nameof(Position.Manager)},{nameof(Position.ShiftLead)}")]
    public async Task<ActionResult<ShiftResponse>> Publish(
        Guid shiftId,
        CancellationToken cancellationToken)
    {
        var published = await shiftService.PublishAsync(shiftId, cancellationToken);
        return Ok(published);
    }

    [HttpPost("shops/{shopId:guid}/schedule/publish")]
    [Authorize(Roles = $"{nameof(Position.Manager)},{nameof(Position.ShiftLead)}")]
    public async Task<ActionResult<PublishScheduleResponse>> PublishShopSchedule(
        Guid shopId,
        [FromBody] PublishScheduleRequest request,
        CancellationToken cancellationToken)
    {
        var result = await shiftService.PublishShopScheduleAsync(shopId, request, cancellationToken);
        return Ok(result);
    }

    [HttpPost("shifts/{shiftId:guid}/cancel")]
    [Authorize(Roles = $"{nameof(Position.Manager)},{nameof(Position.ShiftLead)}")]
    public async Task<ActionResult<ShiftResponse>> Cancel(
        Guid shiftId,
        CancellationToken cancellationToken)
    {
        var cancelled = await shiftService.CancelAsync(shiftId, cancellationToken);
        return Ok(cancelled);
    }
}
