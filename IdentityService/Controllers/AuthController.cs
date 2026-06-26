using Microsoft.AspNetCore.Mvc;
using PRN232.LMSSystem.IdentityService.Interfaces;
using PRN232.LMSSystem.IdentityService.Models.Request;
using PRN232.LMSSystem.IdentityService.Models.Response;
using Asp.Versioning;

namespace PRN232.LMSSystem.IdentityService.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/auth")]
[Produces("application/json", "application/xml")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("login")]
    [ProducesResponseType(typeof(ApiResponse<AuthResponse>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ApiResponse<object>.ErrorResponse("Validation failed", ModelState));

        var response = await _authService.LoginAsync(request);
        return Ok(ApiResponse<AuthResponse>.SuccessResponse(response, "Login successful"));
    }

    [HttpPost("refresh-token")]
    [ProducesResponseType(typeof(ApiResponse<AuthResponse>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ApiResponse<object>.ErrorResponse("Validation failed", ModelState));

        var response = await _authService.RefreshTokenAsync(request);
        return Ok(ApiResponse<AuthResponse>.SuccessResponse(response, "Token refreshed successfully"));
    }
}
