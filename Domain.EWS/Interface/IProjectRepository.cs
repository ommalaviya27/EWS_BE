using Domain.EWS.DataModels.Request.Project;
using Domain.EWS.DataModels.Response.Project;
using Shared.EWS.DataModel.Request;
using Shared.EWS.DataModel.Response;
using Shared.EWS.Entities;
using Shared.EWS.Interfaces.Repositories;

namespace Domain.EWS.Interface
{
    public interface IProjectRepository : IGenericRepository<Projects>
    {
        Task<PagedResponse<GetProjectResponse>> GetAllProjectsAsync(ProjectSearchRequest request);
        Task<bool> ProjectNameExistsAsync(string name, Guid? excludeId = null);
        Task<User?> GetUserWithRoleAsync(int userId);
        Task<IEnumerable<TeamLeaderResponse>> GetTeamLeadersAsync();
    }
}
