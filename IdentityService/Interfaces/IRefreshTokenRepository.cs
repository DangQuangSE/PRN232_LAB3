using PRN232.LMSSystem.IdentityService.Entities;

namespace PRN232.LMSSystem.IdentityService.Interfaces;

public interface IRefreshTokenRepository : IGenericRepository<RefreshToken>
{
    Task<RefreshToken?> GetByTokenAsync(string token);
}
