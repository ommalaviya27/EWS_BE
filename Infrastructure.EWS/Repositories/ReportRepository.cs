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

            var summaries = await BuildSummariesAsync(fromDate);

            var topEmployees = summaries
                .OrderByDescending(e => e.Completed)
                .ThenByDescending(e => e.TotalAssigned)
                .Take(TopEmployeesListSize)
                .Select(e => new TopEmployeeTaskResponse
                {
                    UserId = e.UserId,
                    EmployeeName = e.EmployeeName,
                    Completed = e.Completed,
                    InProgress = e.InProgress,
                    OnHold = e.OnHold,
                    Pending = e.Pending,
                    Total = e.TotalAssigned,
                })
                .ToList();

            return new EmployeePerformanceReportResponse { TopEmployees = topEmployees };
        }

        public async Task<PagedResponse<EmployeeTaskSummaryResponse>> GetEmployeeSummaryAsync(
            EmployeeSummaryRequest request)
        {
            var summaries = await BuildSummariesAsync(fromDate: null);

            // Search by name or email
            var search = request.Search?.Trim().ToLower();
            if (!string.IsNullOrEmpty(search))
            {
                summaries = summaries
                    .Where(e => e.EmployeeName.ToLower().Contains(search)
                             || e.Email.ToLower().Contains(search))
                    .ToList();
            }

            var totalCount = summaries.Count;
            var items = summaries
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToList();

            return PagedResponse<EmployeeTaskSummaryResponse>.Create(
                items, totalCount, request.PageNumber, request.PageSize);
        }

        private async Task<List<EmployeeTaskSummaryResponse>> BuildSummariesAsync(DateTime? fromDate)
        {
            var employees = await _context.Users
                .Where(u => u.RoleId == 3 && !u.IsDeleted)
                .Select(u => new { u.Id, u.Name, u.Email })
                .AsNoTracking()
                .ToListAsync();

            if (employees.Count == 0) return [];

            var employeeIds = employees.Select(e => e.Id).ToList();

            var taskQuery = _context.Tasks
                .Where(t => !t.IsDeleted && employeeIds.Contains(t.AssignedToUserId));

            if (fromDate.HasValue)
                taskQuery = taskQuery.Where(t => t.CreatedAt >= fromDate.Value);

            var tasks = await taskQuery
                .Select(t => new { t.AssignedToUserId, t.TaskStatus })
                .AsNoTracking()
                .ToListAsync();

            var tasksByEmployee = tasks
                .GroupBy(t => t.AssignedToUserId)
                .ToDictionary(g => g.Key, g => g.ToList());

            return employees
                .Select(emp =>
                {
                    var empTasks = tasksByEmployee.TryGetValue(emp.Id, out var tList) ? tList : [];
                    var completed = empTasks.Count(t => t.TaskStatus == TaskStatuses.Completed);
                    var inProgress = empTasks.Count(t => t.TaskStatus == TaskStatuses.InProgress);
                    var onHold = empTasks.Count(t => t.TaskStatus == TaskStatuses.OnHold);
                    var pending = empTasks.Count(t => t.TaskStatus == TaskStatuses.Pending);
                    var total = empTasks.Count;

                    return new EmployeeTaskSummaryResponse
                    {
                        UserId = emp.Id,
                        EmployeeName = emp.Name,
                        Email = emp.Email,
                        TotalAssigned = total,
                        Completed = completed,
                        InProgress = inProgress,
                        Pending = pending,
                        OnHold = onHold,
                        CompletionRate = total > 0 ? Math.Round((double)completed / total * 100, 1) : 0,
                    };
                })
                .OrderByDescending(e => e.CompletionRate)
                .ThenByDescending(e => e.TotalAssigned)
                .ToList();
        }
    }
}