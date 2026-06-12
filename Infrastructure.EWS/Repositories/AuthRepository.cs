using Domain.EWS.Interface;
using Microsoft.EntityFrameworkCore;
using Shared.EWS.Data;
using Shared.EWS.Entities;

namespace Infrastructure.EWS.Repositories
{
    public class AuthRepository(EWSDbContext context)
        : GenericRepository<User>(context), IAuthRepository
    {
        public async Task<bool> EmailExistsAsync(string email)
            => await _context.Users.AnyAsync(u => u.Email == email && !u.IsDeleted);

        public async Task<User?> GetByEmailAsync(string email)
            => await _context.Users.FirstOrDefaultAsync(u => u.Email == email && !u.IsDeleted);

        public async Task<UserToken?> GetValidRefreshTokenAsync(string refreshToken)
            => await _context.UserTokens
                .Include(t => t.User)
                .FirstOrDefaultAsync(t =>
                    t.RefreshToken == refreshToken &&
                    !t.IsRevoked &&
                    t.RefreshTokenExpiresAt > DateTime.UtcNow);

        public async Task<UserToken?> GetActiveRefreshTokenAsync(string refreshToken)
            => await _context.UserTokens
                .FirstOrDefaultAsync(t => t.RefreshToken == refreshToken && !t.IsRevoked);

        public async Task RevokeTokenAsync(UserToken token)
        {
            token.IsRevoked = true;
            token.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }

        public async Task<User?> GetUserByResetTokenAsync(string token)
            => await _context.Users.FirstOrDefaultAsync(u =>
                u.PasswordResetToken == token &&
                u.PasswordResetTokenExpiry > DateTime.UtcNow);

        public async Task<List<UserToken>> GetActiveTokensByUserAsync(int userId)
            => await _context.UserTokens
                .Where(t => t.UserId == userId && !t.IsRevoked)
                .ToListAsync();

        public async Task AddUserTokenAsync(UserToken token)
        {
            _context.UserTokens.Add(token);
            await _context.SaveChangesAsync();
        }

        public async Task RevokeAllUserTokensAsync(List<UserToken> tokens)
        {
            var now = DateTime.UtcNow;
            foreach (var token in tokens)
            {
                token.IsRevoked = true;
                token.UpdatedAt = now;
            }
            await _context.SaveChangesAsync();
        }
    }
}