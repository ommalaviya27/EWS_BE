using Domain.EWS.DataModels.Request.Project;
using Domain.EWS.DataModels.Response.Project;
using Shared.EWS.Entities;
using Shared.EWS.Interfaces;
using Shared.EWS.DataModel.Response;
using Shared.EWS.DataModel.Request;

namespace Application.EWS.Interfaces
{
    public interface IProjectService : IGenericService<Projects>
    {
        Task<PagedResponse<GetProjectResponse>> GetAllProjectsAsync(PaginationRequest pagination);
        Task<GetProjectResponse?> GetProjectByIdAsync(Guid id);
        Task<GetProjectResponse> CreateProjectAsync(CreateProjectRequest request, int callerRoleId);
        Task<GetProjectResponse> UpdateProjectAsync(Guid id, UpdateProjectRequest request, int callerRoleId);
        Task<bool> DeleteProjectAsync(Guid id, int callerRoleId);
        Task<IEnumerable<TeamLeaderResponse>> GetTeamLeadersAsync();
    }
}