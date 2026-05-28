using AutoMapper;
using Application.EWS.Interfaces;
using Domain.EWS.DataModels.Request.Profile;
using Domain.EWS.DataModels.Response.Profile;
using Domain.EWS.Interface;
using Shared.EWS.Entities;
using Shared.EWS.Exceptions;
using Shared.EWS.Services;
using System.Security.Claims;

namespace Application.EWS.Services
{
    public class ProfileService(
        IProfileRepository profileRepository,
        IMapper mapper,
        ClaimsPrincipal principal)
        : GenericService<User>(profileRepository, principal), IProfileService
    {
        private readonly IMapper _mapper = mapper;

        private IProfileRepository ProfileRepository => (IProfileRepository)_repository;

        public async Task<GetProfileResponse> GetProfileAsync()
        {
            var user = await ProfileRepository.GetUserByIdAsync(CurrentUserId)
                ?? throw new NotFoundException($"User with id '{CurrentUserId}' was not found.");

            var roleName = await ProfileRepository.GetRoleNameAsync(user.RoleId);
            var response = _mapper.Map<GetProfileResponse>(user);
            response.RoleName = roleName ?? string.Empty;
            return response;
        }

        public async Task<GetProfileResponse> UpdateProfileAsync(UpdateProfileRequest request)
        {
            var user = await GetByIdAsync(CurrentUserId)
                ?? throw new NotFoundException($"User with id '{CurrentUserId}' was not found.");

            if (await ProfileRepository.EmailTakenAsync(request.Email, CurrentUserId))
                throw new DuplicateRecordException($"Email '{request.Email}' is already in use by another user.");

            user.Name         = request.Name.Trim();
            user.Email        = request.Email.Trim().ToLower();
            user.MobileNumber = request.MobileNumber.Trim();

            await UpdateAsync(user);

            var roleName = await ProfileRepository.GetRoleNameAsync(user.RoleId);
            var response = _mapper.Map<GetProfileResponse>(user);
            response.RoleName = roleName ?? string.Empty;
            return response;
        }

        public async Task ChangePasswordAsync(ChangePasswordRequest request)
        {
            var user = await GetByIdAsync(CurrentUserId)
                ?? throw new NotFoundException($"User with id '{CurrentUserId}' was not found.");

            if (!BCrypt.Net.BCrypt.Verify(request.OldPassword, user.PasswordHash))
                throw new InvalidCredentialsException("Old password is incorrect.");

            if (request.NewPassword != request.ConfirmNewPassword)
                throw new InvalidOperationException("New password and confirm password do not match.");

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);

            var activeTokens = await ProfileRepository.GetActiveTokensByUserAsync(CurrentUserId);
            await ProfileRepository.ChangePasswordAsync(user, activeTokens);
        }
    }
}