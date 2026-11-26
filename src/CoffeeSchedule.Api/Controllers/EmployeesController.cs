using CoffeeSchedule.Application.Employees;
using Microsoft.AspNetCore.Mvc;

namespace CoffeeSchedule.Api.Controllers;

[ApiController]
[Route("api/v1/employees")]
public sealed class EmployeesController(EmployeeService employeeService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<EmployeeResponse>>> GetAll(CancellationToken cancellationToken)
    {
        var employees = await employeeService.GetAllAsync(cancellationToken);
        return Ok(employees);
    }

    [HttpPost]
    public async Task<ActionResult<EmployeeResponse>> Create(
        [FromBody] CreateEmployeeRequest request,
        CancellationToken cancellationToken)
    {
        var created = await employeeService.CreateAsync(request, cancellationToken);
        return Created($"/api/v1/employees/{created.Id}", created);
    }

    [HttpPost("{employeeId:guid}/shops/{coffeeShopId:guid}")]
    public async Task<IActionResult> GrantShopAccess(
        Guid employeeId,
        Guid coffeeShopId,
        CancellationToken cancellationToken)
    {
        await employeeService.GrantShopAccessAsync(employeeId, coffeeShopId, cancellationToken);
        return NoContent();
    }
}
