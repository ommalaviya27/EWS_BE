using Domain.EWS.DataModels.Request.User;
using Domain.EWS.DataModels.Response.User;
using Domain.EWS.Interface;
using Microsoft.EntityFrameworkCore;
using Shared.EWS.Data;
using Shared.EWS.Entities;
using Shared.EWS.Extensions;

namespace Infrastructure.EWS.Repositories
{
    public class UserRepository(EWSDbContext context)
        : GenericRepository<User>(context), IUserRepository
    {
        public async Task<UserPagedResponse> GetAllUsersAsync(UserPaginationRequest pagination)
        {
            var baseQuery = _context.Users
                .Where(u => !u.IsDeleted)
                .Join(_context.Roles,
                    u => u.RoleId,
                    r => r.Id,
                    (u, r) => new { u, r })
                .GroupJoin(_context.Users.Where(tl => !tl.IsDeleted),
                    ur => ur.u.TeamLeadId,
                    tl => (int?)tl.Id,
                    (ur, tls) => new { ur.u, ur.r, tls })
                .SelectMany(
                    x => x.tls.DefaultIfEmpty(),
                    (x, tl) => new GetUserResponse
                    {
                        UserId       = x.u.Id,
                        Name         = x.u.Name,
                        Email        = x.u.Email,
                        MobileNumber = x.u.MobileNumber,
                        RoleId       = x.u.RoleId,
                        RoleName     = x.r.Name,
                        TeamLeadId   = x.u.TeamLeadId,
                        TeamLeadName = tl != null ? tl.Name : null,
                        Status       = x.u.status,
                    })
                .AsNoTracking();

            var search = pagination.Search?.Trim();
            if (!string.IsNullOrEmpty(search))
                baseQuery = baseQuery.Where(u => EF.Functions.ILike(u.Name, $"%{search}%"));

            if (pagination.RoleId.HasValue)
                baseQuery = baseQuery.Where(u => u.RoleId == pagination.RoleId.Value);

            if (pagination.Status.HasValue)
                baseQuery = baseQuery.Where(u => u.Status == pagination.Status.Value);

            var counts = await baseQuery
                .Where(u => u.RoleId == 3)
                .GroupBy(_ => 1)
                .Select(g => new
                {
                    Assigned   = g.Count(u => u.TeamLeadId != null),
                    Unassigned = g.Count(u => u.TeamLeadId == null),
                })
                .FirstOrDefaultAsync();

            var summary = new UserSummary
            {
                AssignedCount   = counts?.Assigned   ?? 0,
                UnassignedCount = counts?.Unassigned ?? 0,
            };

            var filter = (pagination.Filter ?? "all").ToLowerInvariant();
            var filteredQuery = filter switch
            {
                "assigned"   => baseQuery.Where(u => u.RoleId == 3 && u.TeamLeadId != null),
                "unassigned" => baseQuery.Where(u => u.RoleId == 3 && u.TeamLeadId == null),
                _            => baseQuery,
            };

            var paged = await filteredQuery.ToPagedResponseAsync(pagination);
            return UserPagedResponse.From(paged, summary);
        }

        public async Task<GetUserResponse?> GetUserByIdWithDetailsAsync(int id)
        {
            return await _context.Users
                .Where(u => u.Id == id && !u.IsDeleted)
                .Join(_context.Roles,
                    u => u.RoleId,
                    r => r.Id,
                    (u, r) => new { u, r })
                .GroupJoin(_context.Users.Where(tl => !tl.IsDeleted),
                    ur => ur.u.TeamLeadId,
                    tl => (int?)tl.Id,
                    (ur, tls) => new { ur.u, ur.r, tls })
                .SelectMany(
                    x => x.tls.DefaultIfEmpty(),
                    (x, tl) => new GetUserResponse
                    {
                        UserId       = x.u.Id,
                        Name         = x.u.Name,
                        Email        = x.u.Email,
                        MobileNumber = x.u.MobileNumber,
                        RoleId       = x.u.RoleId,
                        RoleName     = x.r.Name,
                        TeamLeadId   = x.u.TeamLeadId,
                        TeamLeadName = tl != null ? tl.Name : null,
                        Status       = x.u.status,
                    })
                .AsNoTracking()
                .FirstOrDefaultAsync();
        }

        public async Task<bool> EmailExistsAsync(string email, int? excludeId = null)
        {
            var query = _context.Users.Where(u => u.Email == email && !u.IsDeleted);
            if (excludeId.HasValue)
                query = query.Where(u => u.Id != excludeId.Value);
            return await query.AnyAsync();
        }

        public async Task<bool> RoleExistsAsync(int roleId)
            => await _context.Roles.AnyAsync(r => r.Id == roleId);

        public async Task<bool> TeamLeadExistsAsync(int teamLeadId)
            => await _context.Users.AnyAsync(u => u.Id == teamLeadId && u.RoleId == 2 && !u.IsDeleted);

        public async Task<User?> GetByEmailAsync(string email)
            => await _context.Users.FirstOrDefaultAsync(u => u.Email == email && !u.IsDeleted);

        public async Task<IEnumerable<RoleResponse>> GetRolesAsync()
        {
            return await _context.Roles
                .AsNoTracking()
                .Select(r => new RoleResponse { RoleId = r.Id, RoleName = r.Name })
                .ToListAsync();
        }

        public async Task<string?> GetRoleNameAsync(int roleId)
            => await _context.Roles
                .Where(r => r.Id == roleId)
                .Select(r => r.Name)
                .AsNoTracking()
                .FirstOrDefaultAsync();

        public async Task<string?> GetTeamLeadNameAsync(int teamLeadId)
            => await _context.Users
                .Where(u => u.Id == teamLeadId && !u.IsDeleted)
                .Select(u => u.Name)
                .AsNoTracking()
                .FirstOrDefaultAsync();
    }
}