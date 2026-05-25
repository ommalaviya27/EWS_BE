using Shared.EWS.Entities;
using Shared.EWS.Interfaces.Repositories;

namespace Domain.EWS.Interface
{
    public interface IProfileRepository : IGenericRepository<User>
    {
        Task<User?> GetUserByIdAsync(int userId);
        Task<string?> GetRoleNameAsync(int roleId);
        Task<bool> EmailTakenAsync(string email, int excludeUserId);
        Task<List<UserToken>> GetActiveTokensByUserAsync(int userId);
        Task UpdateProfileAsync(User user);
        Task ChangePasswordAsync(User user, List<UserToken> activeTokens);
    }
}
