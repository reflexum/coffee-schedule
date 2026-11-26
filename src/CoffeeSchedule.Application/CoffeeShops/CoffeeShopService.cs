using CoffeeSchedule.Application.Abstractions;
using CoffeeSchedule.Domain.Entities;
using CoffeeSchedule.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace CoffeeSchedule.Application.CoffeeShops;

public sealed class CoffeeShopService(IAppDbContext dbContext)
{
    public async Task<CoffeeShopResponse> CreateAsync(
        CreateCoffeeShopRequest request,
        CancellationToken cancellationToken = default)
    {
        var shop = CoffeeShop.Create(
            request.Name,
            request.Address,
            request.TimeZoneId,
            request.OpeningTime,
            request.ClosingTime);

        dbContext.CoffeeShops.Add(shop);
        await dbContext.SaveChangesAsync(cancellationToken);

        return MapToResponse(shop);
    }

    public async Task<IReadOnlyList<CoffeeShopResponse>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var shops = await dbContext.CoffeeShops
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);

        return shops.Select(MapToResponse).ToList();
    }

    public async Task<CoffeeShopResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var shop = await dbContext.CoffeeShops
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException($"Кофейня '{id}' не найдена.");

        return MapToResponse(shop);
    }

    private static CoffeeShopResponse MapToResponse(CoffeeShop shop) =>
        new(shop.Id, shop.Name, shop.Address, shop.TimeZoneId, shop.OpeningTime, shop.ClosingTime, shop.IsActive);
}
