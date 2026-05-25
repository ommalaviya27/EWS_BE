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
        Task<bool> TeamLeadExistsAsync(int teamLeadId);
        Task<User?> GetByEmailAsync(string email);
        Task<IEnumerable<RoleResponse>> GetRolesAsync();
        Task<string?> GetRoleNameAsync(int roleId);
        Task<string?> GetTeamLeadNameAsync(int teamLeadId);
    }
}
