using Domain.EWS.DataModels.Request.MyTasks;
using Domain.EWS.Interface;
using Microsoft.EntityFrameworkCore;
using Shared.EWS.Data;
using Shared.EWS.DataModel.Request;
using Shared.EWS.DataModel.Response;
using Shared.EWS.Entities;
using Shared.EWS.Enums;
using Shared.EWS.Extensions;

namespace Infrastructure.EWS.Repositories
{
    public class MyTaskRepository(EWSDbContext context)
        : GenericRepository<Tasks>(context), IMyTaskRepository
    {
        public async Task<PagedResponse<Tasks>> GetTasksWithDetailsByUserAsync(
            int userId,
            MyTaskSearchRequest request,
            Guid? projectId)
        {
            var query = _context.Tasks
                .Include(t => t.Project)
                .Include(t => t.AssignedBy)
                .Where(t => t.AssignedToUserId == userId && !t.IsDeleted)
                .AsQueryable();

            if (projectId.HasValue)
                query = query.Where(t => t.ProjectId == projectId.Value);

            var search = request.Search?.Trim();
            if (!string.IsNullOrEmpty(search))
                query = query.Where(t => EF.Functions.ILike(t.Title, $"%{search}%"));

            if (request.Status.HasValue)
                query = query.Where(t => t.TaskStatus == request.Status.Value);

            if (request.Priority.HasValue)
                query = query.Where(t => t.Priority == request.Priority.Value);

            if (request.DueDateFrom.HasValue)
                query = query.Where(t => t.DueDate >= request.DueDateFrom.Value.ToUniversalTime());

            if (request.DueDateTo.HasValue)
                query = query.Where(t => t.DueDate <= request.DueDateTo.Value.ToUniversalTime().AddDays(1).AddSeconds(-1));

            return await query.ToPagedResponseAsync(request);
        }

        public async Task<Tasks?> GetTaskWithDetailsAsync(int id)
            => await _context.Tasks
                .Include(t => t.Project)
                .Include(t => t.AssignedBy)
                .FirstOrDefaultAsync(t => t.Id == id && !t.IsDeleted);

        public async Task<List<Guid>> GetTeamLeadProjectIdsAsync(int teamLeadUserId)
            => await _context.Projects
                .Where(p => p.UserId == teamLeadUserId && !p.IsDeleted)
                .Select(p => p.Id)
                .ToListAsync();

        public async Task<Tasks> UpdateTaskStatusAsync(Tasks task, TaskStatuses status)
        {
            task.TaskStatus = status;
            task.UpdatedAt = DateTime.UtcNow;
            _context.Tasks.Update(task);
            await _context.SaveChangesAsync();
            return task;
        }

        public async Task<TaskComment?> GetCommentWithDetailsAsync(int commentId)
            => await _context.TaskComments
                .Include(c => c.Task)
                .Include(c => c.User)
                .FirstOrDefaultAsync(c => c.Id == commentId && !c.IsDeleted);

        public async Task<PagedResponse<TaskComment>> GetCommentsByTaskPagedAsync(int taskId, PaginationRequest pagination)
            => await _context.TaskComments
                .Include(c => c.User)
                .Where(c => c.TaskId == taskId && !c.IsDeleted)
                .OrderBy(c => c.CreatedAt)
                .ToPagedResponseAsync(pagination);

        public async Task<TaskComment> AddCommentAsync(TaskComment comment)
        {
            comment.CreatedAt = DateTime.UtcNow;
            comment.UpdatedAt = DateTime.UtcNow;
            _context.TaskComments.Add(comment);
            await _context.SaveChangesAsync();

            return await _context.TaskComments
                .Include(c => c.User)
                .FirstAsync(c => c.Id == comment.Id);
        }

        public async Task<TaskComment> UpdateCommentAsync(TaskComment comment)
        {
            comment.UpdatedAt = DateTime.UtcNow;
            _context.TaskComments.Update(comment);
            await _context.SaveChangesAsync();
            return comment;
        }

        public async Task<bool> SoftDeleteCommentAsync(int commentId)
        {
            var comment = await _context.TaskComments.FindAsync(commentId);
            if (comment is null) return false;

            comment.IsDeleted = true;
            comment.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<TaskAttachment?> GetAttachmentWithTaskAsync(int attachmentId)
            => await _context.TaskAttachments
                .Include(a => a.Task)
                .FirstOrDefaultAsync(a => a.Id == attachmentId && !a.IsDeleted);

        public async Task<PagedResponse<TaskAttachment>> GetAttachmentsByTaskPagedAsync(int taskId, PaginationRequest pagination)
            => await _context.TaskAttachments
                .Include(a => a.User)
                .Where(a => a.TaskId == taskId && !a.IsDeleted)
                .OrderBy(a => a.CreatedAt)
                .ToPagedResponseAsync(pagination);

        public async Task<List<TaskAttachment>> AddAttachmentsAsync(List<TaskAttachment> attachments)
        {
            var now = DateTime.UtcNow;
            foreach (var a in attachments)
            {
                a.CreatedAt = now;
                a.UpdatedAt = now;
            }

            _context.TaskAttachments.AddRange(attachments);
            await _context.SaveChangesAsync();

            var ids = attachments.Select(a => a.Id).ToList();
            return await _context.TaskAttachments
                .Include(a => a.User)
                .Where(a => ids.Contains(a.Id))
                .ToListAsync();
        }

        public async Task<bool> SoftDeleteAttachmentAsync(int attachmentId)
        {
            var attachment = await _context.TaskAttachments.FindAsync(attachmentId);
            if (attachment is null) return false;

            attachment.IsDeleted = true;
            attachment.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return true;
        }
    }
}