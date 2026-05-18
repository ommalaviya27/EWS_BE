using Domain.EWS.DataModels.Request.Tasks;
using Domain.EWS.DataModels.Response.Project;
using Domain.EWS.DataModels.Response.Tasks;
using Domain.EWS.DataModels.Response.User;
using Shared.EWS.DataModel.Request;
using Shared.EWS.DataModel.Response;
using Shared.EWS.Entities;
using Shared.EWS.Interfaces;

namespace Application.EWS.Interfaces
{
    public interface ITaskService : IGenericService<Tasks>
    {
        Task<PagedResponse<GetTaskResponse>> GetAllTasksAsync(PaginationRequest pagination, Guid? projectId, int callerUserId, int callerRoleId);
        Task<GetTaskResponse?> GetTaskByIdAsync(int id, int callerUserId, int callerRoleId);
        Task<GetTaskResponse> CreateTaskAsync(CreateTaskRequest request, int callerUserId, int callerRoleId);
        Task<GetTaskResponse> UpdateTaskAsync(int id, UpdateTaskRequest request, int callerUserId, int callerRoleId);
        Task<bool> DeleteTaskAsync(int id, int callerUserId, int callerRoleId);
        Task<IEnumerable<GetUserResponse>> GetTeamMembersAsync(int callerUserId, int callerRoleId);
        Task<IEnumerable<GetProjectResponse>> GetMyProjectsAsync(int callerUserId);
    }
}
