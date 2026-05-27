using Application.EWS.Interfaces;
using Domain.EWS.DataModels.Response.Admin;
using Domain.EWS.Interface;
using Shared.EWS.Exceptions;

namespace Application.EWS.Services
{
    public class AdminDashboardService(IAdminDashboardRepository repository) : IAdminDashboardService
    {
        private readonly IAdminDashboardRepository _repository = repository;

        public async Task<AdminDashboardResponse> GetDashboardAsync(int callerRoleId)
        {
            if (callerRoleId != 1)
                throw new ForbiddenException("Access denied. Only Admin can access the dashboard.");

            return await _repository.GetDashboardAsync();
        }
    }
}