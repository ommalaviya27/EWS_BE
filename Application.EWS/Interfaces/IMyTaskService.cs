using Domain.EWS.DataModels.Request.MyTasks;
using Domain.EWS.DataModels.Response.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Shared.EWS.Entities;
using Shared.EWS.Interfaces;

namespace Application.EWS.Interfaces
{
    public interface IMyTaskService : IGenericService<Tasks>
    {
        Task<List<GetTaskResponse>> GetMyTasksAsync(int callerUserId, int callerRoleId);
        Task<GetTaskResponse> UpdateTaskStatusAsync(int taskId, UpdateTaskStatusRequest request, int callerUserId, int callerRoleId);
        Task<TaskCommentResponse> AddCommentAsync(int taskId, AddTaskCommentRequest request, int callerUserId, int callerRoleId);
        Task<TaskCommentResponse> UpdateCommentAsync(int commentId, UpdateTaskCommentRequest request, int callerUserId, int callerRoleId);
        Task<bool> DeleteCommentAsync(int commentId, int callerUserId, int callerRoleId);
        Task<IEnumerable<TaskCommentResponse>> GetCommentsAsync(int taskId, int callerUserId, int callerRoleId);
        Task<IEnumerable<TaskAttachmentResponse>> AddAttachmentsAsync(int taskId, IList<IFormFile> files, int callerUserId, int callerRoleId);
        Task<bool> DeleteAttachmentAsync(int attachmentId, int callerUserId, int callerRoleId);
    }
}