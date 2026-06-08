using Domain.EWS.DataModels.Response.Profile;
using Shared.EWS.Entities;
using Shared.EWS.Interfaces.Repositories;

namespace Domain.EWS.Interface
{
    public interface IProfileRepository : IGenericRepository<User>
    {
        Task<GetProfileResponse?> GetProfileByIdAsync(int userId);
        Task<bool> EmailTakenAsync(string email, int excludeUserId);
        Task<List<UserToken>> GetActiveTokensByUserAsync(int userId);
        Task UpdateProfileAsync(User user);
        Task ChangePasswordAsync(User user, List<UserToken> activeTokens);
        Task<string?> GetRoleNameAsync(int roleId);
    }
}