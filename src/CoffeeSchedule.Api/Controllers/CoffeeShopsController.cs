using CoffeeSchedule.Application.CoffeeShops;
using Microsoft.AspNetCore.Mvc;

namespace CoffeeSchedule.Api.Controllers;

[ApiController]
[Route("api/v1/shops")]
public sealed class CoffeeShopsController(CoffeeShopService coffeeShopService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CoffeeShopResponse>>> GetAll(CancellationToken cancellationToken)
    {
        var shops = await coffeeShopService.GetAllAsync(cancellationToken);
        return Ok(shops);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CoffeeShopResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var shop = await coffeeShopService.GetByIdAsync(id, cancellationToken);
        return Ok(shop);
    }

    [HttpPost]
    public async Task<ActionResult<CoffeeShopResponse>> Create(
        [FromBody] CreateCoffeeShopRequest request,
        CancellationToken cancellationToken)
    {
        var created = await coffeeShopService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }
}
