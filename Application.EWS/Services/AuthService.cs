using Application.EWS.Interfaces;
using Domain.EWS.DataModels.Request.Authentication;
using Domain.EWS.DataModels.Response.Authentication;
using Domain.EWS.Interface;
using Microsoft.Extensions.Configuration;
using Shared.EWS.Entities;
using Shared.EWS.Exceptions;
using System.Security.Cryptography;

namespace Application.EWS.Services
{
    public class AuthService(
        IAuthRepository authRepository,
        ITokenService tokenService,
        IConfiguration configuration,
        IEmailService emailService) : IAuthService
    {
        private readonly IAuthRepository _authRepository = authRepository;
        private readonly ITokenService _tokenService = tokenService;
        private readonly IConfiguration _configuration = configuration;
        private readonly IEmailService _emailService = emailService;

        public async Task<string> RegisterAsync(RegisterRequest request)
        {
            if (await _authRepository.EmailExistsAsync(request.Email))
                throw new DuplicateRecordException("An account with this email already exists.");

            var user = new User
            {
                Name = request.Name,
                Email = request.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                MobileNumber = request.MobileNumber,
                RoleId = 3
            };

            await _authRepository.AddAsync(user);
            return "User Registered successfully";
        }

        public async Task<AuthResponse> LoginAsync(LoginRequest request)
        {
            var user = await _authRepository.GetByEmailAsync(request.Email);

            if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
                throw new InvalidCredentialsException("Invalid email or password.");

            if (user.status == false)
                throw new AccountInactiveException("Your account is inactive. Please contact reporting person.");

            return await IssueTokensAsync(user);
        }

        public async Task<AuthResponse> RefreshTokenAsync(RefreshTokenRequest request)
        {
            var storedToken = await _authRepository.GetValidRefreshTokenAsync(request.RefreshToken)
                ?? throw new TokenException("Invalid or expired refresh token.");

            await _authRepository.RevokeTokenAsync(storedToken);

            return await IssueTokensAsync(storedToken.User);
        }

        public async Task LogoutAsync(string refreshToken)
        {
            var storedToken = await _authRepository.GetActiveRefreshTokenAsync(refreshToken);
            if (storedToken != null)
                await _authRepository.RevokeTokenAsync(storedToken);
        }

        public async Task ForgotPasswordAsync(ForgotPasswordRequest request)
        {
            var user = await _authRepository.GetByEmailAsync(request.Email);
            if (user == null) return;

            var resetToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

            user.PasswordResetToken = resetToken;
            user.PasswordResetTokenExpiry = DateTime.UtcNow.AddMinutes(15);

            await _authRepository.UpdateAsync(user);
            await _emailService.SendPasswordResetEmailAsync(user.Email, user.Name, resetToken);
        }

        public async Task ResetPasswordAsync(ResetPasswordRequest request)
        {
            var user = await _authRepository.GetUserByResetTokenAsync(request.Token)
                ?? throw new ResetTokenException("Invalid or expired password reset token.");

            if (request.NewPassword != request.ConfirmNewPassword)
                throw new InvalidOperationException("New password and confirm password do not match.");

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
            user.PasswordResetToken = null;
            user.PasswordResetTokenExpiry = null;

            var activeTokens = await _authRepository.GetActiveTokensByUserAsync(user.Id);

            await _authRepository.UpdateAsync(user);
            if (activeTokens.Count > 0)
                await _authRepository.RevokeAllUserTokensAsync(activeTokens);
        }

        private async Task<AuthResponse> IssueTokensAsync(User user)
        {
            var accessToken = _tokenService.GenerateAccessToken(user);
            var refreshToken = _tokenService.GenerateRefreshToken();

            var accessExpireMinutes = int.Parse(_configuration["Jwt:ExpireMinutes"] ?? "60");
            var refreshExpireDays = int.Parse(_configuration["Jwt:RefreshTokenExpireDays"] ?? "7");

            var accessTokenExpiresAt = DateTime.UtcNow.AddMinutes(accessExpireMinutes);
            var refreshTokenExpiresAt = DateTime.UtcNow.AddDays(refreshExpireDays);

            await _authRepository.AddUserTokenAsync(new UserToken
            {
                UserId = user.Id,
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                AccessTokenExpiresAt = accessTokenExpiresAt,
                RefreshTokenExpiresAt = refreshTokenExpiresAt
            });

            return new AuthResponse
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                AccessTokenExpiresAt = accessTokenExpiresAt,
                UserId = user.Id,
                Name = user.Name,
                Email = user.Email,
                RoleId = user.RoleId
            };
        }
    }
}