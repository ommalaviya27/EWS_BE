using Domain.EWS.DataModels.Response.Admin;
using Domain.EWS.DataModels.Response.Project;
using Domain.EWS.DataModels.Response.Tasks;
using Domain.EWS.Interface;
using Microsoft.EntityFrameworkCore;
using Shared.EWS.Data;
using Shared.EWS.Entities;
using Shared.EWS.Enums;

namespace Infrastructure.EWS.Repositories
{
    public class AdminDashboardRepository(EWSDbContext context)
        : GenericRepository<User>(context), IAdminDashboardRepository
    {
        public async Task<AdminDashboardResponse> GetDashboardAsync()
        {
            var now = DateTime.UtcNow;

            var totalEmployees = await _context.Users
                .CountAsync(u => u.RoleId == 3 && !u.IsDeleted);

            var totalProjects = await _context.Projects
                .CountAsync(p => !p.IsDeleted);

            var completedTasks = await _context.Tasks
                .CountAsync(t => t.TaskStatus == TaskStatuses.Completed && !t.IsDeleted);

            var pendingTasks = await _context.Tasks
                .CountAsync(t => t.TaskStatus == TaskStatuses.Pending && !t.IsDeleted);

            var overdueProjects = await _context.Projects
                .Where(p => !p.IsDeleted
                         && p.EndDate < now
                         && p.ProjectStatus != ProjectStatus.Completed)
                .OrderBy(p => p.EndDate)
                .Take(5)
                .Select(p => new GetProjectResponse
                {
                    Id = p.Id,
                    Name = p.Name,
                    Description = p.Description,
                    UserId = p.UserId,
                    StartDate = p.StartDate,
                    EndDate = p.EndDate
                })
                .AsNoTracking()
                .ToListAsync();

            var recentCompletedProjects = await _context.Projects
                .Where(p => !p.IsDeleted && p.ProjectStatus == ProjectStatus.Completed)
                .OrderByDescending(p => p.UpdatedAt)
                .Take(5)
                .Select(p => new GetProjectResponse
                {
                    Id = p.Id,
                    Name = p.Name,
                    Description = p.Description,
                    UserId = p.UserId,
                    StartDate = p.StartDate,
                    EndDate = p.EndDate
                })
                .AsNoTracking()
                .ToListAsync();

            var overdueTasks = await _context.Tasks
                .Where(t => !t.IsDeleted
                         && t.DueDate < now
                         && t.TaskStatus != TaskStatuses.Completed)
                .OrderBy(t => t.DueDate)
                .Take(5)
                .Select(t => new GetTaskResponse
                {
                    Id = t.Id,
                    Title = t.Title,
                    Description = t.Description,
                    ProjectId = t.ProjectId,
                    ProjectName = t.Project != null ? t.Project.Name : string.Empty,
                    AssignedToUserId = t.AssignedToUserId,
                    AssignedToUserName = t.AssignedTo != null ? t.AssignedTo.Name : string.Empty,
                    TaskStatus = t.TaskStatus,
                    Priority = t.Priority,
                    DueDate = t.DueDate
                })
                .AsNoTracking()
                .ToListAsync();

            return new AdminDashboardResponse
            {
                TotalEmployees = totalEmployees,
                TotalProjects = totalProjects,
                CompletedTasks = completedTasks,
                PendingTasks = pendingTasks,
                OverdueProjects = overdueProjects,
                RecentCompletedProjects = recentCompletedProjects,
                OverdueTasks = overdueTasks
            };
        }
    }
}