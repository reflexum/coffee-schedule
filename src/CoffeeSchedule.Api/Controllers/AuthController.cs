using CoffeeSchedule.Application.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoffeeSchedule.Api.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/v1/auth")]
public sealed class AuthController(AuthService authService) : ControllerBase
{
    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        var response = await authService.LoginAsync(request, cancellationToken);
        return Ok(response);
    }

    [HttpPost("bootstrap")]
    public async Task<ActionResult<AuthResponse>> BootstrapManager(
        [FromBody] BootstrapManagerRequest request,
        CancellationToken cancellationToken)
    {
        var response = await authService.BootstrapManagerAsync(request, cancellationToken);
        return Created("/api/v1/auth/login", response);
    }
}
