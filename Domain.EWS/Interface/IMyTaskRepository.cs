using Domain.EWS.DataModels.Request.MyTasks;
using Shared.EWS.DataModel.Request;
using Shared.EWS.DataModel.Response;
using Shared.EWS.Entities;
using Shared.EWS.Enums;
using Shared.EWS.Interfaces.Repositories;

namespace Domain.EWS.Interface
{
    public interface IMyTaskRepository : IGenericRepository<Tasks>
    {
        Task<PagedResponse<Tasks>> GetTasksWithDetailsByUserAsync(int userId, MyTaskSearchRequest request, Guid? projectId);
        Task<List<Tasks>> GetAllTasksByUserAsync(int userId);
        Task<List<Tasks>> GetOverdueTasksAsync(int userId);
        Task<Tasks?> GetTaskWithDetailsAsync(int id);
        Task<List<Guid>> GetTeamLeadProjectIdsAsync(int teamLeadUserId);
        Task<Tasks> UpdateTaskStatusAsync(Tasks task, TaskStatuses status);

        Task<TaskComment?> GetCommentWithDetailsAsync(int commentId);
        Task<PagedResponse<TaskComment>> GetCommentsByTaskPagedAsync(int taskId, PaginationRequest pagination);
        Task<TaskComment> AddCommentAsync(TaskComment comment);
        Task<TaskComment> UpdateCommentAsync(TaskComment comment);
        Task<bool> SoftDeleteCommentAsync(int commentId);

        Task<TaskAttachment?> GetAttachmentWithTaskAsync(int attachmentId);
        Task<PagedResponse<TaskAttachment>> GetAttachmentsByTaskPagedAsync(int taskId, PaginationRequest pagination);
        Task<List<TaskAttachment>> AddAttachmentsAsync(List<TaskAttachment> attachments);
        Task<bool> SoftDeleteAttachmentAsync(int attachmentId);
    }
}