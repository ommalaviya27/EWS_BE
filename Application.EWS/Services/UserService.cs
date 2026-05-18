using Application.EWS.Interfaces;
using Domain.EWS.DataModels.Request.User;
using Domain.EWS.DataModels.Response.User;
using Microsoft.EntityFrameworkCore;
using Shared.EWS.Data;
using Shared.EWS.Entities;
using Shared.EWS.Exceptions;
using Shared.EWS.Extensions;
using Shared.EWS.Interfaces.Repositories;
using Shared.EWS.Services;

namespace Application.EWS.Services
{
    public class UserService(
        IGenericRepository<User> repository,
        EWSDbContext context)
        : GenericService<User>(repository), IUserService
    {
        private readonly EWSDbContext _context = context;

        public async Task<UserPagedResponse> GetAllUsersAsync(int callerRoleId, UserPaginationRequest pagination)
        {
            ValidateAdmin(callerRoleId, "view");

            // Base query: all non-deleted users joined with role and optional team-lead
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
                        UserId = x.u.Id,
                        Name = x.u.Name,
                        Email = x.u.Email,
                        MobileNumber = x.u.MobileNumber,
                        RoleId = x.u.RoleId,
                        RoleName = x.r.Name,
                        TeamLeadId = x.u.TeamLeadId,
                        TeamLeadName = tl != null ? tl.Name : null,
                        Status = x.u.status,
                    })
                .AsNoTracking();

            // Compute summary counts (role 3 = Employee) in one round-trip via GroupBy aggregate
            var counts = await baseQuery
                .Where(u => u.RoleId == 3)
                .GroupBy(_ => 1)
                .Select(g => new
                {
                    Total = g.Count(),
                    Assigned = g.Count(u => u.TeamLeadId != null),
                    Unassigned = g.Count(u => u.TeamLeadId == null),
                })
                .FirstOrDefaultAsync();

            var summary = new UserSummary
            {
                TotalEmployees = counts?.Total ?? 0,
                AssignedCount = counts?.Assigned ?? 0,
                UnassignedCount = counts?.Unassigned ?? 0,
            };

            // Apply tab filter server-side before paginating
            var filter = (pagination.Filter ?? "all").ToLowerInvariant();
            var filteredQuery = filter switch
            {
                "assigned" => baseQuery.Where(u => u.RoleId == 3 && u.TeamLeadId != null),
                "unassigned" => baseQuery.Where(u => u.RoleId == 3 && u.TeamLeadId == null),
                _ => baseQuery, // "all": admin + team-lead + all employees
            };

            var paged = await filteredQuery.ToPagedResponseAsync(pagination);
            return UserPagedResponse.From(paged, summary);
        }

        public async Task<GetUserResponse?> GetUserByIdAsync(int id, int callerRoleId)
        {
            ValidateAdmin(callerRoleId, "view");

            var user = await _context.Users
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
                        UserId = x.u.Id,
                        Name = x.u.Name,
                        Email = x.u.Email,
                        MobileNumber = x.u.MobileNumber,
                        RoleId = x.u.RoleId,
                        RoleName = x.r.Name,
                        TeamLeadId = x.u.TeamLeadId,
                        TeamLeadName = tl != null ? tl.Name : null,
                        Status = x.u.status,
                    })
                .AsNoTracking()
                .FirstOrDefaultAsync()
                ?? throw new NotFoundException($"User with id '{id}' was not found.");

            return user;
        }

        public async Task<GetUserResponse> CreateUserAsync(CreateUserRequest request, int callerRoleId)
        {
            ValidateAdmin(callerRoleId, "create");

            await ValidateRoleAsync(request.RoleId);

            if (request.TeamLeadId.HasValue)
                await ValidateTeamLeadAsync(request.TeamLeadId.Value);

            bool emailExists = await _context.Users
                .AnyAsync(u => u.Email == request.Email && !u.IsDeleted);

            if (emailExists)
                throw new DuplicateRecordException($"A user with email '{request.Email}' already exists.");

            var entity = new User
            {
                Name = request.Name.Trim(),
                Email = request.Email.Trim().ToLower(),
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                MobileNumber = request.MobileNumber.Trim(),
                RoleId = request.RoleId,
                TeamLeadId = request.TeamLeadId,
                status = request.Status
            };

            var created = await AddAsync(entity);

            var role = await _context.Roles.FindAsync(created.RoleId);
            string? teamLeadName = null;
            if (created.TeamLeadId.HasValue)
            {
                var tl = await _context.Users.FindAsync(created.TeamLeadId.Value);
                teamLeadName = tl?.Name;
            }
            return MapToResponse(created, role?.Name ?? string.Empty, teamLeadName);
        }

        public async Task<GetUserResponse> UpdateUserAsync(int id, UpdateUserRequest request, int callerRoleId)
        {
            ValidateAdmin(callerRoleId, "update");

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == id && !u.IsDeleted)
                ?? throw new NotFoundException($"User with id '{id}' was not found.");

            await ValidateRoleAsync(request.RoleId);

            if (request.TeamLeadId.HasValue)
                await ValidateTeamLeadAsync(request.TeamLeadId.Value);

            bool emailTaken = await _context.Users
                .AnyAsync(u => u.Email == request.Email && !u.IsDeleted && u.Id != id);

            if (emailTaken)
                throw new DuplicateRecordException($"Email '{request.Email}' is already in use by another user.");

            user.Name = request.Name.Trim();
            user.Email = request.Email.Trim().ToLower();
            user.MobileNumber = request.MobileNumber.Trim();
            user.RoleId = request.RoleId;
            user.TeamLeadId = request.TeamLeadId;
            user.status = request.Status;

            var updated = await UpdateAsync(user);

            var role = await _context.Roles.FindAsync(updated.RoleId);
            string? teamLeadName = null;
            if (updated.TeamLeadId.HasValue)
            {
                var tl = await _context.Users.FindAsync(updated.TeamLeadId.Value);
                teamLeadName = tl?.Name;
            }
            return MapToResponse(updated, role?.Name ?? string.Empty, teamLeadName);
        }

        public async Task<bool> DeleteUserAsync(int id, int callerRoleId)
        {
            ValidateAdmin(callerRoleId, "delete");

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == id && !u.IsDeleted)
                ?? throw new NotFoundException($"User with id '{id}' was not found.");

            return await DeleteAsync(user.Id);
        }

        public async Task<IEnumerable<RoleResponse>> GetRolesAsync(int callerRoleId)
        {
            ValidateAdmin(callerRoleId, "view");

            return await _context.Roles
                .Select(r => new RoleResponse
                {
                    RoleId = r.Id,
                    RoleName = r.Name
                })
                .AsNoTracking()
                .ToListAsync();
        }

        private static void ValidateAdmin(int callerRoleId, string action)
        {
            if (callerRoleId != 1)
                throw new ForbiddenException($"Access denied. Only Admin can {action} users.");
        }

        private async Task ValidateRoleAsync(int roleId)
        {
            bool roleExists = await _context.Roles.AnyAsync(r => r.Id == roleId);
            if (!roleExists)
                throw new NotFoundException($"Role with id '{roleId}' was not found.");
        }

        private async Task ValidateTeamLeadAsync(int teamLeadId)
        {
            bool isTeamLead = await _context.Users
                .AnyAsync(u => u.Id == teamLeadId && u.RoleId == 2 && !u.IsDeleted);
            if (!isTeamLead)
                throw new NotFoundException($"Team Lead with id '{teamLeadId}' was not found or is not a Team Lead.");
        }

        private static GetUserResponse MapToResponse(User u, string roleName, string? teamLeadName = null) => new()
        {
            UserId = u.Id,
            Name = u.Name,
            Email = u.Email,
            MobileNumber = u.MobileNumber,
            RoleId = u.RoleId,
            RoleName = roleName,
            TeamLeadId = u.TeamLeadId,
            TeamLeadName = teamLeadName,
            Status = u.status,
        };
    }
}
