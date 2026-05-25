using Domain.EWS.Interface;
using Microsoft.EntityFrameworkCore;
using Shared.EWS.Data;
using Shared.EWS.Entities;

namespace Infrastructure.EWS.Repositories
{
    public class ProfileRepository(EWSDbContext context)
        : GenericRepository<User>(context), IProfileRepository
    {
        public async Task<User?> GetUserByIdAsync(int userId)
            => await _context.Users
                .Where(u => u.Id == userId && !u.IsDeleted)
                .AsNoTracking()
                .FirstOrDefaultAsync();

        public async Task<string?> GetRoleNameAsync(int roleId)
        {
            var role = await _context.Roles.FindAsync(roleId);
            return role?.Name;
        }

        public async Task<bool> EmailTakenAsync(string email, int excludeUserId)
            => await _context.Users
                .AnyAsync(u => u.Email == email.Trim().ToLower()
                            && !u.IsDeleted
                            && u.Id != excludeUserId);

        public async Task<List<UserToken>> GetActiveTokensByUserAsync(int userId)
            => await _context.UserTokens
                .Where(t => t.UserId == userId && !t.IsRevoked)
                .ToListAsync();

        public async Task UpdateProfileAsync(User user)
        {
            user.UpdatedAt = DateTime.UtcNow;
            _context.Users.Update(user);
            await _context.SaveChangesAsync();
        }

        public async Task ChangePasswordAsync(User user, List<UserToken> activeTokens)
        {
            var now = DateTime.UtcNow;
            user.UpdatedAt = now;
            _context.Users.Update(user);

            foreach (var token in activeTokens)
            {
                token.IsRevoked = true;
                token.UpdatedAt = now;
            }

            await _context.SaveChangesAsync();
        }
    }
}
