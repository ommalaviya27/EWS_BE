using Domain.EWS.DataModels.Response.Profile;
using Domain.EWS.Interface;
using Microsoft.EntityFrameworkCore;
using Shared.EWS.Data;
using Shared.EWS.Entities;

namespace Infrastructure.EWS.Repositories
{
    public class ProfileRepository(EWSDbContext context)
        : GenericRepository<User>(context), IProfileRepository
    {
        public async Task<GetProfileResponse?> GetProfileByIdAsync(int userId)
            => await _context.Users
                .Where(u => u.Id == userId && !u.IsDeleted)
                .Join(_context.Roles,
                    u => u.RoleId,
                    r => r.Id,
                    (u, r) => new { User = u, RoleName = r.Name })
                .GroupJoin(_context.Users,
                    ur => ur.User.ReportingId,
                    tl => tl.Id,
                    (ur, tls) => new { ur.User, ur.RoleName, TeamLeads = tls })
                .SelectMany(
                    x => x.TeamLeads.DefaultIfEmpty(),
                    (x, tl) => new GetProfileResponse
                    {
                        UserId = x.User.Id,
                        Name = x.User.Name,
                        Email = x.User.Email,
                        MobileNumber = x.User.MobileNumber,
                        RoleId = x.User.RoleId,
                        RoleName = x.RoleName,
                        ReportingPersonName = tl != null ? tl.Name : null,
                    })
                .AsNoTracking()
                .FirstOrDefaultAsync();

        public async Task<string?> GetRoleNameAsync(int roleId)
            => await _context.Roles
                .Where(r => r.Id == roleId)
                .Select(r => r.Name)
                .AsNoTracking()
                .FirstOrDefaultAsync();

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