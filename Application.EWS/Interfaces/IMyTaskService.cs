using Domain.EWS.DataModels.Request.Tasks;
using Domain.EWS.DataModels.Response.Tasks;
using Microsoft.AspNetCore.Http;
using Shared.EWS.Entities;
using Shared.EWS.Interfaces;

namespace Application.EWS.Interfaces
{
    public interface IMyTaskService : IGenericService<Tasks>
    {
        Task<List<GetTaskResponse>> GetMyTasksAsync(int callerUserId, int callerRoleId);
        Task<TaskCommentResponse> AddCommentAsync(int taskId, AddTaskCommentRequest request, int callerUserId, int callerRoleId);
        Task<IEnumerable<TaskCommentResponse>> GetCommentsAsync(int taskId, int callerUserId, int callerRoleId);
        Task<IEnumerable<TaskAttachmentResponse>> AddAttachmentsAsync(int taskId, IList<IFormFile> files, int callerUserId, int callerRoleId);
    }
}