using Domain.EWS.DataModels.Request.Reports;
using Domain.EWS.DataModels.Response.Reports;
using Domain.EWS.Interface;
using Microsoft.EntityFrameworkCore;
using Shared.EWS.Data;
using Shared.EWS.DataModel.Response;
using Shared.EWS.Entities;
using Shared.EWS.Enums;

namespace Infrastructure.EWS.Repositories
{
    public class ReportRepository(EWSDbContext context) : GenericRepository<User>(context), IReportRepository
    {
        private const int TopEmployeesListSize = 5;

        public async Task<EmployeePerformanceReportResponse> GetEmployeePerformanceReportAsync(
            EmployeePerformanceReportRequest request)
        {
            var fromDate = request.Filter?.ToLower() == "weekly"
                ? DateTime.UtcNow.AddDays(-7)
                : DateTime.UtcNow.AddDays(-30);

            var taskGroups = await _context.Tasks
                .Where(t => !t.IsDeleted && t.CreatedAt >= fromDate)
                .GroupBy(t => t.AssignedToUserId)
                .Select(g => new
                {
                    UserId = g.Key,
                    Completed = g.Count(t => t.TaskStatus == TaskStatuses.Completed),
                    InProgress = g.Count(t => t.TaskStatus == TaskStatuses.InProgress),
                    OnHold = g.Count(t => t.TaskStatus == TaskStatuses.OnHold),
                    Pending = g.Count(t => t.TaskStatus == TaskStatuses.Pending),
                    Total = g.Count(),
                })
                .AsNoTracking()
                .ToListAsync();

            if (taskGroups.Count == 0)
                return new EmployeePerformanceReportResponse();

            var employeeIds = taskGroups.Select(g => g.UserId).ToList();

            var employeeNames = await _context.Users
                .Where(u => u.RoleId == 3 && !u.IsDeleted && employeeIds.Contains(u.Id))
                .Select(u => new { u.Id, u.Name })
                .AsNoTracking()
                .ToDictionaryAsync(u => u.Id, u => u.Name);

            var topEmployees = taskGroups
                .Where(g => employeeNames.ContainsKey(g.UserId))
                .OrderByDescending(g => g.Completed)
                .ThenByDescending(g => g.Total)
                .Take(TopEmployeesListSize)
                .Select(g => new TopEmployeeTaskResponse
                {
                    UserId = g.UserId,
                    EmployeeName = employeeNames[g.UserId],
                    Completed = g.Completed,
                    InProgress = g.InProgress,
                    OnHold = g.OnHold,
                    Pending = g.Pending,
                    Total = g.Total,
                })
                .ToList();

            return new EmployeePerformanceReportResponse { TopEmployees = topEmployees };
        }

        public async Task<PagedResponse<EmployeeTaskSummaryResponse>> GetEmployeeSummaryAsync(
            EmployeeSummaryRequest request)
        {
            var employeeQuery = _context.Users
                .Where(u => u.RoleId == 3 && !u.IsDeleted)
                .AsNoTracking();

            var search = request.Search?.Trim().ToLower();

            if (!string.IsNullOrEmpty(search))
                employeeQuery = employeeQuery.Where(u =>
                    u.Name.ToLower().Contains(search) ||
                    u.Email.ToLower().Contains(search));

            var totalCount = await employeeQuery.CountAsync();

            var employees = await employeeQuery
                .OrderBy(u => u.Id)
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(u => new { u.Id, u.Name, u.Email })
                .ToListAsync();

            if (employees.Count == 0)
                return PagedResponse<EmployeeTaskSummaryResponse>.Create(
                    [], totalCount, request.PageNumber, request.PageSize);

            var employeeIds = employees.Select(e => e.Id).ToList();

            var taskGroups = await _context.Tasks
                .Where(t => !t.IsDeleted && employeeIds.Contains(t.AssignedToUserId))
                .GroupBy(t => t.AssignedToUserId)
                .Select(g => new
                {
                    UserId = g.Key,
                    Total = g.Count(),
                    Completed = g.Count(t => t.TaskStatus == TaskStatuses.Completed),
                    InProgress = g.Count(t => t.TaskStatus == TaskStatuses.InProgress),
                    Pending = g.Count(t => t.TaskStatus == TaskStatuses.Pending),
                    OnHold = g.Count(t => t.TaskStatus == TaskStatuses.OnHold),
                })
                .AsNoTracking()
                .ToDictionaryAsync(g => g.UserId);

            var items = employees
                .Select(emp =>
                {
                    taskGroups.TryGetValue(emp.Id, out var tg);
                    var total     = tg?.Total ?? 0;
                    var completed = tg?.Completed ?? 0;

                    return new EmployeeTaskSummaryResponse
                    {
                        UserId = emp.Id,
                        EmployeeName = emp.Name,
                        Email = emp.Email,
                        TotalAssigned = total,
                        Completed = completed,
                        InProgress = tg?.InProgress ?? 0,
                        Pending  = tg?.Pending ?? 0,
                        OnHold = tg?.OnHold ?? 0,
                        CompletionRate = total > 0
                            ? Math.Round((double)completed / total * 100, 1)
                            : 0,
                    };
                })
                .OrderByDescending(e => e.CompletionRate)
                .ThenByDescending(e => e.TotalAssigned)
                .ToList();

            return PagedResponse<EmployeeTaskSummaryResponse>.Create(
                items, totalCount, request.PageNumber, request.PageSize);
        }

        public async Task<TaskCompletionOverviewResponse> GetTaskCompletionOverviewAsync(
            TaskCompletionReportRequest request)
        {
            var fromDate = request.Filter?.ToLower() == "weekly"
                ? DateTime.UtcNow.AddDays(-7)
                : DateTime.UtcNow.AddDays(-30);

            var tasks = await _context.Tasks
                .Where(t => !t.IsDeleted && t.CreatedAt >= fromDate)
                .Select(t => new { t.TaskStatus, t.Priority })
                .AsNoTracking()
                .ToListAsync();

            var statusDist = new TaskStatusDistributionResponse
            {
                Pending = tasks.Count(t => t.TaskStatus == TaskStatuses.Pending),
                InProgress = tasks.Count(t => t.TaskStatus == TaskStatuses.InProgress),
                Completed = tasks.Count(t => t.TaskStatus == TaskStatuses.Completed),
                OnHold = tasks.Count(t => t.TaskStatus == TaskStatuses.OnHold),
                Total = tasks.Count,
            };

            var priorityDist = new TaskPriorityDistributionResponse
            {
                Low = tasks.Count(t => t.Priority == TaskPriority.Low),
                Medium = tasks.Count(t => t.Priority == TaskPriority.Medium),
                High = tasks.Count(t => t.Priority == TaskPriority.High),
                Total = tasks.Count,
            };

            return new TaskCompletionOverviewResponse
            {
                StatusDistribution   = statusDist,
                PriorityDistribution = priorityDist,
            };
        }

        public async Task<PagedResponse<TaskCompletionSummaryItemResponse>> GetTaskCompletionSummaryAsync(
            TaskCompletionSummaryRequest request)
        {
            var query = _context.Tasks
                .Where(t => !t.IsDeleted)
                .Include(t => t.AssignedTo)
                .Include(t => t.Project)
                .AsNoTracking();

            var search = request.Search?.Trim().ToLower();
            if (!string.IsNullOrEmpty(search))
                query = query.Where(t =>
                    t.Title.ToLower().Contains(search) ||
                    (t.AssignedTo != null && t.AssignedTo.Name.ToLower().Contains(search)));

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderByDescending(t => t.CreatedAt)
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(t => new TaskCompletionSummaryItemResponse
                {
                    TaskId = t.Id,
                    Title = t.Title,
                    AssignedTo = t.AssignedTo != null ? t.AssignedTo.Name : string.Empty,
                    Priority = t.Priority.ToString(),
                    Status = t.TaskStatus.ToString(),
                    DueDate = t.DueDate,
                    ProjectName = t.Project != null ? t.Project.Name : string.Empty,
                })
                .ToListAsync();

            return PagedResponse<TaskCompletionSummaryItemResponse>.Create(
                items, totalCount, request.PageNumber, request.PageSize);
        }

        public async Task<ProjectProgressOverviewResponse> GetProjectProgressOverviewAsync()
        {
            var projects = await _context.Projects
                .Where(p => !p.IsDeleted)
                .Select(p => new { p.ProjectStatus })
                .AsNoTracking()
                .ToListAsync();

            return new ProjectProgressOverviewResponse
            {
                StatusDistribution = new ProjectStatusDistributionResponse
                {
                    Active = projects.Count(p => p.ProjectStatus == ProjectStatus.Active),
                    Completed = projects.Count(p => p.ProjectStatus == ProjectStatus.Completed),
                    Total = projects.Count,
                },
            };
        }

        public async Task<PagedResponse<ProjectProgressSummaryResponse>> GetProjectProgressSummaryAsync(
            ProjectProgressRequest request)
        {
            var projectQuery = _context.Projects
                .Where(p => !p.IsDeleted && p.ProjectStatus == ProjectStatus.Active)
                .AsNoTracking();

            var search = request.Search?.Trim().ToLower();

            if (!string.IsNullOrEmpty(search))
                projectQuery = projectQuery.Where(p => p.Name.ToLower().Contains(search));

            var totalCount = await projectQuery.CountAsync();

            if (totalCount == 0)
                return PagedResponse<ProjectProgressSummaryResponse>.Create(
                    [], totalCount, request.PageNumber, request.PageSize);

            var allProjects = await projectQuery
                .Select(p => new { p.Id, p.Name })
                .ToListAsync();

            var allProjectIds = allProjects.Select(p => p.Id).ToList();

            var taskGroups = await _context.Tasks
                .Where(t => !t.IsDeleted && allProjectIds.Contains(t.ProjectId))
                .GroupBy(t => t.ProjectId)
                .Select(g => new
                {
                    ProjectId = g.Key,
                    Total = g.Count(),
                    Completed = g.Count(t => t.TaskStatus == TaskStatuses.Completed),
                    InProgress = g.Count(t => t.TaskStatus == TaskStatuses.InProgress),
                    Pending = g.Count(t => t.TaskStatus == TaskStatuses.Pending),
                    OnHold = g.Count(t => t.TaskStatus == TaskStatuses.OnHold),
                })
                .AsNoTracking()
                .ToDictionaryAsync(g => g.ProjectId);

            var items = allProjects
                .Select(p =>
                {
                    taskGroups.TryGetValue(p.Id, out var tg);
                    var total     = tg?.Total ?? 0;
                    var completed = tg?.Completed ?? 0;

                    return new ProjectProgressSummaryResponse
                    {
                        ProjectId = p.Id,
                        ProjectName = p.Name,
                        TotalTasks = total,
                        CompletedTasks = completed,
                        InProgressTasks = tg?.InProgress ?? 0,
                        PendingTasks = tg?.Pending ?? 0,
                        OnHoldTasks = tg?.OnHold ?? 0,
                        ProgressPercentage = total > 0
                            ? Math.Round((double)completed / total * 100, 1)
                            : 0,
                    };
                })
                .OrderByDescending(p => p.ProgressPercentage)
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToList();

            return PagedResponse<ProjectProgressSummaryResponse>.Create(
                items, totalCount, request.PageNumber, request.PageSize);
        }
    }
}