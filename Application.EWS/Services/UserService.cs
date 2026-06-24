using AutoMapper;
using Application.EWS.Interfaces;
using Domain.EWS.DataModels.Request.User;
using Domain.EWS.DataModels.Response.User;
using Domain.EWS.Interface;
using Shared.EWS.Entities;
using Shared.EWS.Exceptions;
using Shared.EWS.Services;
using System.Security.Claims;

namespace Application.EWS.Services
{
    public class UserService(
        IUserRepository repository,
        IMapper mapper,
        ClaimsPrincipal principal)
        : GenericService<User>(repository, principal), IUserService
    {
        private readonly IUserRepository _userRepository = repository;
        private readonly IMapper _mapper = mapper;

        public async Task<UserPagedResponse> GetAllUsersAsync(UserPaginationRequest pagination)
        {
            ValidateAdmin("view");
            return await _userRepository.GetAllUsersAsync(pagination);
        }

        public async Task<GetUserResponse?> GetUserByIdAsync(int id)
        {
            ValidateAdmin("view");
            return await _userRepository.GetUserByIdWithDetailsAsync(id)
                ?? throw new NotFoundException($"User with id '{id}' was not found.");
        }

        public async Task<GetUserResponse> CreateUserAsync(CreateUserRequest request)
        {
            ValidateAdmin("create");

            if (request.RoleId == 1)
                throw new ForbiddenException("Admin users cannot be created. Only one Admin is allowed in the system.");

            await ValidateRoleAsync(request.RoleId);

            int? resolvedReportingId = request.RoleId switch
            {
                3 => request.ReportingId,
                2 => CurrentUserId,
                _ => null
            };

            if (request.RoleId == 3 && resolvedReportingId.HasValue)
                await ValidateReportingAsync(resolvedReportingId.Value);

            if (await _userRepository.EmailExistsAsync(request.Email))
                throw new DuplicateRecordException($"A user with email '{request.Email}' already exists.");

            var entity = new User
            {
                Name = request.Name.Trim(),
                Email = request.Email.Trim().ToLower(),
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                MobileNumber = request.MobileNumber.Trim(),
                RoleId = request.RoleId,
                ReportingId = resolvedReportingId,
                status = request.Status
            };

            var created = await AddAsync(entity);

            var roleName = await _userRepository.GetRoleNameAsync(created.RoleId);
            string? reportingName = created.ReportingId.HasValue
                ? await _userRepository.GetReportingNameAsync(created.ReportingId.Value)
                : null;

            var response = _mapper.Map<GetUserResponse>(created);
            response.RoleName = roleName ?? string.Empty;
            response.ReportingName = reportingName;
            return response;
        }

        public async Task<GetUserResponse> UpdateUserAsync(int id, UpdateUserRequest request)
        {
            ValidateAdmin("update");

            var user = await GetByIdAsync(id)
                ?? throw new NotFoundException($"User with id '{id}' was not found.");

            if (request.RoleId == 1)
                throw new ForbiddenException("Cannot assign Admin role to a user. Only one Admin is allowed in the system.");

            if (user.RoleId == 1)
                throw new ForbiddenException("The Admin user cannot be modified through this operation.");

            await ValidateRoleAsync(request.RoleId);

            int? resolvedReportingId = request.RoleId switch
            {
                3 => request.ReportingId,
                2 => request.ReportingId ?? user.ReportingId,
                _ => null
            };

            if (request.RoleId == 3 && resolvedReportingId.HasValue)
                await ValidateReportingAsync(resolvedReportingId.Value);

            if (request.RoleId == 2 && resolvedReportingId.HasValue)
                await ValidateAdminAsync(resolvedReportingId.Value);

            if (await _userRepository.EmailExistsAsync(request.Email, id))
                throw new DuplicateRecordException($"Email '{request.Email}' is already in use by another user.");

            user.Name = request.Name.Trim();
            user.Email = request.Email.Trim().ToLower();
            user.MobileNumber = request.MobileNumber.Trim();
            user.RoleId = request.RoleId;
            user.ReportingId = resolvedReportingId;
            user.status = request.Status;

            var updated = await UpdateAsync(user);

            var roleName = await _userRepository.GetRoleNameAsync(updated.RoleId);
            string? reportingName = updated.ReportingId.HasValue
                ? await _userRepository.GetReportingNameAsync(updated.ReportingId.Value)
                : null;

            var response = _mapper.Map<GetUserResponse>(updated);
            response.RoleName = roleName ?? string.Empty;
            response.ReportingName = reportingName;
            return response;
        }

        public async Task<bool> DeleteUserAsync(int id)
        {
            ValidateAdmin("delete");

            var user = await GetByIdAsync(id)
                ?? throw new NotFoundException($"User with id '{id}' was not found.");

            if (user.RoleId == 1)
                throw new ForbiddenException("The Admin user cannot be deleted.");

            return await DeleteAsync(user.Id);
        }

        public async Task<IEnumerable<RoleResponse>> GetRolesAsync()
        {
            ValidateAdmin("view");
            return await _userRepository.GetRolesAsync();
        }

        private void ValidateAdmin(string action)
        {
            if (CurrentRoleId != 1)
                throw new ForbiddenException($"Access denied. Only Admin can {action} users.");
        }

        private async Task ValidateRoleAsync(int roleId)
        {
            if (!await _userRepository.RoleExistsAsync(roleId))
                throw new NotFoundException($"Role with id '{roleId}' was not found.");
        }

        private async Task ValidateReportingAsync(int reportingId)
        {
            if (!await _userRepository.ReportingExistsAsync(reportingId))
                throw new NotFoundException($"Reporting manager with id '{reportingId}' was not found or is not a Team Lead.");
        }

        private async Task ValidateAdminAsync(int adminId)
        {
            if (!await _userRepository.AdminExistsAsync(adminId))
                throw new NotFoundException($"Admin with id '{adminId}' was not found or is not an Admin.");
        }
    }
}