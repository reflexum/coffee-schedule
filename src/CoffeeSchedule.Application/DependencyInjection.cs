using CoffeeSchedule.Application.Auth;
using CoffeeSchedule.Application.CoffeeShops;
using CoffeeSchedule.Application.Employees;
using CoffeeSchedule.Application.Preferences;
using CoffeeSchedule.Application.Schedule;
using CoffeeSchedule.Application.Shifts;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CoffeeSchedule.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.TryAddSingleton(TimeProvider.System);
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);
        services.AddScoped<AuthService>();
        services.AddScoped<CoffeeShopService>();
        services.AddScoped<EmployeeService>();
        services.AddScoped<ShiftService>();
        services.AddScoped<PreferenceService>();
        services.AddScoped<ScheduleService>();
        return services;
    }
}
