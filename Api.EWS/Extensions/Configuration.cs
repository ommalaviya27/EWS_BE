using Application.EWS.Interfaces;
using Application.EWS.Services;
using Domain.EWS.Interface;
using Infrastructure.EWS.Repositories;
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
            services.AddScoped<IProfileService, ProfileService>();
            services.AddScoped<IAdminDashboardService, AdminDashboardService>();
        }

        public static void RegisterRepositories(this IServiceCollection services)
        {
            services.RegisterSharedRepositories();
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IProjectRepository, ProjectRepository>();
            services.AddScoped<ITaskRepository, TaskRepository>();
            services.AddScoped<IMyTaskRepository, MyTaskRepository>();
            services.AddScoped<IAuthRepository, AuthRepository>();
            services.AddScoped<IProfileRepository, ProfileRepository>();
            services.AddScoped<IAdminDashboardRepository, AdminDashboardRepository>();
        }
    }
}
