using Domain.EWS.DataModels.Response.Admin;
using Shared.EWS.Entities;
using Shared.EWS.Interfaces;

namespace Application.EWS.Interfaces
{
    public interface IAdminDashboardService : IGenericService<User>
    {
        Task<AdminDashboardResponse> GetDashboardAsync();
    }
}