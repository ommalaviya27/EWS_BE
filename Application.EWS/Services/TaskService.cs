using AutoMapper;
using Application.EWS.Interfaces;
using Domain.EWS.DataModels.Request.Tasks;
using Domain.EWS.DataModels.Response.Project;
using Domain.EWS.DataModels.Response.Tasks;
using Domain.EWS.DataModels.Response.User;
using Domain.EWS.Interface;
using Shared.EWS.DataModel.Response;
using Shared.EWS.Entities;
using Shared.EWS.Enums;
using Shared.EWS.Exceptions;
using Shared.EWS.Interfaces.Services;
using Shared.EWS.Services;
using System.Security.Claims;

namespace Application.EWS.Services
{
    public class TaskService(
        ITaskRepository repository,
        IFileService fileService,
        IMapper mapper,
        ClaimsPrincipal principal)
        : GenericService<Tasks>(repository, principal), ITaskService
    {
        private readonly ITaskRepository _taskRepository = repository;
        private readonly IFileService _fileService = fileService;
        private readonly IMapper _mapper = mapper;

        public async Task<PagedResponse<GetTaskResponse>> GetAllTasksAsync(TaskSearchRequest request, Guid? projectId)
        {
            List<Guid>? projectIdFilter = null;
            int? assignedToFilter = null;

            if (CurrentRoleId == 2)
                projectIdFilter = await _taskRepository.GetTeamLeadProjectIdsAsync(CurrentUserId);
            else if (CurrentRoleId == 3)
                assignedToFilter = CurrentUserId;

            var paged = await _taskRepository.GetAllTasksWithDetailsAsync(request, projectId, assignedToFilter, projectIdFilter);
            var mapped = paged.Items.Select(t => _mapper.Map<GetTaskResponse>(t)).ToList();

            return new PagedResponse<GetTaskResponse>
            {
                Items = mapped,
                TotalCount = paged.TotalCount,
                PageNumber = paged.PageNumber,
                PageSize = paged.PageSize
            };
        }

        public async Task<GetTaskResponse?> GetTaskByIdAsync(int id)
        {
            var task = await _taskRepository.GetTaskWithDetailsAsync(id)
                ?? throw new NotFoundException($"Task with id '{id}' was not found.");

            await AuthorizeViewAsync(task);
            return _mapper.Map<GetTaskResponse>(task);
        }

        public async Task<GetTaskResponse> CreateTaskAsync(CreateTaskRequest request)
        {
            ValidateTeamLeadOrAdmin("create");

            var project = await _taskRepository.GetProjectByIdAsync(request.ProjectId)
                ?? throw new NotFoundException($"Project with id '{request.ProjectId}' was not found.");

            if (CurrentRoleId == 2 && project.UserId != CurrentUserId)
                throw new ForbiddenException("You can only create tasks for projects assigned to you.");

            if (project.ProjectStatus == ProjectStatus.Completed)
                throw new InvalidOperationException("Tasks cannot be created as project is completed.");

            await ValidateAssigneeAsync(request.AssignedToUserId);

            if (request.DueDate <= DateTime.UtcNow)
                throw new InvalidOperationException("Due date must be in the future.");

            var isDuplicate = await _taskRepository.IsDuplicateTaskAsync(request.ProjectId, request.Title);
            if (isDuplicate)
                throw new InvalidOperationException(
                    $"A task with the title '{request.Title.Trim()}' already exists in this project. Please use a unique task title.");

            var entity = new Tasks
            {
                Title = request.Title.Trim(),
                Description = request.Description.Trim(),
                ProjectId = request.ProjectId,
                AssignedToUserId = request.AssignedToUserId,
                AssignedByUserId = CurrentUserId,
                TaskStatus = TaskStatuses.Pending,
                Priority = request.Priority,
                DueDate = request.DueDate
            };

            var created = await AddAsync(entity);
            return await GetTaskByIdAsync(created.Id)
                   ?? throw new InvalidOperationException("Failed to retrieve newly created task.");
        }

        public async Task<GetTaskResponse> UpdateTaskAsync(int id, UpdateTaskRequest request)
        {
            ValidateTeamLeadOrAdmin("update");

            var task = await GetByIdAsync(id)
                ?? throw new NotFoundException($"Task with id '{id}' was not found.");

            if (CurrentRoleId == 2)
            {
                var project = await _taskRepository.GetProjectByIdAsync(task.ProjectId)
                    ?? throw new NotFoundException("Associated project not found.");

                if (project.UserId != CurrentUserId)
                    throw new ForbiddenException("You can only update tasks in projects assigned to you.");
            }

            await ValidateAssigneeAsync(request.AssignedToUserId);

            var isDuplicate = await _taskRepository.IsDuplicateTaskAsync(task.ProjectId, request.Title, id);
            if (isDuplicate)
                throw new InvalidOperationException(
                    $"A task with the title '{request.Title.Trim()}' already exists in this project. Please use a unique task title.");

            task.Title = request.Title.Trim();
            task.Description = request.Description.Trim();
            task.AssignedToUserId = request.AssignedToUserId;
            task.TaskStatus = request.Status;
            task.Priority = request.Priority;
            task.DueDate = request.DueDate;

            var updated = await UpdateAsync(task);
            return await GetTaskByIdAsync(updated.Id)
                   ?? throw new InvalidOperationException("Failed to retrieve updated task.");
        }

        public async Task<bool> DeleteTaskAsync(int id)
        {
            ValidateTeamLeadOrAdmin("delete");

            var task = await GetByIdAsync(id)
                ?? throw new NotFoundException($"Task with id '{id}' was not found.");

            if (CurrentRoleId == 2)
            {
                var project = await _taskRepository.GetProjectByIdAsync(task.ProjectId)
                    ?? throw new NotFoundException("Associated project not found.");

                if (project.UserId != CurrentUserId)
                    throw new ForbiddenException("You can only delete tasks in projects assigned to you.");
            }

            return await DeleteAsync(task.Id);
        }

        public async Task<IEnumerable<GetUserResponse>> GetTeamMembersAsync()
        {
            ValidateTeamLeadOrAdmin("view team members for");
            var users = await _taskRepository.GetTeamMembersAsync(CurrentUserId);
            return _mapper.Map<IEnumerable<GetUserResponse>>(users);
        }

        public async Task<PagedResponse<GetProjectResponse>> GetMyProjectsAsync(ProjectListRequest request)
            => await _taskRepository.GetProjectsByUserIdAsync(CurrentUserId, request);

        public async Task<TeamLeadDashboardResponse> GetTeamLeadDashboardAsync()
        {
            if (CurrentRoleId != 2)
                throw new ForbiddenException("Only Team Leads can access the team lead dashboard.");

            var taskCounts = await _taskRepository.GetTeamTaskCountsAsync(CurrentUserId);
            var activeProjectCount = await _taskRepository.GetActiveProjectCountAsync(CurrentUserId);

            var activeProjects = await _taskRepository.GetActiveProjectsByTaskCountAsync(CurrentUserId, 5);
            var completedProjects = await _taskRepository.GetRecentlyCompletedProjectsAsync(CurrentUserId, 5);
            var overdueTasks = await _taskRepository.GetOverdueTeamTasksAsync(CurrentUserId, 5);
            var recentTeamTasks = await _taskRepository.GetRecentTeamTasksAsync(CurrentUserId, 5);

            return new TeamLeadDashboardResponse
            {
                MyTeamTaskCount = taskCounts.MyTeamTaskCount,
                OverdueTaskCount = taskCounts.OverdueTaskCount,
                ActiveProjectCount = activeProjectCount,
                ActiveProjects = activeProjects,
                CompletedProjects = completedProjects,
                OverdueTasks = overdueTasks,
                RecentTeamTasks = recentTeamTasks,
            };
        }

        private void ValidateTeamLeadOrAdmin(string action)
        {
            if (CurrentRoleId != 1 && CurrentRoleId != 2)
                throw new ForbiddenException($"Access denied. Only a Team Lead or Admin can {action} a task.");
        }

        private async Task AuthorizeViewAsync(Tasks task)
        {
            if (CurrentRoleId == 1) return;

            if (CurrentRoleId == 2)
            {
                var myProjectIds = await _taskRepository.GetTeamLeadProjectIdsAsync(CurrentUserId);
                if (!myProjectIds.Contains(task.ProjectId))
                    throw new ForbiddenException("You do not have access to this task.");
            }
            else if (CurrentRoleId == 3)
            {
                if (task.AssignedToUserId != CurrentUserId)
                    throw new ForbiddenException("You do not have access to this task.");
            }
        }

        private async Task ValidateAssigneeAsync(int assigneeUserId)
        {
            var assignee = await _taskRepository.GetAssigneeAsync(assigneeUserId)
                ?? throw new NotFoundException($"User with id '{assigneeUserId}' was not found.");

            if (!assignee.status)
                throw new InvalidOperationException("Cannot assign a task to an inactive user.");

            if (assignee.RoleId != 3)
                throw new InvalidOperationException("Tasks can only be assigned to employees.");

            if (CurrentRoleId == 2 && assignee.TeamLeadId != CurrentUserId)
                throw new ForbiddenException("You can only assign tasks to employees under your team.");
        }
    }
}