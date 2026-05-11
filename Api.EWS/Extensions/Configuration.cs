using Application.EWS.Interfaces;
using Application.EWS.Services;

namespace Api.EWS.Extensions
{
    public static class Configuration
    {
        public static void RegisterServices(this IServiceCollection services)
        {
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<ITokenService, TokenService>();
            services.AddScoped<IEmailService, EmailService>();
        }
    }
}
