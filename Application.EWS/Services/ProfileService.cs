using Application.EWS.Interfaces;
using Domain.EWS.DataModels.Request.Profile;
using Domain.EWS.DataModels.Response.Profile;
using Microsoft.EntityFrameworkCore;
using Shared.EWS.Data;
using Shared.EWS.Exceptions;

namespace Application.EWS.Services
{
    public class ProfileService(EWSDbContext context) : IProfileService
    {
        private readonly EWSDbContext _context = context;

        public async Task<GetProfileResponse> GetProfileAsync(int userId)
        {
            var result = await _context.Users
                .Where(u => u.Id == userId && !u.IsDeleted)
                .Join(_context.Roles,
                    u => u.RoleId,
                    r => r.Id,
                    (u, r) => new GetProfileResponse
                    {
                        UserId = u.Id,
                        Name = u.Name,
                        Email = u.Email,
                        MobileNumber = u.MobileNumber,
                        RoleId = u.RoleId,
                        RoleName = r.Name,
                    })
                .AsNoTracking()
                .FirstOrDefaultAsync()
                ?? throw new NotFoundException($"User with id '{userId}' was not found.");

            return result;
        }

        public async Task<GetProfileResponse> UpdateProfileAsync(int userId, UpdateProfileRequest request)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted)
                ?? throw new NotFoundException($"User with id '{userId}' was not found.");

            bool emailTaken = await _context.Users
                .AnyAsync(u => u.Email == request.Email.Trim().ToLower() && !u.IsDeleted && u.Id != userId);

            if (emailTaken)
                throw new DuplicateRecordException($"Email '{request.Email}' is already in use by another user.");

            user.Name = request.Name.Trim();
            user.Email = request.Email.Trim().ToLower();
            user.MobileNumber = request.MobileNumber.Trim();
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            var role = await _context.Roles.FindAsync(user.RoleId);

            return new GetProfileResponse
            {
                UserId = user.Id,
                Name = user.Name,
                Email = user.Email,
                MobileNumber = user.MobileNumber,
                RoleId = user.RoleId,
                RoleName = role?.Name ?? string.Empty,
            };
        }

        public async Task ChangePasswordAsync(int userId, ChangePasswordRequest request)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted)
                ?? throw new NotFoundException($"User with id '{userId}' was not found.");

            if (!BCrypt.Net.BCrypt.Verify(request.OldPassword, user.PasswordHash))
                throw new InvalidCredentialsException("Old password is incorrect.");

            if (request.NewPassword != request.ConfirmNewPassword)
                throw new InvalidOperationException("New password and confirm password do not match.");

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
            user.UpdatedAt = DateTime.UtcNow;

            var activeTokens = await _context.UserTokens
                .Where(t => t.UserId == userId && !t.IsRevoked)
                .ToListAsync();

            foreach (var token in activeTokens)
            {
                token.IsRevoked = true;
                token.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
        }
    }
}
