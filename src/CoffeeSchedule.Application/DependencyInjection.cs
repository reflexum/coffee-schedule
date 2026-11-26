using CoffeeSchedule.Application.CoffeeShops;
using CoffeeSchedule.Application.Employees;
using CoffeeSchedule.Application.Shifts;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace CoffeeSchedule.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);
        services.AddScoped<CoffeeShopService>();
        services.AddScoped<EmployeeService>();
        services.AddScoped<ShiftService>();
        return services;
    }
}
