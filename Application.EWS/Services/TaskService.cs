using Application.EWS.Interfaces;
using Domain.EWS.DataModels.Request.Tasks;
using Domain.EWS.DataModels.Response.Project;
using Domain.EWS.DataModels.Response.Tasks;
using Domain.EWS.DataModels.Response.User;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Shared.EWS.Data;
using Shared.EWS.DataModel.Request;
using Shared.EWS.DataModel.Response;
using Shared.EWS.Entities;
using Shared.EWS.Enums;
using Shared.EWS.Exceptions;
using Shared.EWS.Extensions;
using Shared.EWS.Interfaces.Repositories;
using Shared.EWS.Interfaces.Services;
using Shared.EWS.Services;

namespace Application.EWS.Services
{
    public class TaskService(
        IGenericRepository<Tasks> repository,
        EWSDbContext context,
        IFileService fileService)
        : GenericService<Tasks>(repository), ITaskService
    {
        private readonly EWSDbContext _context = context;
        private readonly IFileService _fileService = fileService;

        public async Task<PagedResponse<GetTaskResponse>> GetAllTasksAsync(
            PaginationRequest pagination,
            Guid? projectId,
            int callerUserId,
            int callerRoleId)
        {
            IQueryable<Tasks> query = _context.Tasks
                .Include(t => t.Project)
                .Include(t => t.AssignedTo)
                .Include(t => t.AssignedBy)
                .Include(t => t.Comments).ThenInclude(c => c.User)
                .Include(t => t.Attachments).ThenInclude(a => a.User)
                .Where(t => !t.IsDeleted);

            if (callerRoleId == 2)
            {
                var myProjectIds = await GetTeamLeadProjectIdsAsync(callerUserId);
                query = query.Where(t => myProjectIds.Contains(t.ProjectId));
            }
            else if (callerRoleId == 3)
            {
                query = query.Where(t => t.AssignedToUserId == callerUserId);
            }

            if (projectId.HasValue)
                query = query.Where(t => t.ProjectId == projectId.Value);

            var projected = query.Select(t => MapToResponse(t));
            return await projected.ToPagedResponseAsync(pagination);
        }

        public async Task<GetTaskResponse?> GetTaskByIdAsync(int id, int callerUserId, int callerRoleId)
        {
            var task = await _context.Tasks
                .Include(t => t.Project)
                .Include(t => t.AssignedTo)
                .Include(t => t.AssignedBy)
                .Include(t => t.Comments).ThenInclude(c => c.User)
                .Include(t => t.Attachments).ThenInclude(a => a.User)
                .Where(t => t.Id == id && !t.IsDeleted)
                .FirstOrDefaultAsync()
                ?? throw new NotFoundException($"Task with id '{id}' was not found.");

            await AuthorizeViewAsync(task, callerUserId, callerRoleId);
            return MapToResponse(task);
        }

        public async Task<GetTaskResponse> CreateTaskAsync(
            CreateTaskRequest request,
            int callerUserId,
            int callerRoleId)
        {
            ValidateTeamLeadOrAdmin(callerRoleId, "create");

            var project = await _context.Projects
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == request.ProjectId && !p.IsDeleted)
                ?? throw new NotFoundException($"Project with id '{request.ProjectId}' was not found.");

            if (callerRoleId == 2 && project.UserId != callerUserId)
                throw new ForbiddenException("You can only create tasks for projects assigned to you.");

            await ValidateAssigneeAsync(request.AssignedToUserId, callerUserId, callerRoleId);

            if (request.DueDate <= DateTime.UtcNow)
                throw new InvalidOperationException("Due date must be in the future.");

            var entity = new Tasks
            {
                Title = request.Title.Trim(),
                Description = request.Description.Trim(),
                ProjectId = request.ProjectId,
                AssignedToUserId = request.AssignedToUserId,
                AssignedByUserId = callerUserId,
                TaskStatus = TaskStatuses.Pending,
                Priority = request.Priority,
                DueDate = request.DueDate
            };

            var created = await AddAsync(entity);
            return await GetTaskByIdAsync(created.Id, callerUserId, callerRoleId)
                   ?? throw new InvalidOperationException("Failed to retrieve newly created task.");
        }

        public async Task<GetTaskResponse> UpdateTaskAsync(
            int id,
            UpdateTaskRequest request,
            int callerUserId,
            int callerRoleId)
        {
            ValidateTeamLeadOrAdmin(callerRoleId, "update");

            var task = await GetByIdAsync(id)
                ?? throw new NotFoundException($"Task with id '{id}' was not found.");

            if (callerRoleId == 2)
            {
                var project = await _context.Projects
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.Id == task.ProjectId && !p.IsDeleted)
                    ?? throw new NotFoundException("Associated project not found.");

                if (project.UserId != callerUserId)
                    throw new ForbiddenException("You can only update tasks in projects assigned to you.");
            }

            await ValidateAssigneeAsync(request.AssignedToUserId, callerUserId, callerRoleId);

            task.Title = request.Title.Trim();
            task.Description = request.Description.Trim();
            task.AssignedToUserId = request.AssignedToUserId;
            task.TaskStatus = request.Status;
            task.Priority = request.Priority;
            task.DueDate = request.DueDate;

            var updated = await UpdateAsync(task);
            return await GetTaskByIdAsync(updated.Id, callerUserId, callerRoleId)
                   ?? throw new InvalidOperationException("Failed to retrieve updated task.");
        }

        public async Task<bool> DeleteTaskAsync(int id, int callerUserId, int callerRoleId)
        {
            ValidateTeamLeadOrAdmin(callerRoleId, "delete");

            var task = await GetByIdAsync(id)
                ?? throw new NotFoundException($"Task with id '{id}' was not found.");

            if (callerRoleId == 2)
            {
                var project = await _context.Projects
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.Id == task.ProjectId && !p.IsDeleted)
                    ?? throw new NotFoundException("Associated project not found.");

                if (project.UserId != callerUserId)
                    throw new ForbiddenException("You can only delete tasks in projects assigned to you.");
            }

            return await DeleteAsync(task.Id);
        }

        public async Task<IEnumerable<GetUserResponse>> GetTeamMembersAsync(int callerUserId, int callerRoleId)
        {
            ValidateTeamLeadOrAdmin(callerRoleId, "view team members for");

            return await _context.Users
                .Where(u => u.TeamLeadId == callerUserId && u.RoleId == 3 && !u.IsDeleted)
                .Select(u => new GetUserResponse
                {
                    UserId = u.Id,
                    Name = u.Name,
                    Email = u.Email,
                    MobileNumber = u.MobileNumber,
                    RoleId = u.RoleId,
                    Status = u.status
                })
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<IEnumerable<GetProjectResponse>> GetMyProjectsAsync(int callerUserId)
        {
            return await _context.Projects
                .Where(p => p.UserId == callerUserId && !p.IsDeleted)
                .Select(p => new GetProjectResponse
                {
                    Id = p.Id,
                    Name = p.Name,
                    Description = p.Description,
                    UserId = p.UserId,
                    ProjectStatus = p.ProjectStatus,
                    StartDate = p.StartDate,
                    EndDate = p.EndDate
                })
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<GetTaskResponse> UpdateTaskStatusAsync(
            int taskId,
            UpdateTaskStatusRequest request,
            int callerUserId,
            int callerRoleId)
        {
            var task = await _context.Tasks
                .FirstOrDefaultAsync(t => t.Id == taskId && !t.IsDeleted)
                ?? throw new NotFoundException($"Task with id '{taskId}' was not found.");

            if (callerRoleId == 3 && task.AssignedToUserId != callerUserId)
                throw new ForbiddenException("You can only update the status of tasks assigned to you.");

            if (callerRoleId == 2)
            {
                var myProjectIds = await GetTeamLeadProjectIdsAsync(callerUserId);
                if (!myProjectIds.Contains(task.ProjectId))
                    throw new ForbiddenException("You do not have access to this task.");
            }

            task.TaskStatus = request.Status;
            task.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return await GetTaskByIdAsync(taskId, callerUserId, callerRoleId)
                   ?? throw new InvalidOperationException("Failed to retrieve updated task.");
        }

        private static void ValidateTeamLeadOrAdmin(int callerRoleId, string action)
        {
            if (callerRoleId != 1 && callerRoleId != 2)
                throw new ForbiddenException($"Access denied. Only a Team Lead or Admin can {action} a task.");
        }

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

        private async Task ValidateAssigneeAsync(int assigneeUserId, int callerUserId, int callerRoleId)
        {
            var assignee = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == assigneeUserId && !u.IsDeleted)
                ?? throw new NotFoundException($"User with id '{assigneeUserId}' was not found.");

            if (!assignee.status)
                throw new InvalidOperationException("Cannot assign a task to an inactive user.");

            if (assignee.RoleId != 3)
                throw new InvalidOperationException("Tasks can only be assigned to employees.");

            if (callerRoleId == 2 && assignee.TeamLeadId != callerUserId)
                throw new ForbiddenException("You can only assign tasks to employees under your team.");
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
    }
}