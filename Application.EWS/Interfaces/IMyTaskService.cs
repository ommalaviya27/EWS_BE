using Domain.EWS.DataModels.Request.MyTasks;
using Domain.EWS.DataModels.Response.MyTasks;
using Domain.EWS.DataModels.Response.MyTasks;
using Domain.EWS.DataModels.Response.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Shared.EWS.DataModel.Response;
using Shared.EWS.Entities;
using Shared.EWS.Interfaces;

namespace Application.EWS.Interfaces
{
    public interface IMyTaskService : IGenericService<Tasks>
    {
        Task<EmployeeDashboardResponse> GetEmployeeDashboardAsync();
        Task<List<MyProjectResponse>> GetMyProjectsAsync();
        Task<List<GetTaskResponse>> GetMyTasksAsync(Guid? projectId = null);
        Task<GetTaskResponse> UpdateTaskStatusAsync(int taskId, UpdateTaskStatusRequest request);
        Task<TaskCommentResponse> AddCommentAsync(int taskId, AddTaskCommentRequest request);
        Task<TaskCommentResponse> UpdateCommentAsync(int commentId, UpdateTaskCommentRequest request);
        Task<bool> DeleteCommentAsync(int commentId);
        Task<PagedResponse<TaskCommentResponse>> GetCommentsPagedAsync(int taskId, GetCommentPaginationRequest pagination);
        Task<IEnumerable<TaskAttachmentResponse>> AddAttachmentsAsync(int taskId, IList<IFormFile> files);
        Task<bool> DeleteAttachmentAsync(int attachmentId);
        Task<PagedResponse<TaskAttachmentResponse>> GetAttachmentsPagedAsync(int taskId, GetAttachmentPaginationRequest pagination);
        Task<FileContentHttpResult> DownloadAttachmentAsync(int attachmentId);
    }
}