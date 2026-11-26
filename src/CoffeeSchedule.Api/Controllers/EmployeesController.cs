using CoffeeSchedule.Application.Abstractions;
using CoffeeSchedule.Application.Employees;
using CoffeeSchedule.Domain.Enums;
using CoffeeSchedule.Domain.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoffeeSchedule.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/employees")]
public sealed class EmployeesController(
    EmployeeService employeeService,
    ICurrentUserService currentUserService) : ControllerBase
{
    [HttpGet]
    [Authorize(Roles = $"{nameof(Position.Manager)},{nameof(Position.ShiftLead)}")]
    public async Task<ActionResult<IReadOnlyList<EmployeeResponse>>> GetAll(CancellationToken cancellationToken)
    {
        var employees = await employeeService.GetAllAsync(cancellationToken);
        return Ok(employees);
    }

    [HttpPost]
    [Authorize(Roles = nameof(Position.Manager))]
    public async Task<ActionResult<EmployeeResponse>> Create(
        [FromBody] CreateEmployeeRequest request,
        CancellationToken cancellationToken)
    {
        var created = await employeeService.CreateAsync(request, cancellationToken);
        return Created($"/api/v1/employees/{created.Id}", created);
    }

    [HttpPost("{employeeId:guid}/shops/{coffeeShopId:guid}")]
    [Authorize(Roles = nameof(Position.Manager))]
    public async Task<IActionResult> GrantShopAccess(
        Guid employeeId,
        Guid coffeeShopId,
        CancellationToken cancellationToken)
    {
        await employeeService.GrantShopAccessAsync(employeeId, coffeeShopId, cancellationToken);
        return NoContent();
    }

    [HttpPost("{employeeId:guid}/password")]
    public async Task<IActionResult> SetPassword(
        Guid employeeId,
        [FromBody] SetPasswordRequest request,
        CancellationToken cancellationToken)
    {
        if (currentUserService.EmployeeId != employeeId && currentUserService.Position != Position.Manager)
        {
            throw new ForbiddenException("Сменить пароль другого сотрудника может только управляющий.");
        }

        await employeeService.SetPasswordAsync(employeeId, request, cancellationToken);
        return NoContent();
    }
}
