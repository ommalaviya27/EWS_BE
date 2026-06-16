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

            await ValidateRoleAsync(request.RoleId);

            if (request.TeamLeadId.HasValue)
                await ValidateTeamLeadAsync(request.TeamLeadId.Value);

            if (await _userRepository.EmailExistsAsync(request.Email))
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

            var roleName = await _userRepository.GetRoleNameAsync(created.RoleId);
            string? teamLeadName = created.TeamLeadId.HasValue
                ? await _userRepository.GetTeamLeadNameAsync(created.TeamLeadId.Value)
                : null;

            var response = _mapper.Map<GetUserResponse>(created);
            response.RoleName = roleName ?? string.Empty;
            response.TeamLeadName = teamLeadName;
            return response;
        }

        public async Task<GetUserResponse> UpdateUserAsync(int id, UpdateUserRequest request)
        {
            ValidateAdmin("update");

            var user = await GetByIdAsync(id)
                ?? throw new NotFoundException($"User with id '{id}' was not found.");

            await ValidateRoleAsync(request.RoleId);

            if (request.TeamLeadId.HasValue)
                await ValidateTeamLeadAsync(request.TeamLeadId.Value);

            if (await _userRepository.EmailExistsAsync(request.Email, id))
                throw new DuplicateRecordException($"Email '{request.Email}' is already in use by another user.");

            user.Name = request.Name.Trim();
            user.Email = request.Email.Trim().ToLower();
            user.MobileNumber = request.MobileNumber.Trim();
            user.RoleId = request.RoleId;
            user.TeamLeadId = request.TeamLeadId;
            user.status = request.Status;

            var updated = await UpdateAsync(user);

            var roleName = await _userRepository.GetRoleNameAsync(updated.RoleId);
            string? teamLeadName = updated.TeamLeadId.HasValue
                ? await _userRepository.GetTeamLeadNameAsync(updated.TeamLeadId.Value)
                : null;

            var response = _mapper.Map<GetUserResponse>(updated);
            response.RoleName = roleName ?? string.Empty;
            response.TeamLeadName = teamLeadName;
            return response;
        }

        public async Task<bool> DeleteUserAsync(int id)
        {
            ValidateAdmin("delete");

            var user = await GetByIdAsync(id)
                ?? throw new NotFoundException($"User with id '{id}' was not found.");

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

        private async Task ValidateTeamLeadAsync(int teamLeadId)
        {
            if (!await _userRepository.TeamLeadExistsAsync(teamLeadId))
                throw new NotFoundException($"Team Lead with id '{teamLeadId}' was not found or is not a Team Lead.");
        }
    }
}