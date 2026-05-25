using Domain.EWS.Interface;
using Microsoft.EntityFrameworkCore;
using Shared.EWS.Data;
using Shared.EWS.Entities;
using Shared.EWS.Enums;

namespace Infrastructure.EWS.Repositories
{
    public class MyTaskRepository(EWSDbContext context)
        : GenericRepository<Tasks>(context), IMyTaskRepository
    {
        public async Task<List<Tasks>> GetTasksWithDetailsByUserAsync(int userId)
            => await _context.Tasks
                .Include(t => t.Project)
                .Include(t => t.AssignedBy)
                .Include(t => t.Comments).ThenInclude(c => c.User)
                .Include(t => t.Attachments).ThenInclude(a => a.User)
                .Where(t => t.AssignedToUserId == userId && !t.IsDeleted)
                .ToListAsync();

        public async Task<Tasks?> GetTaskWithDetailsAsync(int id)
            => await _context.Tasks
                .Include(t => t.Project)
                .Include(t => t.AssignedBy)
                .Include(t => t.Comments).ThenInclude(c => c.User)
                .Include(t => t.Attachments).ThenInclude(a => a.User)
                .FirstOrDefaultAsync(t => t.Id == id && !t.IsDeleted);

        public async Task<List<Guid>> GetTeamLeadProjectIdsAsync(int teamLeadUserId)
            => await _context.Projects
                .Where(p => p.UserId == teamLeadUserId && !p.IsDeleted)
                .Select(p => p.Id)
                .ToListAsync();

        public async Task<Tasks> UpdateTaskStatusAsync(Tasks task, TaskStatuses status)
        {
            task.TaskStatus = status;
            task.UpdatedAt  = DateTime.UtcNow;
            _context.Tasks.Update(task);
            await _context.SaveChangesAsync();
            return task;
        }

        // ── Comments ──────────────────────────────────────────────────────────

        public async Task<TaskComment?> GetCommentWithDetailsAsync(int commentId)
            => await _context.TaskComments
                .Include(c => c.Task)
                .Include(c => c.User)
                .FirstOrDefaultAsync(c => c.Id == commentId && !c.IsDeleted);

        public async Task<List<TaskComment>> GetCommentsByTaskAsync(int taskId)
            => await _context.TaskComments
                .Include(c => c.User)
                .Where(c => c.TaskId == taskId && !c.IsDeleted)
                .OrderBy(c => c.CreatedAt)
                .ToListAsync();

        public async Task<TaskComment> AddCommentAsync(TaskComment comment)
        {
            comment.CreatedAt = DateTime.UtcNow;
            comment.UpdatedAt = DateTime.UtcNow;
            _context.TaskComments.Add(comment);
            await _context.SaveChangesAsync();

            // Re-fetch with navigation so service gets full data back
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

        // ── Attachments ───────────────────────────────────────────────────────

        public async Task<TaskAttachment?> GetAttachmentWithTaskAsync(int attachmentId)
            => await _context.TaskAttachments
                .Include(a => a.Task)
                .FirstOrDefaultAsync(a => a.Id == attachmentId && !a.IsDeleted);

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
