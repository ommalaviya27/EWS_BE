using Domain.EWS.DataModels.Request.Tasks;
using Domain.EWS.DataModels.Response.Project;
using Domain.EWS.DataModels.Response.Tasks;
using Shared.EWS.DataModel.Request;
using Shared.EWS.DataModel.Response;
using Shared.EWS.Entities;
using Shared.EWS.Interfaces.Repositories;

namespace Domain.EWS.Interface
{
    public interface ITaskRepository : IGenericRepository<Tasks>
    {
        Task<PagedResponse<Tasks>> GetAllTasksWithDetailsAsync(TaskSearchRequest request, Guid? projectId, int? assignedToUserId, List<Guid>? projectIdFilter);
        Task<Tasks?> GetTaskWithDetailsAsync(int id);
        Task<Projects?> GetProjectByIdAsync(Guid projectId);
        Task<User?> GetAssigneeAsync(int userId);
        Task<List<Guid>> GetTeamLeadProjectIdsAsync(int teamLeadUserId);
        Task<IEnumerable<User>> GetTeamMembersAsync(int teamLeadUserId);
        Task<PagedResponse<GetProjectResponse>> GetProjectsByUserIdAsync(int userId, ProjectListRequest request);
        Task<TeamLeadDashboardResponse> GetTeamTaskCountsAsync(int teamLeadUserId);
        Task<int> GetActiveProjectCountAsync(int teamLeadUserId);
        Task<List<GetTaskResponse>> GetRecentTeamTasksAsync(int teamLeadUserId, int take = 5);
        Task<List<GetTaskResponse>> GetOverdueTeamTasksAsync(int teamLeadUserId, int take = 5);

        Task<List<GetProjectResponse>> GetActiveProjectsByTaskCountAsync(int teamLeadUserId, int take = 5);
        Task<List<GetProjectResponse>> GetRecentlyCompletedProjectsAsync(int teamLeadUserId, int take = 5);
    }
}