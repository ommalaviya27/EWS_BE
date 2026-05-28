using Domain.EWS.DataModels.Request.Tasks;
using Domain.EWS.DataModels.Response.Project;
using Domain.EWS.DataModels.Response.Tasks;
using Domain.EWS.DataModels.Response.User;
using Shared.EWS.DataModel.Response;
using Shared.EWS.Entities;
using Shared.EWS.Interfaces;

namespace Application.EWS.Interfaces
{
    public interface ITaskService : IGenericService<Tasks>
    {
        Task<PagedResponse<GetTaskResponse>> GetAllTasksAsync(TaskSearchRequest request, Guid? projectId);
        Task<GetTaskResponse?> GetTaskByIdAsync(int id);
        Task<GetTaskResponse> CreateTaskAsync(CreateTaskRequest request);
        Task<GetTaskResponse> UpdateTaskAsync(int id, UpdateTaskRequest request);
        Task<bool> DeleteTaskAsync(int id);
        Task<IEnumerable<GetUserResponse>> GetTeamMembersAsync();
        Task<IEnumerable<GetProjectResponse>> GetMyProjectsAsync();
        Task<TeamLeadDashboardResponse> GetTeamLeadDashboardAsync(int pageNumber, int pageSize);
    }
}
