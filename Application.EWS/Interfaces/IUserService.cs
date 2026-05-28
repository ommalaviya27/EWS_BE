using Domain.EWS.DataModels.Request.User;
using Domain.EWS.DataModels.Response.User;
using Shared.EWS.Entities;
using Shared.EWS.Interfaces;

namespace Application.EWS.Interfaces
{
    public interface IUserService : IGenericService<User>
    {
        Task<UserPagedResponse> GetAllUsersAsync(UserPaginationRequest pagination);
        Task<GetUserResponse?> GetUserByIdAsync(int id);
        Task<GetUserResponse> CreateUserAsync(CreateUserRequest request);
        Task<GetUserResponse> UpdateUserAsync(int id, UpdateUserRequest request);
        Task<bool> DeleteUserAsync(int id);
        Task<IEnumerable<RoleResponse>> GetRolesAsync();
    }
}