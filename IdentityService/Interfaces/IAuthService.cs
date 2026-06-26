using PRN232.LMSSystem.IdentityService.Models.Request;
using PRN232.LMSSystem.IdentityService.Models.Response;

namespace PRN232.LMSSystem.IdentityService.Interfaces;

public interface IAuthService
{
    Task<AuthResponse> LoginAsync(LoginRequest request);
    Task<AuthResponse> RefreshTokenAsync(RefreshTokenRequest request);
}
