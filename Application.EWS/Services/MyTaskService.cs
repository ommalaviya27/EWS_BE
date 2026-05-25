using AutoMapper;
using Application.EWS.Interfaces;
using Domain.EWS.DataModels.Request.MyTasks;
using Domain.EWS.DataModels.Response.MyTasks;
using Domain.EWS.DataModels.Response.Tasks;
using Domain.EWS.Interface;
using Microsoft.AspNetCore.Http;
using Shared.EWS.Entities;
using Shared.EWS.Exceptions;
using Shared.EWS.Interfaces.Services;
using Shared.EWS.Services;

namespace Application.EWS.Services
{
    public class MyTaskService(
        IMyTaskRepository repository,
        IFileService fileService,
        IMapper mapper)
        : GenericService<Tasks>(repository), IMyTaskService
    {
        private readonly IMyTaskRepository _myTaskRepository = repository;
        private readonly IFileService _fileService = fileService;
        private readonly IMapper _mapper = mapper;

        public async Task<EmployeeDashboardResponse> GetEmployeeDashboardAsync(int callerUserId, int callerRoleId)
        {
            if (callerRoleId != 3)
                throw new UnauthorizedAccessException("Only employees can access their dashboard.");

            var now     = DateTime.UtcNow;
            var weekEnd = now.AddDays(7);

            var tasks = await _myTaskRepository.GetTasksWithDetailsByUserAsync(callerUserId);

            var assignedTasks = tasks
                .Where(t => t.TaskStatus != Shared.EWS.Enums.TaskStatuses.Completed)
                .ToList();

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
                AssignedTasks         = _mapper.Map<List<GetTaskResponse>>(assignedTasks),
                UpcomingDeadlines     = _mapper.Map<List<GetTaskResponse>>(upcomingDeadlines),
                OnHoldTasks           = _mapper.Map<List<GetTaskResponse>>(onHoldTasks),
                CompletedTasks        = _mapper.Map<List<GetTaskResponse>>(completedTasks),
            };
        }

        public async Task<List<GetTaskResponse>> GetMyTasksAsync(int callerUserId, int callerRoleId)
        {
            if (callerRoleId != 3)
                throw new UnauthorizedAccessException("Only employees can access their own tasks.");

            var tasks = await _myTaskRepository.GetTasksWithDetailsByUserAsync(callerUserId);
            return _mapper.Map<List<GetTaskResponse>>(tasks);
        }

        public async Task<GetTaskResponse> UpdateTaskStatusAsync(int taskId, UpdateTaskStatusRequest request, int callerUserId, int callerRoleId)
        {
            var task = await _myTaskRepository.GetTaskWithDetailsAsync(taskId)
                ?? throw new NotFoundException($"Task with id '{taskId}' was not found.");

            await AuthorizeViewAsync(task, callerUserId, callerRoleId);
            var updated = await _myTaskRepository.UpdateTaskStatusAsync(task, request.Status);

            return _mapper.Map<GetTaskResponse>(updated);
        }

        public async Task<TaskCommentResponse> AddCommentAsync(int taskId, AddTaskCommentRequest request, int callerUserId, int callerRoleId)
        {
            var task = await _myTaskRepository.GetTaskWithDetailsAsync(taskId)
                ?? throw new NotFoundException($"Task with id '{taskId}' was not found.");

            await AuthorizeViewAsync(task, callerUserId, callerRoleId);

            var comment = new TaskComment
            {
                TaskId  = taskId,
                UserId  = callerUserId,
                Comment = request.Comment.Trim()
            };

            var saved = await _myTaskRepository.AddCommentAsync(comment);
            return _mapper.Map<TaskCommentResponse>(saved);
        }

        public async Task<TaskCommentResponse> UpdateCommentAsync(int commentId, UpdateTaskCommentRequest request, int callerUserId, int callerRoleId)
        {
            var comment = await _myTaskRepository.GetCommentWithDetailsAsync(commentId)
                ?? throw new NotFoundException($"Comment with id '{commentId}' was not found.");

            if (comment.Task == null)
                throw new NotFoundException($"Task for comment with id '{commentId}' was not found.");

            await AuthorizeViewAsync(comment.Task, callerUserId, callerRoleId);

            if (comment.UserId != callerUserId)
                throw new ForbiddenException("You can only edit your own comments.");

            comment.Comment = request.Comment.Trim();
            var updated = await _myTaskRepository.UpdateCommentAsync(comment);
            return _mapper.Map<TaskCommentResponse>(updated);
        }

        public async Task<bool> DeleteCommentAsync(int commentId, int callerUserId, int callerRoleId)
        {
            var comment = await _myTaskRepository.GetCommentWithDetailsAsync(commentId)
                ?? throw new NotFoundException($"Comment with id '{commentId}' was not found.");

            if (comment.Task == null)
                throw new NotFoundException($"Task for comment with id '{commentId}' was not found.");

            await AuthorizeViewAsync(comment.Task, callerUserId, callerRoleId);

            if (comment.UserId != callerUserId)
                throw new ForbiddenException("You can only delete your own comments.");

            return await _myTaskRepository.SoftDeleteCommentAsync(commentId);
        }

        public async Task<IEnumerable<TaskCommentResponse>> GetCommentsAsync(int taskId, int callerUserId, int callerRoleId)
        {
            var task = await _myTaskRepository.GetTaskWithDetailsAsync(taskId)
                ?? throw new NotFoundException($"Task with id '{taskId}' was not found.");

            await AuthorizeViewAsync(task, callerUserId, callerRoleId);

            var comments = await _myTaskRepository.GetCommentsByTaskAsync(taskId);
            return _mapper.Map<IEnumerable<TaskCommentResponse>>(comments);
        }

        public async Task<IEnumerable<TaskAttachmentResponse>> AddAttachmentsAsync(int taskId, IList<IFormFile> files, int callerUserId, int callerRoleId)
        {
            if (files == null || files.Count == 0)
                throw new ArgumentException("No files provided.");

            var task = await _myTaskRepository.GetTaskWithDetailsAsync(taskId)
                ?? throw new NotFoundException($"Task with id '{taskId}' was not found.");

            await AuthorizeViewAsync(task, callerUserId, callerRoleId);

            const string subFolder = "Tasks";
            var attachments = new List<TaskAttachment>();

            foreach (var file in files)
            {
                var storedFileName = await _fileService.SaveAttachmentAsync(file, subFolder);
                attachments.Add(new TaskAttachment
                {
                    TaskId   = taskId,
                    UserId   = callerUserId,
                    FileName = file.FileName,
                    FileUrl  = storedFileName,
                    FileSize = file.Length
                });
            }

            var saved = await _myTaskRepository.AddAttachmentsAsync(attachments);
            return _mapper.Map<IEnumerable<TaskAttachmentResponse>>(saved);
        }

        public async Task<bool> DeleteAttachmentAsync(int attachmentId, int callerUserId, int callerRoleId)
        {
            var attachment = await _myTaskRepository.GetAttachmentWithTaskAsync(attachmentId)
                ?? throw new NotFoundException($"Attachment with id '{attachmentId}' was not found.");

            if (attachment.Task == null)
                throw new NotFoundException($"Task for attachment with id '{attachmentId}' was not found.");

            await AuthorizeViewAsync(attachment.Task, callerUserId, callerRoleId);

            if (attachment.UserId != callerUserId)
                throw new ForbiddenException("You can only delete your own attachments.");

            await _myTaskRepository.SoftDeleteAttachmentAsync(attachmentId);

            const string subFolder = "Tasks";
            var filePath = Path.Combine(_fileService.BaseAttachmentPath, subFolder, attachment.FileUrl);
            if (File.Exists(filePath))
                File.Delete(filePath);

            return true;
        }

        private async Task AuthorizeViewAsync(Tasks task, int callerUserId, int callerRoleId)
        {
            if (callerRoleId == 1) return;

            if (callerRoleId == 2)
            {
                var myProjectIds = await _myTaskRepository.GetTeamLeadProjectIdsAsync(callerUserId);
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