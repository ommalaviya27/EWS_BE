using AutoMapper;
using Application.EWS.Interfaces;
using Domain.EWS.DataModels.Request.Profile;
using Domain.EWS.DataModels.Response.Profile;
using Domain.EWS.Interface;
using Shared.EWS.Exceptions;

namespace Application.EWS.Services
{
    public class ProfileService(
        IProfileRepository profileRepository,
        IMapper mapper) : IProfileService
    {
        private readonly IProfileRepository _profileRepository = profileRepository;
        private readonly IMapper _mapper = mapper;

        public async Task<GetProfileResponse> GetProfileAsync(int userId)
        {
            var user = await _profileRepository.GetUserByIdAsync(userId)
                ?? throw new NotFoundException($"User with id '{userId}' was not found.");

            var roleName = await _profileRepository.GetRoleNameAsync(user.RoleId);
            var response = _mapper.Map<GetProfileResponse>(user);
            response.RoleName = roleName ?? string.Empty;
            return response;
        }

        public async Task<GetProfileResponse> UpdateProfileAsync(int userId, UpdateProfileRequest request)
        {
            var user = await _profileRepository.GetByIdAsync(userId)
                ?? throw new NotFoundException($"User with id '{userId}' was not found.");

            if (await _profileRepository.EmailTakenAsync(request.Email, userId))
                throw new DuplicateRecordException($"Email '{request.Email}' is already in use by another user.");

            user.Name         = request.Name.Trim();
            user.Email        = request.Email.Trim().ToLower();
            user.MobileNumber = request.MobileNumber.Trim();

            await _profileRepository.UpdateProfileAsync(user);

            var roleName = await _profileRepository.GetRoleNameAsync(user.RoleId);
            var response = _mapper.Map<GetProfileResponse>(user);
            response.RoleName = roleName ?? string.Empty;
            return response;
        }

        public async Task ChangePasswordAsync(int userId, ChangePasswordRequest request)
        {
            var user = await _profileRepository.GetByIdAsync(userId)
                ?? throw new NotFoundException($"User with id '{userId}' was not found.");

            if (!BCrypt.Net.BCrypt.Verify(request.OldPassword, user.PasswordHash))
                throw new InvalidCredentialsException("Old password is incorrect.");

            if (request.NewPassword != request.ConfirmNewPassword)
                throw new InvalidOperationException("New password and confirm password do not match.");

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);

            var activeTokens = await _profileRepository.GetActiveTokensByUserAsync(userId);

            // Single DB round-trip: password update + token revocations together
            await _profileRepository.ChangePasswordAsync(user, activeTokens);
        }
    }
}
