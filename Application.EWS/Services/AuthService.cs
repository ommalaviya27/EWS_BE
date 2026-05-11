using Application.EWS.Interfaces;
using Domain.EWS.DataModels.Request.Authentication;
using Domain.EWS.DataModels.Response.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Shared.EWS.Data;
using Shared.EWS.Entities;
using Shared.EWS.Exceptions;
using System.Security.Cryptography;

namespace Application.EWS.Services
{
    public class AuthService : IAuthService
    {
        private readonly EWSDbContext _context;
        private readonly ITokenService _tokenService;
        private readonly IConfiguration _configuration;
        private readonly IEmailService _emailService;

        public AuthService(EWSDbContext context, ITokenService tokenService,
            IConfiguration configuration, IEmailService emailService)
        {
            _context = context;
            _tokenService = tokenService;
            _configuration = configuration;
            _emailService = emailService;
        }

        public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
        {
            if (await _context.Users.AnyAsync(u => u.Email == request.Email))
                throw new DuplicateRecordException("An account with this email already exists.");

            var user = new User
            {
                Name = request.Name,
                Email = request.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                MobileNumber = request.MobileNumber,
                RoleId = request.RoleId
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return await IssueTokensAsync(user);
        }

        public async Task<AuthResponse> LoginAsync(LoginRequest request)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == request.Email);

            if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
                throw new InvalidCredentialsException();

            return await IssueTokensAsync(user);
        }

        public async Task<AuthResponse> RefreshTokenAsync(RefreshTokenRequest request)
        {
            var storedToken = await _context.UserTokens
                .Include(t => t.User)
                .FirstOrDefaultAsync(t =>
                    t.RefreshToken == request.RefreshToken &&
                    !t.IsRevoked &&
                    t.RefreshTokenExpiresAt > DateTime.UtcNow);

            if (storedToken == null)
                throw new TokenException("Invalid or expired refresh token.");

            storedToken.IsRevoked = true;
            storedToken.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return await IssueTokensAsync(storedToken.User);
        }

        public async Task LogoutAsync(string refreshToken)
        {
            var storedToken = await _context.UserTokens
                .FirstOrDefaultAsync(t => t.RefreshToken == refreshToken && !t.IsRevoked);

            if (storedToken != null)
            {
                storedToken.IsRevoked = true;
                storedToken.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
        }

        public async Task ForgotPasswordAsync(ForgotPasswordRequest request)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == request.Email);

            if (user == null) return;

            var resetToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

            user.PasswordResetToken = resetToken;
            user.PasswordResetTokenExpiry = DateTime.UtcNow.AddMinutes(15);
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            await _emailService.SendPasswordResetEmailAsync(user.Email, user.Name, resetToken);
        }

        public async Task ResetPasswordAsync(ResetPasswordRequest request)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u =>
                u.PasswordResetToken == request.Token &&
                u.PasswordResetTokenExpiry > DateTime.UtcNow);

            if (user == null)
                throw new ResetTokenException();

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
            user.PasswordResetToken = null;
            user.PasswordResetTokenExpiry = null;
            user.UpdatedAt = DateTime.UtcNow;

            var activeTokens = await _context.UserTokens
                .Where(t => t.UserId == user.Id && !t.IsRevoked)
                .ToListAsync();

            foreach (var token in activeTokens)
            {
                token.IsRevoked = true;
                token.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
        }

        private async Task<AuthResponse> IssueTokensAsync(User user)
        {
            var accessToken  = _tokenService.GenerateAccessToken(user);
            var refreshToken = _tokenService.GenerateRefreshToken();

            var accessExpireMinutes  = int.Parse(_configuration["Jwt:ExpireMinutes"] ?? "60");
            var refreshExpireDays    = int.Parse(_configuration["Jwt:RefreshTokenExpireDays"] ?? "7");

            var accessTokenExpiresAt  = DateTime.UtcNow.AddMinutes(accessExpireMinutes);
            var refreshTokenExpiresAt = DateTime.UtcNow.AddDays(refreshExpireDays);

            _context.UserTokens.Add(new UserToken
            {
                UserId = user.Id,
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                AccessTokenExpiresAt = accessTokenExpiresAt,
                RefreshTokenExpiresAt = refreshTokenExpiresAt
            });

            await _context.SaveChangesAsync();

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