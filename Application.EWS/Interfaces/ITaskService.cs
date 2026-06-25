using Domain.EWS.DataModels.Request.Tasks;
using Domain.EWS.DataModels.Response.Project;
using Domain.EWS.DataModels.Response.Tasks;
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
        Task<IEnumerable<TeamLeaderResponse>> GetTeamMembersAsync();
        Task<PagedResponse<GetProjectResponse>> GetMyProjectsAsync(ProjectListRequest request);
        Task<TeamLeadDashboardResponse> GetTeamLeadDashboardAsync();
    }
}
