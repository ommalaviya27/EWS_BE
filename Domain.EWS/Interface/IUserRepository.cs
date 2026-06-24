using Domain.EWS.DataModels.Request.User;
using Domain.EWS.DataModels.Response.User;
using Shared.EWS.Entities;
using Shared.EWS.Interfaces.Repositories;

namespace Domain.EWS.Interface
{
    public interface IUserRepository : IGenericRepository<User>
    {
        Task<UserPagedResponse> GetAllUsersAsync(UserPaginationRequest pagination);
        Task<GetUserResponse?> GetUserByIdWithDetailsAsync(int id);
        Task<bool> EmailExistsAsync(string email, int? excludeId = null);
        Task<bool> RoleExistsAsync(int roleId);
        Task<bool> ReportingExistsAsync(int reportingId);
        Task<bool> AdminExistsAsync(int adminId);
        Task<IEnumerable<RoleResponse>> GetRolesAsync();
        Task<string?> GetRoleNameAsync(int roleId);
        Task<string?> GetReportingNameAsync(int reportingId);
    }
}
