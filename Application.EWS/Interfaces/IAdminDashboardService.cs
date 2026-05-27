using Domain.EWS.DataModels.Response.Admin;

namespace Application.EWS.Interfaces
{
    public interface IAdminDashboardService
    {
        Task<AdminDashboardResponse> GetDashboardAsync(int callerRoleId);
    }
}
