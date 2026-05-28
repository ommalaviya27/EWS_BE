using Infrastructure.EWS.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Shared.EWS.Interfaces;
using Shared.EWS.Interfaces.Repositories;
using Shared.EWS.Interfaces.Services;
using Shared.EWS.Services;
using System.Security.Claims;

namespace Shared.EWS.Extensions
{
    public static class Configuration
    {
        public static void RegisterSharedServices(this IServiceCollection services)
        {
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            services.AddScoped(s =>
                s.GetService<IHttpContextAccessor>()?.HttpContext?.User ?? new ClaimsPrincipal());

            services.AddScoped(typeof(IGenericService<>), typeof(GenericService<>));
            services.AddScoped<IFileService, FileService>();
        }

        public static void RegisterSharedRepositories(this IServiceCollection services)
        {
            services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
        }
    }
}