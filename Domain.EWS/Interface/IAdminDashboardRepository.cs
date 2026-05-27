using Domain.EWS.DataModels.Response.Admin;

namespace Domain.EWS.Interface
{
    public interface IAdminDashboardRepository
    {
        Task<AdminDashboardResponse> GetDashboardAsync();
    }
}
