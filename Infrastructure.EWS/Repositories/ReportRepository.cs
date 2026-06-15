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
                .Take(5)
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

        public async Task<PagedResponse<EmployeeTaskSummaryResponse>> GetEmployeeSummaryAsync(EmployeeSummaryRequest request)
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
                    var total = tg?.Total ?? 0;
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

        public async Task<TaskCompletionOverviewResponse> GetTaskCompletionOverviewAsync(TaskCompletionReportRequest request)
        {
            var fromDate = request.Filter?.ToLower() == "weekly"
                ? DateTime.UtcNow.AddDays(-7)
                : DateTime.UtcNow.AddDays(-30);

            var dist = await _context.Tasks
                .Where(t => !t.IsDeleted && t.CreatedAt >= fromDate)
                .GroupBy(_ => 1)
                .Select(g => new
                {
                    StatusPending = g.Count(t => t.TaskStatus == TaskStatuses.Pending),
                    StatusInProgress = g.Count(t => t.TaskStatus == TaskStatuses.InProgress),
                    StatusCompleted = g.Count(t => t.TaskStatus == TaskStatuses.Completed),
                    StatusOnHold = g.Count(t => t.TaskStatus == TaskStatuses.OnHold),
                    PriorityLow = g.Count(t => t.Priority == TaskPriority.Low),
                    PriorityMedium = g.Count(t => t.Priority == TaskPriority.Medium),
                    PriorityHigh = g.Count(t => t.Priority == TaskPriority.High),
                    Total = g.Count(),
                })
                .AsNoTracking()
                .FirstOrDefaultAsync();

            return new TaskCompletionOverviewResponse
            {
                StatusDistribution = new TaskStatusDistributionResponse
                {
                    Pending = dist?.StatusPending ?? 0,
                    InProgress = dist?.StatusInProgress ?? 0,
                    Completed = dist?.StatusCompleted ?? 0,
                    OnHold = dist?.StatusOnHold ?? 0,
                    Total = dist?.Total ?? 0,
                },
                PriorityDistribution = new TaskPriorityDistributionResponse
                {
                    Low = dist?.PriorityLow ?? 0,
                    Medium = dist?.PriorityMedium ?? 0,
                    High = dist?.PriorityHigh ?? 0,
                    Total = dist?.Total ?? 0,
                },
            };
        }

        public async Task<PagedResponse<TaskCompletionSummaryItemResponse>> GetTaskCompletionSummaryAsync(TaskCompletionSummaryRequest request)
        {
            var query = _context.Tasks
                .Where(t => !t.IsDeleted)
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
            var dist = await _context.Projects
                .Where(p => !p.IsDeleted)
                .GroupBy(_ => 1)
                .Select(g => new
                {
                    Active = g.Count(p => p.ProjectStatus == ProjectStatus.Active),
                    Completed = g.Count(p => p.ProjectStatus == ProjectStatus.Completed),
                    Total = g.Count(),
                })
                .AsNoTracking()
                .FirstOrDefaultAsync();

            return new ProjectProgressOverviewResponse
            {
                StatusDistribution = new ProjectStatusDistributionResponse
                {
                    Active = dist?.Active ?? 0,
                    Completed = dist?.Completed ?? 0,
                    Total = dist?.Total ?? 0,
                },
            };
        }

       public async Task<PagedResponse<ProjectProgressSummaryResponse>> GetProjectProgressSummaryAsync(ProjectProgressRequest request)
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

            var rawItems = await projectQuery
                .OrderBy(p => p.Id)
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(p => new
                {
                    p.Id,
                    p.Name,
                    TotalTasks = _context.Tasks.Count(t => t.ProjectId == p.Id && !t.IsDeleted),
                    CompletedTasks = _context.Tasks.Count(t => t.ProjectId == p.Id && !t.IsDeleted && t.TaskStatus == TaskStatuses.Completed),
                    InProgressTasks = _context.Tasks.Count(t => t.ProjectId == p.Id && !t.IsDeleted && t.TaskStatus == TaskStatuses.InProgress),
                    PendingTasks = _context.Tasks.Count(t => t.ProjectId == p.Id && !t.IsDeleted && t.TaskStatus == TaskStatuses.Pending),
                    OnHoldTasks = _context.Tasks.Count(t => t.ProjectId == p.Id && !t.IsDeleted && t.TaskStatus == TaskStatuses.OnHold),
                })
                .AsNoTracking()
                .ToListAsync();

            var items = rawItems
                .Select(p => new ProjectProgressSummaryResponse
                {
                    ProjectId = p.Id,
                    ProjectName = p.Name,
                    TotalTasks = p.TotalTasks,
                    CompletedTasks = p.CompletedTasks,
                    InProgressTasks = p.InProgressTasks,
                    PendingTasks = p.PendingTasks,
                    OnHoldTasks = p.OnHoldTasks,
                    ProgressPercentage = p.TotalTasks > 0
                        ? Math.Round((double)p.CompletedTasks / p.TotalTasks * 100, 1)
                        : 0,
                })
                .OrderByDescending(p => p.ProgressPercentage)
                .ToList();

            return PagedResponse<ProjectProgressSummaryResponse>.Create(
                items, totalCount, request.PageNumber, request.PageSize);
        }
    }
}