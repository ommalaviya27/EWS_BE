using Application.EWS.Interfaces;
using Shared.EWS.Entities;
using Domain.EWS.DataModels.Request.MyTasks;
using Domain.EWS.DataModels.Response.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Shared.EWS.Services;
using Shared.EWS.Interfaces.Repositories;
using Shared.EWS.Data;
using Shared.EWS.Interfaces.Services;
using Microsoft.EntityFrameworkCore;
using Shared.EWS.Exceptions;

namespace Application.EWS.Services
{
    public class MyTaskService(
        IGenericRepository<Tasks> repository,
        EWSDbContext context,
        IFileService fileService)
        : GenericService<Tasks>(repository), IMyTaskService
    {
        private readonly EWSDbContext _context = context;
        private readonly IFileService _fileService = fileService;

        public async Task<List<GetTaskResponse>> GetMyTasksAsync(int callerUserId, int callerRoleId)
        {
            if (callerRoleId != 3)
                throw new UnauthorizedAccessException("Only employees can access their own tasks.");

            var tasks = await _context.Tasks
                .Include(t => t.Project)
                .Include(t => t.AssignedBy)
                .Include(t => t.Comments).ThenInclude(c => c.User)
                .Include(t => t.Attachments).ThenInclude(a => a.User)
                .Where(t => t.AssignedToUserId == callerUserId && !t.IsDeleted)
                .ToListAsync();

            return tasks.Select(MapToResponse).ToList();
        }

        public async Task<GetTaskResponse> UpdateTaskStatusAsync(int taskId, UpdateTaskStatusRequest request, int callerUserId, int callerRoleId)
        {
            var task = await _context.Tasks
                .Include(t => t.Project)
                .Include(t => t.AssignedBy)
                .Include(t => t.Comments).ThenInclude(c => c.User)
                .Include(t => t.Attachments).ThenInclude(a => a.User)
                .FirstOrDefaultAsync(t => t.Id == taskId && !t.IsDeleted)
                ?? throw new NotFoundException($"Task with id '{taskId}' was not found.");

            await AuthorizeViewAsync(task, callerUserId, callerRoleId);

            task.TaskStatus = request.Status;
            await _context.SaveChangesAsync();

            return MapToResponse(task);
        }

        public async Task<TaskCommentResponse> AddCommentAsync(int taskId, AddTaskCommentRequest request, int callerUserId, int callerRoleId)
        {
            var task = await _context.Tasks
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == taskId && !t.IsDeleted)
                ?? throw new NotFoundException($"Task with id '{taskId}' was not found.");

            await AuthorizeViewAsync(task, callerUserId, callerRoleId);

            var comment = new TaskComment
            {
                TaskId = taskId,
                UserId = callerUserId,
                Comment = request.Comment.Trim()
            };

            _context.TaskComments.Add(comment);
            await _context.SaveChangesAsync();

            var saved = await _context.TaskComments
                .Include(c => c.User)
                .FirstAsync(c => c.Id == comment.Id);

            return MapCommentToResponse(saved);
        }

        public async Task<TaskCommentResponse> UpdateCommentAsync(int commentId, UpdateTaskCommentRequest request, int callerUserId, int callerRoleId)
        {
            var comment = await _context.TaskComments
                .Include(c => c.Task)
                .Include(c => c.User)
                .FirstOrDefaultAsync(c => c.Id == commentId && !c.IsDeleted)
                ?? throw new NotFoundException($"Comment with id '{commentId}' was not found.");

            if (comment.Task == null)
                throw new NotFoundException($"Task for comment with id '{commentId}' was not found.");

            await AuthorizeViewAsync(comment.Task, callerUserId, callerRoleId);

            if (comment.UserId != callerUserId)
                throw new ForbiddenException("You can only edit your own comments.");

            comment.Comment = request.Comment.Trim();
            await _context.SaveChangesAsync();

            return MapCommentToResponse(comment);
        }

        public async Task<bool> DeleteCommentAsync(int commentId, int callerUserId, int callerRoleId)
        {
            var comment = await _context.TaskComments
                .Include(c => c.Task)
                .FirstOrDefaultAsync(c => c.Id == commentId && !c.IsDeleted)
                ?? throw new NotFoundException($"Comment with id '{commentId}' was not found.");

            if (comment.Task == null)
                throw new NotFoundException($"Task for comment with id '{commentId}' was not found.");

            await AuthorizeViewAsync(comment.Task, callerUserId, callerRoleId);

            if (comment.UserId != callerUserId)
                throw new ForbiddenException("You can only delete your own comments.");

            comment.IsDeleted = true;
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<IEnumerable<TaskCommentResponse>> GetCommentsAsync(int taskId, int callerUserId, int callerRoleId)
        {
            var task = await _context.Tasks
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == taskId && !t.IsDeleted)
                ?? throw new NotFoundException($"Task with id '{taskId}' was not found.");

            await AuthorizeViewAsync(task, callerUserId, callerRoleId);

            return await _context.TaskComments
                .Include(c => c.User)
                .Where(c => c.TaskId == taskId && !c.IsDeleted)
                .OrderBy(c => c.CreatedAt)
                .Select(c => MapCommentToResponse(c))
                .ToListAsync();
        }

        public async Task<IEnumerable<TaskAttachmentResponse>> AddAttachmentsAsync(int taskId, IList<IFormFile> files, int callerUserId, int callerRoleId)
        {
            if (files == null || files.Count == 0)
                throw new ArgumentException("No files provided.");

            var task = await _context.Tasks
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == taskId && !t.IsDeleted)
                ?? throw new NotFoundException($"Task with id '{taskId}' was not found.");

            await AuthorizeViewAsync(task, callerUserId, callerRoleId);

            var attachments = new List<TaskAttachment>();
            const string subFolder = "Tasks";

            foreach (var file in files)
            {
                var storedFileName = await _fileService.SaveAttachmentAsync(file, subFolder);
                attachments.Add(new TaskAttachment
                {
                    TaskId = taskId,
                    UserId = callerUserId,
                    FileName = file.FileName,
                    FileUrl = storedFileName,
                    FileSize = file.Length
                });
            }

            _context.TaskAttachments.AddRange(attachments);
            await _context.SaveChangesAsync();

            var ids = attachments.Select(a => a.Id).ToList();
            var saved = await _context.TaskAttachments
                .Include(a => a.User)
                .Where(a => ids.Contains(a.Id))
                .ToListAsync();

            return saved.Select(MapAttachmentToResponse);
        }

        public async Task<bool> DeleteAttachmentAsync(int attachmentId, int callerUserId, int callerRoleId)
        {
            var attachment = await _context.TaskAttachments
                .Include(a => a.Task)
                .FirstOrDefaultAsync(a => a.Id == attachmentId && !a.IsDeleted)
                ?? throw new NotFoundException($"Attachment with id '{attachmentId}' was not found.");

            if (attachment.Task == null)
                throw new NotFoundException($"Task for attachment with id '{attachmentId}' was not found.");

            await AuthorizeViewAsync(attachment.Task, callerUserId, callerRoleId);

            if (attachment.UserId != callerUserId)
                throw new ForbiddenException("You can only delete your own attachments.");

            attachment.IsDeleted = true;
            await _context.SaveChangesAsync();

            const string subFolder = "Tasks";
            var filePath = Path.Combine(_fileService.BaseAttachmentPath, subFolder, attachment.FileUrl);
            if (File.Exists(filePath))
                File.Delete(filePath);

            return true;
        }

        private static GetTaskResponse MapToResponse(Tasks t) => new()
        {
            Id = t.Id,
            Title = t.Title,
            Description = t.Description,
            ProjectId = t.ProjectId,
            ProjectName = t.Project?.Name ?? string.Empty,
            AssignedToUserId = t.AssignedToUserId,
            AssignedToUserName = t.AssignedTo?.Name ?? string.Empty,
            AssignedByUserId = t.AssignedByUserId,
            AssignedByUserName = t.AssignedBy?.Name ?? string.Empty,
            TaskStatus = t.TaskStatus,
            Priority = t.Priority,
            DueDate = t.DueDate,
            Comments = t.Comments
                .Where(c => !c.IsDeleted)
                .OrderBy(c => c.CreatedAt)
                .Select(MapCommentToResponse)
                .ToList(),
            Attachments = t.Attachments
                .Where(a => !a.IsDeleted)
                .OrderBy(a => a.CreatedAt)
                .Select(MapAttachmentToResponse)
                .ToList()
        };

        private static TaskCommentResponse MapCommentToResponse(TaskComment c) => new()
        {
            Id = c.Id,
            TaskId = c.TaskId,
            UserId = c.UserId,
            UserName = c.User?.Name ?? string.Empty,
            Comment = c.Comment,
            CreatedAt = c.CreatedAt
        };

        private static TaskAttachmentResponse MapAttachmentToResponse(TaskAttachment a) => new()
        {
            Id = a.Id,
            TaskId = a.TaskId,
            UserId = a.UserId,
            UserName = a.User?.Name ?? string.Empty,
            FileName = a.FileName,
            FileUrl = a.FileUrl,
            FileSize = a.FileSize,
            CreatedAt = a.CreatedAt
        };

        private async Task<List<Guid>> GetTeamLeadProjectIdsAsync(int teamLeadUserId)
        {
            return await _context.Projects
                .Where(p => p.UserId == teamLeadUserId && !p.IsDeleted)
                .Select(p => p.Id)
                .ToListAsync();
        }

        private async Task AuthorizeViewAsync(Tasks task, int callerUserId, int callerRoleId)
        {
            if (callerRoleId == 1) return;

            if (callerRoleId == 2)
            {
                var myProjectIds = await GetTeamLeadProjectIdsAsync(callerUserId);
                if (!myProjectIds.Contains(task.ProjectId))
                    throw new ForbiddenException("You do not have access to this task.");
            }
            else if (callerRoleId == 3)
            {
                if (task.AssignedToUserId != callerUserId)
                    throw new ForbiddenException("You do not have access to this task.");
            }
        }
    }
}
