using Microsoft.EntityFrameworkCore;
using PRN232.LMSSystem.IdentityService.Data;
using PRN232.LMSSystem.IdentityService.Entities;
using PRN232.LMSSystem.IdentityService.Interfaces;

namespace PRN232.LMSSystem.IdentityService.Repositories;

public class RefreshTokenRepository : GenericRepository<RefreshToken>, IRefreshTokenRepository
{
    public RefreshTokenRepository(IdentityDbContext context) : base(context)
    {
    }

    public async Task<RefreshToken?> GetByTokenAsync(string token)
    {
        return await _dbSet
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.Token == token);
    }
}
