using Domain.EWS.DataModels.Response.Admin;
using Shared.EWS.Entities;
using Shared.EWS.Interfaces.Repositories;

namespace Domain.EWS.Interface
{
    public interface IAdminDashboardRepository : IGenericRepository<User>
    {
        Task<AdminDashboardResponse> GetDashboardAsync();
    }
}
