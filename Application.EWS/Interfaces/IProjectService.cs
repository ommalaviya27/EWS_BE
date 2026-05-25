using Domain.EWS.DataModels.Request.Project;
using Domain.EWS.DataModels.Response.Project;
using Shared.EWS.DataModel.Response;
using Shared.EWS.Entities;
using Shared.EWS.Interfaces;

namespace Application.EWS.Interfaces
{
    public interface IProjectService : IGenericService<Projects>
    {
        Task<PagedResponse<GetProjectResponse>> GetAllProjectsAsync(ProjectSearchRequest request);
        Task<GetProjectResponse?> GetProjectByIdAsync(Guid id);
        Task<GetProjectResponse> CreateProjectAsync(CreateProjectRequest request, int callerRoleId);
        Task<GetProjectResponse> UpdateProjectAsync(Guid id, UpdateProjectRequest request, int callerRoleId);
        Task<bool> DeleteProjectAsync(Guid id, int callerRoleId);
        Task<IEnumerable<TeamLeaderResponse>> GetTeamLeadersAsync();
    }
}