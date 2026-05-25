using Domain.EWS.DataModels.Request.Tasks;
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
        Task<IEnumerable<Projects>> GetProjectsByUserIdAsync(int userId);
    }
}
