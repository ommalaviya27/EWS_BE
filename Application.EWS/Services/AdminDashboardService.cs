using Application.EWS.Interfaces;
using Domain.EWS.DataModels.Response.Admin;
using Domain.EWS.Interface;
using Shared.EWS.Entities;
using Shared.EWS.Exceptions;
using Shared.EWS.Services;
using System.Security.Claims;

namespace Application.EWS.Services
{
    public class AdminDashboardService(IAdminDashboardRepository repository, ClaimsPrincipal principal)
        : GenericService<User>(repository, principal), IAdminDashboardService
    {
        private IAdminDashboardRepository DashboardRepository => (IAdminDashboardRepository)_repository;

        public async Task<AdminDashboardResponse> GetDashboardAsync()
        {
            if (CurrentRoleId != 1)
                throw new ForbiddenException("Access denied. Only Admin can access the dashboard.");

            return await DashboardRepository.GetDashboardAsync();
        }
    }
}