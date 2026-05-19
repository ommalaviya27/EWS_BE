using Application.EWS.Interfaces;
using Application.EWS.Services;
using Shared.EWS.Extensions;

namespace Api.EWS.Extensions
{
    public static class Configuration
    {
        public static void RegisterServices(this IServiceCollection services)
        {
            services.RegisterSharedServices();
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<ITokenService, TokenService>();
            services.AddScoped<IEmailService, EmailService>();
            services.AddScoped<IProjectService, ProjectService>();
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<ITaskService, TaskService>();
            services.AddScoped<IMyTaskService, MyTaskService>();
        }

        public static void RegisterRepositories(this IServiceCollection services)
        {
            services.RegisterSharedRepositories();
        }
    }
}
