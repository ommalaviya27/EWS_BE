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
        IProjectRepository projectRepository,
        IMapper mapper,
        ClaimsPrincipal principal)
        : GenericService<Tasks>(repository, principal), ITaskService
    {
        private readonly ITaskRepository _taskRepository = repository;
        private readonly IFileService _fileService = fileService;
        private readonly IProjectRepository _projectRepository = projectRepository;
        private readonly IMapper _mapper = mapper;

        public async Task<PagedResponse<GetTaskResponse>> GetAllTasksAsync(
            TaskSearchRequest request,
            Guid? projectId)
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
                Items      = mapped,
                TotalCount = paged.TotalCount,
                PageNumber = paged.PageNumber,
                PageSize   = paged.PageSize
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

            var entity = new Tasks
            {
                Title            = request.Title.Trim(),
                Description      = request.Description.Trim(),
                ProjectId        = request.ProjectId,
                AssignedToUserId = request.AssignedToUserId,
                AssignedByUserId = CurrentUserId,
                TaskStatus       = TaskStatuses.Pending,
                Priority         = request.Priority,
                DueDate          = request.DueDate
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

            task.Title           = request.Title.Trim();
            task.Description     = request.Description.Trim();
            task.AssignedToUserId= request.AssignedToUserId;
            task.TaskStatus      = request.Status;
            task.Priority        = request.Priority;
            task.DueDate         = request.DueDate;

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

        public async Task<IEnumerable<GetProjectResponse>> GetMyProjectsAsync()
        {
            var projects = await _taskRepository.GetProjectsByUserIdAsync(CurrentUserId);
            return _mapper.Map<IEnumerable<GetProjectResponse>>(projects);
        }

        public async Task<TeamLeadDashboardResponse> GetTeamLeadDashboardAsync()
        {
            if (CurrentRoleId != 2)
                throw new ForbiddenException("Only Team Leads can access the team lead dashboard.");

            var now = DateTime.UtcNow;

            var allTeamTasks = await _taskRepository.GetAllTeamTasksAsync(CurrentUserId);
            var myTeamTaskCount  = allTeamTasks.Count;
            var overdueTaskCount = allTeamTasks
                .Count(t => t.DueDate < now && t.TaskStatus != TaskStatuses.Completed);

            var myProjects       = (await _taskRepository.GetProjectsByUserIdAsync(CurrentUserId)).ToList();
            var activeProjectCount = myProjects.Count(p => p.ProjectStatus == ProjectStatus.Active);

            // Top 5 active projects ordered by task count descending
            var activeProjectsWithCount = await _taskRepository
                .GetActiveProjectsByTaskCountAsync(CurrentUserId, 5);

            var activeProjectCards = activeProjectsWithCount
                .Select(x => new GetProjectResponse
                {
                    Id          = x.Project.Id,
                    Name        = x.Project.Name,
                    Description = x.Project.Description,
                    UserId      = x.Project.UserId,
                    StartDate   = x.Project.StartDate,
                    EndDate     = x.Project.EndDate,
                    TaskCount   = x.TaskCount
                })
                .ToList();

            // Top 5 recently completed projects (no status field needed)
            var completedProjectEntities = await _taskRepository
                .GetRecentlyCompletedProjectsAsync(CurrentUserId, 5);

            var completedProjectCards = completedProjectEntities
                .Select(p => new GetProjectResponse
                {
                    Id          = p.Id,
                    Name        = p.Name,
                    Description = p.Description,
                    UserId      = p.UserId,
                    StartDate   = p.StartDate,
                    EndDate     = p.EndDate
                })
                .ToList();

            // Top 5 overdue tasks (most overdue first)
            var overdueTaskEntities = await _taskRepository.GetOverdueTeamTasksAsync(CurrentUserId, 5);
            var overdueTaskCards = overdueTaskEntities
                .Select(MapToTaskResponse)
                .ToList();

            // Top 5 high-priority tasks ordered by nearest due date
            var highPriorityTaskEntities = await _taskRepository
                .GetHighPriorityTeamTasksByDueDateAsync(CurrentUserId, 5);

            var recentTeamTaskCards = highPriorityTaskEntities
                .Select(MapToTaskResponse)
                .ToList();

            return new TeamLeadDashboardResponse
            {
                MyTeamTaskCount    = myTeamTaskCount,
                OverdueTaskCount   = overdueTaskCount,
                ActiveProjectCount = activeProjectCount,
                ActiveProjects     = activeProjectCards,
                CompletedProjects  = completedProjectCards,
                OverdueTasks       = overdueTaskCards,
                RecentTeamTasks    = recentTeamTaskCards,
            };
        }

        private static GetTaskResponse MapToTaskResponse(Tasks t) =>
            new()
            {
                Id                 = t.Id,
                Title              = t.Title,
                Description        = t.Description,
                ProjectId          = t.ProjectId,
                ProjectName        = t.Project?.Name ?? string.Empty,
                AssignedToUserId   = t.AssignedToUserId,
                AssignedToUserName = t.AssignedTo?.Name ?? string.Empty,
                AssignedByUserId   = t.AssignedByUserId,
                AssignedByUserName = t.AssignedBy?.Name ?? string.Empty,
                TaskStatus         = t.TaskStatus,
                Priority           = t.Priority,
                DueDate            = t.DueDate
            };

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