using Shared.EWS.Entities;
using Shared.EWS.Enums;
using Shared.EWS.Interfaces.Repositories;

namespace Domain.EWS.Interface
{
    public interface IMyTaskRepository : IGenericRepository<Tasks>
    {
        Task<List<Tasks>> GetTasksWithDetailsByUserAsync(int userId);
        Task<Tasks?> GetTaskWithDetailsAsync(int id);
        Task<List<Guid>> GetTeamLeadProjectIdsAsync(int teamLeadUserId);
        Task<Tasks> UpdateTaskStatusAsync(Tasks task, TaskStatuses status);

        // Comments
        Task<TaskComment?> GetCommentWithDetailsAsync(int commentId);
        Task<List<TaskComment>> GetCommentsByTaskAsync(int taskId);
        Task<TaskComment> AddCommentAsync(TaskComment comment);
        Task<TaskComment> UpdateCommentAsync(TaskComment comment);
        Task<bool> SoftDeleteCommentAsync(int commentId);

        // Attachments
        Task<TaskAttachment?> GetAttachmentWithTaskAsync(int attachmentId);
        Task<List<TaskAttachment>> AddAttachmentsAsync(List<TaskAttachment> attachments);
        Task<bool> SoftDeleteAttachmentAsync(int attachmentId);
    }
}