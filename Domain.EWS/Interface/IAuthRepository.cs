using Shared.EWS.Entities;
using Shared.EWS.Interfaces.Repositories;

namespace Domain.EWS.Interface
{
    public interface IAuthRepository : IGenericRepository<User>
    {
        Task<bool> EmailExistsAsync(string email);
        Task<User?> GetByEmailAsync(string email);
        Task<UserToken?> GetValidRefreshTokenAsync(string refreshToken);
        Task<UserToken?> GetActiveRefreshTokenAsync(string refreshToken);
        Task RevokeTokenAsync(UserToken token);
        Task<User?> GetUserByResetTokenAsync(string token);
        Task<List<UserToken>> GetActiveTokensByUserAsync(int userId);
        Task AddUserTokenAsync(UserToken token);
        Task RevokeAllUserTokensAsync(List<UserToken> tokens);
    }
}