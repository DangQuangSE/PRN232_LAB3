using Microsoft.EntityFrameworkCore;
using PRN232.LMSSystem.IdentityService.Data;
using PRN232.LMSSystem.IdentityService.Entities;
using PRN232.LMSSystem.IdentityService.Interfaces;

namespace PRN232.LMSSystem.IdentityService.Repositories;

public class UserRepository : GenericRepository<User>, IUserRepository
{
    public UserRepository(IdentityDbContext context) : base(context)
    {
    }

    public async Task<User?> GetByUsernameAsync(string username)
    {
        return await _dbSet
            .Include(u => u.RefreshTokens)
            .FirstOrDefaultAsync(u => u.Username.ToLower() == username.ToLower());
    }
}
