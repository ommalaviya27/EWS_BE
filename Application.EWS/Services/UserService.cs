using Application.EWS.Interfaces;
using Domain.EWS.DataModels.Request.User;
using Domain.EWS.DataModels.Response.User;
using Microsoft.EntityFrameworkCore;
using Shared.EWS.Data;
using Shared.EWS.DataModel.Request;
using Shared.EWS.DataModel.Response;
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

        public async Task<PagedResponse<GetUserResponse>> GetAllUsersAsync(int callerRoleId, PaginationRequest pagination)
        {
            ValidateAdmin(callerRoleId, "view");

            var query = _context.Users
                .Where(u => !u.IsDeleted)
                .Join(_context.Roles,
                    u => u.RoleId,
                    r => r.Id,
                    (u, r) => new GetUserResponse
                    {
                        UserId       = u.Id,
                        Name         = u.Name,
                        Email        = u.Email,
                        MobileNumber = u.MobileNumber,
                        RoleId       = u.RoleId,
                        RoleName     = r.Name,
                        Status       = u.status,
                        CreatedAt    = u.CreatedAt
                    })
                .AsNoTracking();

            return await query.ToPagedResponseAsync(pagination);
        }

        public async Task<GetUserResponse?> GetUserByIdAsync(int id, int callerRoleId)
        {
            ValidateAdmin(callerRoleId, "view");

            var user = await _context.Users
                .Where(u => u.Id == id && !u.IsDeleted)
                .Join(_context.Roles,
                    u => u.RoleId,
                    r => r.Id,
                    (u, r) => new GetUserResponse
                    {
                        UserId       = u.Id,
                        Name         = u.Name,
                        Email        = u.Email,
                        MobileNumber = u.MobileNumber,
                        RoleId       = u.RoleId,
                        RoleName     = r.Name,
                        Status       = u.status,
                        CreatedAt    = u.CreatedAt
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

            bool emailExists = await _context.Users
                .AnyAsync(u => u.Email == request.Email && !u.IsDeleted);

            if (emailExists)
                throw new DuplicateRecordException($"A user with email '{request.Email}' already exists.");

            var entity = new User
            {
                Name         = request.Name.Trim(),
                Email        = request.Email.Trim().ToLower(),
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                MobileNumber = request.MobileNumber.Trim(),
                RoleId       = request.RoleId,
                status       = request.Status
            };

            var created = await AddAsync(entity);

            var role = await _context.Roles.FindAsync(created.RoleId);
            return MapToResponse(created, role?.Name ?? string.Empty);
        }

        public async Task<GetUserResponse> UpdateUserAsync(int id, UpdateUserRequest request, int callerRoleId)
        {
            ValidateAdmin(callerRoleId, "update");

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == id && !u.IsDeleted)
                ?? throw new NotFoundException($"User with id '{id}' was not found.");

            await ValidateRoleAsync(request.RoleId);

            bool emailTaken = await _context.Users
                .AnyAsync(u => u.Email == request.Email && !u.IsDeleted && u.Id != id);

            if (emailTaken)
                throw new DuplicateRecordException($"Email '{request.Email}' is already in use by another user.");

            user.Name         = request.Name.Trim();
            user.Email        = request.Email.Trim().ToLower();
            user.MobileNumber = request.MobileNumber.Trim();
            user.RoleId       = request.RoleId;
            user.status       = request.Status;

            var updated = await UpdateAsync(user);

            var role = await _context.Roles.FindAsync(updated.RoleId);
            return MapToResponse(updated, role?.Name ?? string.Empty);
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
                    RoleId   = r.Id,
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

        private static GetUserResponse MapToResponse(User u, string roleName) => new()
        {
            UserId       = u.Id,
            Name         = u.Name,
            Email        = u.Email,
            MobileNumber = u.MobileNumber,
            RoleId       = u.RoleId,
            RoleName     = roleName,
            Status       = u.status,
            CreatedAt    = u.CreatedAt
        };
    }
}