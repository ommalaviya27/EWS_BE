using AutoMapper;
using Application.EWS.Interfaces;
using Domain.EWS.DataModels.Request.MyTasks;
using Domain.EWS.DataModels.Response.MyTasks;
using Domain.EWS.DataModels.Response.Tasks;
using Domain.EWS.Interface;
using Microsoft.AspNetCore.Http;
using Shared.EWS.DataModel.Response;
using Shared.EWS.Entities;
using Shared.EWS.Exceptions;
using Shared.EWS.Interfaces.Services;
using Shared.EWS.Services;
using System.Security.Claims;

namespace Application.EWS.Services
{
    public class MyTaskService(
        IMyTaskRepository repository,
        IFileService fileService,
        IMapper mapper,
        ClaimsPrincipal principal)
        : GenericService<Tasks>(repository, principal), IMyTaskService
    {
        private readonly IMyTaskRepository _myTaskRepository = repository;
        private readonly IFileService _fileService = fileService;
        private readonly IMapper _mapper = mapper;

        public async Task<EmployeeDashboardResponse> GetEmployeeDashboardAsync()
        {
            if (CurrentRoleId != 3)
                throw new UnauthorizedAccessException("Only employees can access their dashboard.");

            var now = DateTime.UtcNow;
            var weekEnd = now.AddDays(7);

            var allTasksRequest = new MyTaskSearchRequest { PageNumber = 1, PageSize = int.MaxValue };
            var pagedTasks = await _myTaskRepository.GetTasksWithDetailsByUserAsync(CurrentUserId, allTasksRequest, null);
            var tasks = pagedTasks.Items;

            var assignedTasks = tasks.ToList();

            var completedTasks = tasks
                .Where(t => t.TaskStatus == Shared.EWS.Enums.TaskStatuses.Completed)
                .ToList();

            var upcomingDeadlines = tasks
                .Where(t => t.TaskStatus != Shared.EWS.Enums.TaskStatuses.Completed
                         && t.DueDate >= now
                         && t.DueDate <= weekEnd)
                .OrderBy(t => t.DueDate)
                .ToList();

            var onHoldTasks = tasks
                .Where(t => t.TaskStatus == Shared.EWS.Enums.TaskStatuses.OnHold)
                .OrderBy(t => t.DueDate)
                .ToList();

            return new EmployeeDashboardResponse
            {
                AssignedTaskCount     = assignedTasks.Count,
                CompletedTaskCount    = completedTasks.Count,
                UpcomingDeadlineCount = upcomingDeadlines.Count,
                UpcomingDeadlines     = _mapper.Map<List<GetTaskResponse>>(upcomingDeadlines),
                OnHoldTasks           = _mapper.Map<List<GetTaskResponse>>(onHoldTasks),
                CompletedTasks        = _mapper.Map<List<GetTaskResponse>>(completedTasks),
            };
        }

        public async Task<List<MyProjectResponse>> GetMyProjectsAsync()
        {
            if (CurrentRoleId != 3)
                throw new UnauthorizedAccessException("Only employees can access their project list.");

            var allTasksRequest = new MyTaskSearchRequest { PageNumber = 1, PageSize = int.MaxValue };
            var pagedTasks = await _myTaskRepository.GetTasksWithDetailsByUserAsync(CurrentUserId, allTasksRequest, null);
            var tasks = pagedTasks.Items;

            var seen = new HashSet<Guid>();
            var projects = new List<MyProjectResponse>();

            foreach (var t in tasks)
            {
                if (t.Project == null || !seen.Add(t.ProjectId)) continue;
                projects.Add(new MyProjectResponse
                {
                    Id            = t.Project.Id,
                    Name          = t.Project.Name,
                    Description   = t.Project.Description,
                    ProjectStatus = (int?)t.Project.ProjectStatus,
                    StartDate     = t.Project.StartDate,
                    EndDate       = t.Project.EndDate,
                });
            }

            return projects;
        }

        public async Task<PagedResponse<GetTaskResponse>> GetMyTasksAsync(
            MyTaskSearchRequest request,
            Guid? projectId = null)
        {
            if (CurrentRoleId != 3)
                throw new UnauthorizedAccessException("Only employees can access their own tasks.");

            var paged = await _myTaskRepository.GetTasksWithDetailsByUserAsync(CurrentUserId, request, projectId);
            var mapped = paged.Items.Select(t => _mapper.Map<GetTaskResponse>(t)).ToList();

            return new PagedResponse<GetTaskResponse>
            {
                Items      = mapped,
                TotalCount = paged.TotalCount,
                PageNumber = paged.PageNumber,
                PageSize   = paged.PageSize,
            };
        }

        public async Task<GetTaskResponse> UpdateTaskStatusAsync(int taskId, UpdateTaskStatusRequest request)
        {
            var task = await _myTaskRepository.GetTaskWithDetailsAsync(taskId)
                ?? throw new NotFoundException($"Task with id '{taskId}' was not found.");

            await AuthorizeViewAsync(task);
            var updated = await _myTaskRepository.UpdateTaskStatusAsync(task, request.Status);

            return _mapper.Map<GetTaskResponse>(updated);
        }

        public async Task<TaskCommentResponse> AddCommentAsync(int taskId, AddTaskCommentRequest request)
        {
            if (CurrentRoleId != 3)
                throw new ForbiddenException("Only employees can add comments.");

            var task = await _myTaskRepository.GetTaskWithDetailsAsync(taskId)
                ?? throw new NotFoundException($"Task with id '{taskId}' was not found.");

            await AuthorizeViewAsync(task);

            var comment = new TaskComment
            {
                TaskId  = taskId,
                UserId  = CurrentUserId,
                Comment = request.Comment.Trim()
            };

            var saved = await _myTaskRepository.AddCommentAsync(comment);
            return _mapper.Map<TaskCommentResponse>(saved);
        }

        public async Task<TaskCommentResponse> UpdateCommentAsync(int commentId, UpdateTaskCommentRequest request)
        {
            if (CurrentRoleId != 3)
                throw new ForbiddenException("Only employees can update comments.");

            var comment = await _myTaskRepository.GetCommentWithDetailsAsync(commentId)
                ?? throw new NotFoundException($"Comment with id '{commentId}' was not found.");

            if (comment.Task == null)
                throw new NotFoundException($"Task for comment with id '{commentId}' was not found.");

            await AuthorizeViewAsync(comment.Task);

            if (comment.UserId != CurrentUserId)
                throw new ForbiddenException("You can only edit your own comments.");

            comment.Comment = request.Comment.Trim();
            var updated = await _myTaskRepository.UpdateCommentAsync(comment);
            return _mapper.Map<TaskCommentResponse>(updated);
        }

        public async Task<bool> DeleteCommentAsync(int commentId)
        {
            if (CurrentRoleId != 3)
                throw new ForbiddenException("Only employees can delete comments.");

            var comment = await _myTaskRepository.GetCommentWithDetailsAsync(commentId)
                ?? throw new NotFoundException($"Comment with id '{commentId}' was not found.");

            if (comment.Task == null)
                throw new NotFoundException($"Task for comment with id '{commentId}' was not found.");

            await AuthorizeViewAsync(comment.Task);

            if (comment.UserId != CurrentUserId)
                throw new ForbiddenException("You can only delete your own comments.");

            return await _myTaskRepository.SoftDeleteCommentAsync(commentId);
        }

        public async Task<PagedResponse<TaskCommentResponse>> GetCommentsPagedAsync(
            int taskId, GetCommentPaginationRequest pagination)
        {
            var task = await _myTaskRepository.GetTaskWithDetailsAsync(taskId)
                ?? throw new NotFoundException($"Task with id '{taskId}' was not found.");

            await AuthorizeViewAsync(task);

            var pagedComments = await _myTaskRepository.GetCommentsByTaskPagedAsync(taskId, pagination);
            var mappedItems   = _mapper.Map<IEnumerable<TaskCommentResponse>>(pagedComments.Items);
            return PagedResponse<TaskCommentResponse>.Create(
                mappedItems,
                pagedComments.TotalCount,
                pagedComments.PageNumber,
                pagedComments.PageSize);
        }

        public async Task<IEnumerable<TaskAttachmentResponse>> AddAttachmentsAsync(int taskId, IList<IFormFile> files)
        {
            if (CurrentRoleId != 3)
                throw new ForbiddenException("Only employees can upload attachments.");

            if (files == null || files.Count == 0)
                throw new ArgumentException("No files provided.");

            var task = await _myTaskRepository.GetTaskWithDetailsAsync(taskId)
                ?? throw new NotFoundException($"Task with id '{taskId}' was not found.");

            await AuthorizeViewAsync(task);

            const string subFolder = "Tasks";
            var attachments = new List<TaskAttachment>();

            foreach (var file in files)
            {
                var storedFileName = await _fileService.SaveAttachmentAsync(file, subFolder);
                attachments.Add(new TaskAttachment
                {
                    TaskId   = taskId,
                    UserId   = CurrentUserId,
                    FileName = file.FileName,
                    FileUrl  = storedFileName,
                    FileSize = file.Length
                });
            }

            var saved = await _myTaskRepository.AddAttachmentsAsync(attachments);
            return _mapper.Map<IEnumerable<TaskAttachmentResponse>>(saved);
        }

        public async Task<bool> DeleteAttachmentAsync(int attachmentId)
        {
            if (CurrentRoleId != 3)
                throw new ForbiddenException("Only employees can delete attachments.");

            var attachment = await _myTaskRepository.GetAttachmentWithTaskAsync(attachmentId)
                ?? throw new NotFoundException($"Attachment with id '{attachmentId}' was not found.");

            if (attachment.Task == null)
                throw new NotFoundException($"Task for attachment with id '{attachmentId}' was not found.");

            await AuthorizeViewAsync(attachment.Task);

            if (attachment.UserId != CurrentUserId)
                throw new ForbiddenException("You can only delete your own attachments.");

            await _myTaskRepository.SoftDeleteAttachmentAsync(attachmentId);

            const string subFolder = "Tasks";
            var filePath = Path.Combine(_fileService.BaseAttachmentPath, subFolder, attachment.FileUrl);
            if (File.Exists(filePath))
                File.Delete(filePath);

            return true;
        }

        public async Task<PagedResponse<TaskAttachmentResponse>> GetAttachmentsPagedAsync(
            int taskId, GetAttachmentPaginationRequest pagination)
        {
            var task = await _myTaskRepository.GetTaskWithDetailsAsync(taskId)
                ?? throw new NotFoundException($"Task with id '{taskId}' was not found.");

            await AuthorizeViewAsync(task);

            var pagedAttachments = await _myTaskRepository.GetAttachmentsByTaskPagedAsync(taskId, pagination);
            var mappedItems      = _mapper.Map<IEnumerable<TaskAttachmentResponse>>(pagedAttachments.Items);
            return PagedResponse<TaskAttachmentResponse>.Create(
                mappedItems,
                pagedAttachments.TotalCount,
                pagedAttachments.PageNumber,
                pagedAttachments.PageSize);
        }

        public async Task<Microsoft.AspNetCore.Http.HttpResults.FileContentHttpResult> DownloadAttachmentAsync(int attachmentId)
        {
            var attachment = await _myTaskRepository.GetAttachmentWithTaskAsync(attachmentId)
                ?? throw new NotFoundException($"Attachment with id '{attachmentId}' was not found.");

            if (attachment.Task == null)
                throw new NotFoundException($"Task for attachment id '{attachmentId}' was not found.");

            await AuthorizeViewAsync(attachment.Task);

            return await _fileService.GetFileResultAsync(attachment.FileUrl, "Tasks", attachment.FileName);
        }

        private async Task AuthorizeViewAsync(Tasks task)
        {
            if (CurrentRoleId == 1) return;

            if (CurrentRoleId == 2)
            {
                var myProjectIds = await _myTaskRepository.GetTeamLeadProjectIdsAsync(CurrentUserId);
                if (!myProjectIds.Contains(task.ProjectId))
                    throw new ForbiddenException("You do not have access to this task.");
            }
            else if (CurrentRoleId == 3)
            {
                if (task.AssignedToUserId != CurrentUserId)
                    throw new ForbiddenException("You do not have access to this task.");
            }
        }
    }
}