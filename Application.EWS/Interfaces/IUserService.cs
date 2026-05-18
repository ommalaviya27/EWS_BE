using Domain.EWS.DataModels.Request.User;
using Domain.EWS.DataModels.Response.User;
using Shared.EWS.Entities;
using Shared.EWS.Interfaces;

namespace Application.EWS.Interfaces
{
    public interface IUserService : IGenericService<User>
    {
        Task<UserPagedResponse> GetAllUsersAsync(int callerRoleId, UserPaginationRequest pagination);
        Task<GetUserResponse?> GetUserByIdAsync(int id, int callerRoleId);
        Task<GetUserResponse> CreateUserAsync(CreateUserRequest request, int callerRoleId);
        Task<GetUserResponse> UpdateUserAsync(int id, UpdateUserRequest request, int callerRoleId);
        Task<bool> DeleteUserAsync(int id, int callerRoleId);
        Task<IEnumerable<RoleResponse>> GetRolesAsync(int callerRoleId);
    }
}