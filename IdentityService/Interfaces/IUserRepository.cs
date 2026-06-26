using PRN232.LMSSystem.IdentityService.Entities;

namespace PRN232.LMSSystem.IdentityService.Interfaces;

public interface IUserRepository : IGenericRepository<User>
{
    Task<User?> GetByUsernameAsync(string username);
}
