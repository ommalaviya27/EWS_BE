using Domain.EWS.DataModels.Request.MyTasks;
using Domain.EWS.DataModels.Response.MyTasks;
using Domain.EWS.DataModels.Response.Project;
using Domain.EWS.DataModels.Response.Tasks;
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

        public async Task<EmployeeDashboardResponse> GetEmployeeDashboardCountsAsync(int userId)
        {
            var now = DateTime.UtcNow;
            var fiveDaysEnd = now.AddDays(5);

            return await _context.Tasks
                .Where(t => t.AssignedToUserId == userId && !t.IsDeleted)
                .GroupBy(_ => 1)
                .Select(g => new EmployeeDashboardResponse
                {
                    AssignedTaskCount = g.Count(),
                    CompletedTaskCount = g.Count(t => t.TaskStatus == TaskStatuses.Completed),
                    UpcomingDeadlineCount = g.Count(t =>
                        t.TaskStatus != TaskStatuses.Completed &&
                        t.DueDate >= now &&
                        t.DueDate <= fiveDaysEnd),
                })
                .AsNoTracking()
                .FirstOrDefaultAsync()
                ?? new EmployeeDashboardResponse();
        }

        public async Task<List<GetTaskResponse>> GetUpcomingDeadlineTasksAsync(int userId, int take = 5)
        {
            var now = DateTime.UtcNow;
            var fiveDaysEnd = now.AddDays(5);

            return await _context.Tasks
                .Where(t => t.AssignedToUserId == userId
                         && !t.IsDeleted
                         && t.TaskStatus != TaskStatuses.Completed
                         && t.DueDate >= now
                         && t.DueDate <= fiveDaysEnd)
                .OrderBy(t => t.DueDate)
                .Take(take)
                .Select(t => new GetTaskResponse
                {
                    Id = t.Id,
                    Title = t.Title,
                    Description = t.Description,
                    ProjectId = t.ProjectId,
                    ProjectName = t.Project != null ? t.Project.Name : string.Empty,
                    AssignedToUserId = t.AssignedToUserId,
                    AssignedByUserId = t.AssignedByUserId,
                    AssignedByUserName = t.AssignedBy != null ? t.AssignedBy.Name : string.Empty,
                    TaskStatus = t.TaskStatus,
                    Priority = t.Priority,
                    DueDate = t.DueDate,
                })
                .AsNoTracking()
                .ToListAsync();
        }


        public async Task<List<GetTaskResponse>> GetOnHoldActiveProjectTasksAsync(int userId, int take = 5)
        {
            return await _context.Tasks
                .Where(t => t.AssignedToUserId == userId
                         && !t.IsDeleted
                         && t.TaskStatus == TaskStatuses.OnHold
                         && t.Project != null
                         && t.Project.ProjectStatus == ProjectStatus.Active)
                .OrderBy(t => t.DueDate)
                .Take(take)
                .Select(t => new GetTaskResponse
                {
                    Id = t.Id,
                    Title = t.Title,
                    Description = t.Description,
                    ProjectId = t.ProjectId,
                    ProjectName = t.Project != null ? t.Project.Name : string.Empty,
                    AssignedToUserId = t.AssignedToUserId,
                    AssignedByUserId = t.AssignedByUserId,
                    AssignedByUserName = t.AssignedBy != null ? t.AssignedBy.Name : string.Empty,
                    TaskStatus = t.TaskStatus,
                    Priority = t.Priority,
                    DueDate = t.DueDate,
                })
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<List<GetTaskResponse>> GetRecentCompletedTasksAsync(int userId, int take = 5)
        {
            return await _context.Tasks
                .Where(t => t.AssignedToUserId == userId
                         && !t.IsDeleted
                         && t.TaskStatus == TaskStatuses.Completed)
                .OrderByDescending(t => t.UpdatedAt)
                .Take(take)
                .Select(t => new GetTaskResponse
                {
                    Id = t.Id,
                    Title = t.Title,
                    Description = t.Description,
                    ProjectId = t.ProjectId,
                    ProjectName = t.Project != null ? t.Project.Name : string.Empty,
                    AssignedToUserId = t.AssignedToUserId,
                    AssignedByUserId = t.AssignedByUserId,
                    AssignedByUserName = t.AssignedBy != null ? t.AssignedBy.Name : string.Empty,
                    TaskStatus = t.TaskStatus,
                    Priority = t.Priority,
                    DueDate = t.DueDate,
                })
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<List<GetTaskResponse>> GetOverdueTasksAsync(int userId, int take = 5)
        {
            var now = DateTime.UtcNow;

            return await _context.Tasks
                .Where(t => t.AssignedToUserId == userId
                         && !t.IsDeleted
                         && t.TaskStatus != TaskStatuses.Completed
                         && t.DueDate < now)
                .OrderBy(t => t.DueDate)
                .Take(take)
                .Select(t => new GetTaskResponse
                {
                    Id = t.Id,
                    Title = t.Title,
                    Description = t.Description,
                    ProjectId = t.ProjectId,
                    ProjectName = t.Project != null ? t.Project.Name : string.Empty,
                    AssignedToUserId = t.AssignedToUserId,
                    AssignedByUserId = t.AssignedByUserId,
                    AssignedByUserName = t.AssignedBy != null ? t.AssignedBy.Name : string.Empty,
                    TaskStatus = t.TaskStatus,
                    Priority = t.Priority,
                    DueDate = t.DueDate,
                })
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<PagedResponse<GetProjectResponse>> GetMyProjectsAsync(int userId, MyProjectListRequest request)
        {
            var projectIds = await _context.Tasks
                .Where(t => t.AssignedToUserId == userId && !t.IsDeleted)
                .Select(t => t.ProjectId)
                .Distinct()
                .ToListAsync();

            if (!projectIds.Any())
                return PagedResponse<GetProjectResponse>.Create(
                    new List<GetProjectResponse>(), 0, request.PageNumber, request.PageSize);

            var query = _context.Projects
                .Where(p => projectIds.Contains(p.Id) && !p.IsDeleted)
                .AsQueryable();

            var search = request.Search?.Trim();
            if (!string.IsNullOrEmpty(search))
                query = query.Where(p => EF.Functions.ILike(p.Name, $"%{search}%"));

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderBy(p => p.Name)
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(p => new GetProjectResponse
                {
                    Id = p.Id,
                    Name = p.Name,
                    Description = p.Description,
                    UserId = p.UserId,
                    ProjectStatus = p.ProjectStatus,
                    StartDate = p.StartDate,
                    EndDate = p.EndDate,
                    TaskCount = _context.Tasks.Count(t => t.ProjectId == p.Id && t.AssignedToUserId == userId && !t.IsDeleted),
                })
                .AsNoTracking()
                .ToListAsync();

            return PagedResponse<GetProjectResponse>.Create(items, totalCount, request.PageNumber, request.PageSize);
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